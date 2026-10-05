using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CodexQuotaLite
{
    public sealed class CodexQuotaSource : IQuotaSource
    {
        private static readonly object processStartEncodingSync = new object();
        private static readonly Encoding protocolEncoding = new UTF8Encoding(false);
        private readonly string executableOverride;
        private readonly string supportDirectory;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly object sync = new object();
        private Process activeProcess;
        private bool disposed;

        public CodexQuotaSource(string codexExecutableOverride, string supportDirectory)
        {
            executableOverride = codexExecutableOverride;
            this.supportDirectory = Path.GetFullPath(supportDirectory);
        }

        public Task<QuotaSnapshot> FetchAsync(CancellationToken cancellationToken)
        {
            return Task.Run(delegate { return FetchCoreAsync(cancellationToken); }, cancellationToken);
        }

        private async Task<QuotaSnapshot> FetchCoreAsync(CancellationToken cancellationToken)
        {
            lock (sync) { if (disposed) throw new ObjectDisposedException("CodexQuotaSource"); }
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            {
                linked.CancelAfter(TimeSpan.FromSeconds(35));
                bool entered = false;
                Process process = null;
                ProcessJob job = null;
                string stage = "waiting";
                try
                {
                    await gate.WaitAsync(linked.Token).ConfigureAwait(false);
                    entered = true;
                    stage = "paths";
                    linked.Token.ThrowIfCancellationRequested();
                    string executable = FindExecutable();
                    string temp = Path.Combine(supportDirectory, "tmp", "CodexQuotaLite");
                    string logs = Path.Combine(supportDirectory, "logs", "CodexQuotaLite");
                    Directory.CreateDirectory(temp);
                    Directory.CreateDirectory(logs);
                    var json = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024, RecursionLimit = 64 };
                    stage = "process-options";
                    var info = new ProcessStartInfo {
                        FileName = executable,
                        Arguments = "app-server -c " + QuoteArgument("log_dir=" + json.Serialize(logs.Replace('\\', '/'))) + " -c analytics.enabled=false",
                        WorkingDirectory = temp,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = new UTF8Encoding(false),
                        StandardErrorEncoding = new UTF8Encoding(false)
                    };
                    stage = "environment";
                    StringDictionary environment = GetChildEnvironment(info);
                    environment["TEMP"] = temp;
                    environment["TMP"] = temp;
                    environment["RUST_LOG"] = "off";
                    stage = "process-start";
                    process = new Process { StartInfo = info };
                    lock (sync)
                    {
                        linked.Token.ThrowIfCancellationRequested();
                        if (disposed) throw new OperationCanceledException(linked.Token);
                        StartWithoutInputPreamble(process);
                        activeProcess = process;
                        stage = "job";
                        job = ProcessJob.TryAttach(process);
                    }
                    // Discard stderr without retaining raw account or service output.
                    stage = "protocol";
                    Task stderr = DrainAsync(process.StandardError);
                    Task cancellation = Task.Delay(Timeout.Infinite, linked.Token);
                    var replies = new ProtocolLineReader(process.StandardOutput.BaseStream);
                    await SendAsync(process, json.Serialize(new {
                        id = 1, method = "initialize", @params = new {
                            clientInfo = new { name = "codex_quota_lite", title = "Codex Quota Lite", version = typeof(CodexQuotaSource).Assembly.GetName().Version.ToString(3) }
                        }
                    })).ConfigureAwait(false);
                    await ReadReplyAsync(replies, 1, json, cancellation, linked.Token).ConfigureAwait(false);
                    await SendAsync(process, "{\"method\":\"initialized\",\"params\":{}}").ConfigureAwait(false);
                    await SendAsync(process, "{\"id\":2,\"method\":\"account/read\",\"params\":{\"refreshToken\":false}}").ConfigureAwait(false);
                    string accountJson = await ReadReplyAsync(replies, 2, json, cancellation, linked.Token).ConfigureAwait(false);
                    ValidateAccount(accountJson, json);
                    await SendAsync(process, "{\"id\":3,\"method\":\"account/rateLimits/read\"}").ConfigureAwait(false);
                    string limitsJson = await ReadReplyAsync(replies, 3, json, cancellation, linked.Token).ConfigureAwait(false);
                    QuotaSnapshot snapshot = QuotaParser.Parse(accountJson, limitsJson, DateTimeOffset.UtcNow);
                    // Drain task observes its own errors; the process is closed by finally below.
                    GC.KeepAlive(stderr);
                    return snapshot;
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested || lifetime.IsCancellationRequested) throw;
                    throw new InvalidOperationException("读取超时，请检查网络后刷新。");
                }
                catch (InvalidOperationException) { throw; }
                catch (System.ComponentModel.Win32Exception)
                {
                    throw new InvalidOperationException("无法启动 Codex，请确认已安装且可以正常打开。");
                }
                catch (UnauthorizedAccessException)
                {
                    throw new InvalidOperationException("无法访问程序目录，请移到有写入权限的文件夹后重试。");
                }
                catch (IOException)
                {
                    if (linked.IsCancellationRequested) throw new OperationCanceledException(linked.Token);
                    throw new InvalidOperationException("Codex 连接已断开，请稍后刷新。");
                }
                catch (Exception exception)
                {
                    if (linked.IsCancellationRequested) throw new OperationCanceledException(linked.Token);
                    var safeError = new InvalidOperationException("暂时无法读取额度，请确认 Codex 已登录且网络可用。");
                    safeError.Data["CauseType"] = exception.GetType().Name;
                    safeError.Data["Stage"] = stage;
                    safeError.Data["Stack"] = exception.StackTrace;
                    throw safeError;
                }
                finally
                {
                    if (process != null)
                    {
                        CloseProcess(process);
                        lock (sync) { if (ReferenceEquals(activeProcess, process)) activeProcess = null; }
                        if (job != null) job.Dispose();
                        process.Dispose();
                    }
                    if (entered) gate.Release();
                }
            }
        }

        private static async Task SendAsync(Process process, string message)
        {
            // Never buffer text in Framework's writer: its encoding comes from
            // Console.InputEncoding, which may be a legacy code page or UTF-16.
            byte[] bytes = protocolEncoding.GetBytes(message + "\n");
            Stream input = process.StandardInput.BaseStream;
            await input.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            await input.FlushAsync().ConfigureAwait(false);
        }

        private static void StartWithoutInputPreamble(Process process)
        {
            // .NET Framework has no ProcessStartInfo.StandardInputEncoding.
            // Process.Start immediately AutoFlushes its writer, including a BOM,
            // before callers can replace it. Serialize our starts and suppress
            // only that preamble, restoring the host's encoding and input reader.
            lock (processStartEncodingSync)
            {
                Encoding original = Console.InputEncoding;
                if (original.GetPreamble().Length == 0)
                {
                    // A WinExe normally has no console. Do not call the console
                    // encoding setter here: SetConsoleCP would fail in that case.
                    process.Start();
                    return;
                }
                Encoding withoutPreamble;
                switch (original.CodePage)
                {
                    case 65001: withoutPreamble = new UTF8Encoding(false); break;
                    case 1200: withoutPreamble = new UnicodeEncoding(false, false); break;
                    case 1201: withoutPreamble = new UnicodeEncoding(true, false); break;
                    case 12000: withoutPreamble = new UTF32Encoding(false, false); break;
                    case 12001: withoutPreamble = new UTF32Encoding(true, false); break;
                    default: throw new InvalidOperationException("无法建立 Codex UTF-8 连接，请重新打开程序后重试。");
                }
                TextReader originalReader = Console.In;
                Console.InputEncoding = withoutPreamble;
                try { process.Start(); }
                finally
                {
                    try { Console.InputEncoding = original; }
                    finally { Console.SetIn(originalReader); }
                }
            }
        }

        private static StringDictionary GetChildEnvironment(ProcessStartInfo info)
        {
            try { return info.EnvironmentVariables; }
            catch (ArgumentException)
            {
                // Framework's lazy copy uses Add and fails when a host provides both Path/PATH.
                // That first copy has already allocated its dictionary; rebuild it with case-insensitive assignment.
                StringDictionary values = info.EnvironmentVariables;
                values.Clear();
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {
                    string key = (string)entry.Key;
                    values[key] = Environment.GetEnvironmentVariable(key) ?? (string)entry.Value;
                }
                return values;
            }
        }

        private static async Task<string> ReadReplyAsync(ProtocolLineReader replies, int expectedId,
            JavaScriptSerializer json, Task cancellation, CancellationToken token)
        {
            for (int count = 0; count < 512; count++)
            {
                token.ThrowIfCancellationRequested();
                string line = await replies.ReadLineAsync(cancellation, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (line == null) throw new InvalidOperationException("Codex 连接已退出，请确认本机 Codex 能正常运行。");
                Dictionary<string, object> message;
                try { message = json.Deserialize<Dictionary<string, object>>(line.TrimStart('\uFEFF')); }
                catch { throw new InvalidOperationException("Codex 返回了无法识别的数据，请刷新或更新 Codex。"); }
                if (message == null) continue;
                object id;
                if (message.ContainsKey("method") || !message.TryGetValue("id", out id) || id == null) continue;
                if (Convert.ToString(id, System.Globalization.CultureInfo.InvariantCulture) != expectedId.ToString()) continue;
                if (message.ContainsKey("error")) throw new InvalidOperationException("额度服务暂不可用，请确认 Codex 已登录后重试。");
                if (!message.ContainsKey("result")) throw new InvalidOperationException("额度响应不完整，请稍后刷新。");
                return line;
            }
            throw new InvalidOperationException("Codex 返回的通知过多，请稍后刷新。");
        }

        private sealed class ProtocolLineReader
        {
            private const int MaximumLineLength = 1024 * 1024;
            private readonly Stream stream;
            private readonly Decoder decoder = protocolEncoding.GetDecoder();
            private readonly byte[] bytes = new byte[4096];
            private readonly char[] characters = new char[protocolEncoding.GetMaxCharCount(4096)];
            private int position;
            private int length;
            private bool ended;
            private bool firstCharacter = true;
            private bool skipLineFeed;

            public ProtocolLineReader(Stream stream) { this.stream = stream; }

            public async Task<string> ReadLineAsync(Task cancellation, CancellationToken token)
            {
                var line = new StringBuilder();
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    while (position < length)
                    {
                        char value = characters[position++];
                        if (firstCharacter)
                        {
                            firstCharacter = false;
                            if (value == '\uFEFF') continue;
                        }
                        if (skipLineFeed)
                        {
                            skipLineFeed = false;
                            if (value == '\n') continue;
                        }
                        if (value == '\r' || value == '\n')
                        {
                            skipLineFeed = value == '\r';
                            return line.ToString();
                        }
                        if (line.Length == MaximumLineLength)
                            throw new InvalidOperationException("额度响应过大，请稍后重试。");
                        line.Append(value);
                    }
                    if (ended) return line.Length == 0 ? null : line.ToString();

                    // Pipe reads return available bytes; StreamReader.ReadAsync may
                    // wait to fill a character buffer even after a complete reply.
                    Task<int> read = stream.ReadAsync(bytes, 0, bytes.Length);
                    if (await Task.WhenAny(read, cancellation).ConfigureAwait(false) != read)
                    {
                        // Closing the owned process releases the pending pipe read.
                        // Observe any resulting error without retaining its output.
                        GC.KeepAlive(read.ContinueWith(delegate(Task<int> finished) { GC.KeepAlive(finished.Exception); },
                            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default));
                        token.ThrowIfCancellationRequested();
                        throw new OperationCanceledException(token);
                    }
                    int count = await read.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    ended = count == 0;
                    length = decoder.GetChars(bytes, 0, count, characters, 0, ended);
                    position = 0;
                }
            }
        }

        private static void ValidateAccount(string data, JavaScriptSerializer json)
        {
            var envelope = json.Deserialize<Dictionary<string, object>>(data);
            object value;
            var result = envelope.TryGetValue("result", out value) ? value as Dictionary<string, object> : null;
            var account = result != null && result.TryGetValue("account", out value) ? value as Dictionary<string, object> : null;
            if (account == null) throw new InvalidOperationException("请先在 Codex 中登录 ChatGPT 账号，再点击刷新。");
            string type = account.TryGetValue("type", out value) ? value as string : null;
            if (type != "chatgpt" && type != "chatgptAuthTokens")
                throw new InvalidOperationException("当前登录方式不提供订阅额度，请在 Codex 中使用 ChatGPT 账号登录。");
        }

        private static async Task DrainAsync(StreamReader reader)
        {
            var buffer = new char[1024];
            try { while (await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false) > 0) { } }
            catch { /* Process shutdown closes this stream. No stderr data is retained. */ }
        }

        private static void CloseProcess(Process process)
        {
            try { process.StandardInput.Close(); } catch { }
            try
            {
                // FetchCoreAsync runs on a worker; this bounded cleanup never blocks the UI.
                if (!process.WaitForExit(500)) { process.Kill(); process.WaitForExit(1500); }
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }

        private string FindExecutable()
        {
            if (!String.IsNullOrWhiteSpace(executableOverride))
            {
                if (File.Exists(executableOverride)) return Path.GetFullPath(executableOverride);
                throw new InvalidOperationException("指定的 Codex 程序不存在，请检查程序路径。");
            }
            string installedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
            if (Directory.Exists(installedRoot))
            {
                var candidates = new DirectoryInfo(installedRoot).GetDirectories().OrderByDescending(d => d.LastWriteTimeUtc);
                foreach (var directory in candidates)
                {
                    string candidate = Path.Combine(directory.FullName, "codex.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string folder in path.Split(Path.PathSeparator))
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(folder)) continue;
                    string candidate = Path.Combine(folder.Trim('"'), "codex.exe");
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }
                catch (ArgumentException) { }
                catch (NotSupportedException) { }
            }
            throw new InvalidOperationException("未找到 Codex，请先安装并登录 Codex 后重试。");
        }

        private static string QuoteArgument(string value)
        {
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in value)
            {
                if (character == '\\') { slashes++; continue; }
                if (character == '"') { result.Append('\\', slashes * 2 + 1); result.Append('"'); }
                else { result.Append('\\', slashes); result.Append(character); }
                slashes = 0;
            }
            result.Append('\\', slashes * 2); result.Append('"');
            return result.ToString();
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                lifetime.Cancel();
                // Only the process created by this instance is ever terminated.
                if (activeProcess != null)
                {
                    try { if (!activeProcess.HasExited) activeProcess.Kill(); }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                }
            }
        }
    }
}
