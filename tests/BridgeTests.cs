using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CodexQuotaLite;

internal static class BridgeTests
{
    private static string fakePath;
    private static string supportPath;
    private static string pidPath;
    private static int passed;
    private static int failed;
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();
    [DllImport("kernel32.dll")] private static extern uint GetConsoleCP();

    public static int Main(string[] args)
    {
        if (args.Length == 4 && args[0] == "--without-console") return WithoutConsole(args);
        fakePath = args[0]; supportPath = args[1];
        Directory.CreateDirectory(supportPath);
        pidPath = Path.Combine(supportPath, "fake.pid");
        Environment.SetEnvironmentVariable("QUOTA_FAKE_PID_FILE", pidPath);
        Run("UTF-8 BOM host sends a BOM-free first request and restores input encoding", delegate {
            CheckHostEncoding(new UTF8Encoding(true));
        });
        Run("UTF-16 host sends UTF-8 requests and restores input encoding", delegate {
            CheckHostEncoding(new UnicodeEncoding(false, true));
        });
        Run("UTF-16 without a BOM still sends UTF-8 requests", delegate {
            CheckHostEncoding(new UnicodeEncoding(false, false));
        });
        Run("failed process startup restores the host input encoding and reader", delegate {
            Scenario("success");
            string invalid = Path.Combine(supportPath, "not-an-executable.bin");
            File.WriteAllText(invalid, "Invalid executable fixture");
            Encoding original = Console.InputEncoding;
            TextReader originalReader = Console.In;
            try
            {
                Console.InputEncoding = new UTF8Encoding(true);
                Console.SetIn(new StringReader("input retained after startup failure"));
                using (var source = new CodexQuotaSource(invalid, supportPath))
                    ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "无法启动 Codex", invalid);
                Check(Console.InputEncoding.CodePage == 65001 && Console.InputEncoding.GetPreamble().Length == 3, "failed startup changed encoding");
                Check(Console.In.ReadLine() == "input retained after startup failure", "failed startup changed reader");
            }
            finally { Console.InputEncoding = original; Console.SetIn(originalReader); }
        });
        Run("simultaneous quota sources preserve the host encoding", delegate {
            Scenario("success");
            Encoding original = Console.InputEncoding;
            TextReader originalReader = Console.In;
            var sources = new CodexQuotaSource[8];
            try
            {
                Console.InputEncoding = new UTF8Encoding(true);
                Console.SetIn(new StringReader("input retained after parallel starts"));
                // Each child still enforces the byte-level protocol; disable the
                // single-process evidence file so concurrent children do not share it.
                Environment.SetEnvironmentVariable("QUOTA_FAKE_PID_FILE", null);
                var tasks = new Task<QuotaSnapshot>[sources.Length];
                for (int i = 0; i < sources.Length; i++)
                {
                    sources[i] = new CodexQuotaSource(fakePath, Path.Combine(supportPath, "parallel-" + i));
                    tasks[i] = sources[i].FetchAsync(CancellationToken.None);
                }
                Task.WaitAll(tasks);
                foreach (var task in tasks) Check(task.Result.Windows[0].RemainingPercent == 63, "parallel quota result");
                Check(Console.InputEncoding.CodePage == 65001 && Console.InputEncoding.GetPreamble().Length == 3, "parallel startup changed encoding");
                Check(Console.In.ReadLine() == "input retained after parallel starts", "parallel startup changed reader");
            }
            finally
            {
                foreach (var source in sources) if (source != null) source.Dispose();
                Environment.SetEnvironmentVariable("QUOTA_FAKE_PID_FILE", pidPath);
                Console.InputEncoding = original; Console.SetIn(originalReader);
            }
        });
        Run("quota reads work without an attached console", delegate {
            Scenario("success");
            string report = Path.Combine(supportPath, "without-console.txt");
            if (File.Exists(report)) File.Delete(report);
            var options = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,
                "--without-console \"" + fakePath + "\" \"" + supportPath + "\" \"" + report + "\"") {
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            };
            using (var child = Process.Start(options))
            {
                if (!child.WaitForExit(10000)) { child.Kill(); child.WaitForExit(); throw new Exception("console-free host timed out"); }
                string result = File.Exists(report) ? File.ReadAllText(report) : "no child report";
                Check(child.ExitCode == 0 && result.StartsWith("PASS ConsoleCP=0;"), result);
                Console.WriteLine(result);
            }
            NoChild();
        });
        Run("ordered handshake and notices return actual parsed quota", delegate {
            Scenario("success");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                var result = source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult();
                Check(result.PlanLabel == "Pro", "plan");
                Check(result.Windows.Count == 1 && result.Windows[0].RemainingPercent == 63, "remaining quota");
            }
            Reached("first-byte:7B");
            Reached("client-version:" + typeof(CodexQuotaSource).Assembly.GetName().Version.ToString(3));
            Reached("eof");
            NoChild();
        });
        Run("API errors do not reveal service details", delegate {
            Scenario("api-error");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "服务暂不可用", "SECRET_MUST_NOT_LEAK");
            Reached("api-error");
            NoChild();
        });
        Run("sign out gives a login action", delegate {
            Scenario("signed-out");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                try { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
                catch (InvalidOperationException ex) { Check(ex.Message.Contains("登录"), "login guidance"); }
            }
            Reached("signed-out");
            NoChild();
        });
        Run("cancellation interrupts stalled server and closes owned process", delegate {
            Scenario("hang");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            using (var cancel = new CancellationTokenSource())
            {
                var task = source.FetchAsync(cancel.Token);
                WaitForStage("hang", 5000);
                var elapsed = Stopwatch.StartNew();
                cancel.Cancel();
                try { task.GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
                catch (OperationCanceledException) { }
                Check(elapsed.Elapsed.TotalSeconds < 5, "cancellation bound");
            }
            Reached("hang");
            NoChild();
        });
        Run("unexpected process exit fails promptly", delegate {
            Scenario("early-exit");
            var elapsed = Stopwatch.StartNew();
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "连接已", "private@example.test");
            Check(elapsed.Elapsed.TotalSeconds < 5, "early exit bound");
            Reached("started");
            NoChild();
        });
        Run("malformed protocol data is rejected safely", delegate {
            Scenario("malformed");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "无法识别", "not-json");
            Reached("malformed");
            NoChild();
        });
        Run("stalled server reaches automatic deadline without caller cancellation", delegate {
            Scenario("hang");
            var elapsed = Stopwatch.StartNew();
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                try { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
                catch (InvalidOperationException ex) { Check(ex.Message.Contains("超时"), "automatic timeout guidance"); }
            }
            Check(elapsed.Elapsed.TotalSeconds >= 30 && elapsed.Elapsed.TotalSeconds < 40, "automatic timeout bound");
            Reached("hang");
            NoChild();
        });
        Run("dispose cancels an active refresh", delegate {
            Scenario("hang");
            var source = new CodexQuotaSource(fakePath, supportPath);
            var task = source.FetchAsync(CancellationToken.None);
            try { WaitForStage("hang", 5000); }
            finally { source.Dispose(); }
            try { task.GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            NoChild();
        });
        Environment.SetEnvironmentVariable("QUOTA_FAKE_SCENARIO", null);
        Environment.SetEnvironmentVariable("QUOTA_FAKE_PID_FILE", null);
        Console.WriteLine("Bridge: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
    private static void Scenario(string name)
    {
        Environment.SetEnvironmentVariable("QUOTA_FAKE_SCENARIO", name);
        if (File.Exists(pidPath)) File.Delete(pidPath);
        if (File.Exists(pidPath + ".trace")) File.Delete(pidPath + ".trace");
    }
    private static void NoChild()
    {
        Check(File.Exists(pidPath), "query process never started");
        int id = Int32.Parse(File.ReadAllText(pidPath));
        try { using (var child = Process.GetProcessById(id)) { Check(child.HasExited, "query process leaked"); } }
        catch (ArgumentException) { }
    }
    private static void ExpectFailure(Action call, string expected, string forbidden)
    {
        try { call(); throw new Exception("unexpected success"); }
            catch (InvalidOperationException ex) { Check(ex.Message.Contains(expected), "wrong failure: " + ex.Message); Check(!ex.Message.Contains(forbidden), "sensitive detail leaked"); Check(!ex.Data.Contains("CauseType"), "unexpected internal failure: " + ex.Data["CauseType"]); }
    }
    private static void CheckHostEncoding(Encoding requested)
    {
        Encoding original = Console.InputEncoding;
        TextReader originalReader = Console.In;
        try
        {
            Console.InputEncoding = requested;
            Console.SetIn(new StringReader("existing console input"));
            uint consoleCodePage = GetConsoleCP();
            Scenario("success");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                var result = source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult();
                Check(result.PlanLabel == "Pro" && result.Windows[0].RemainingPercent == 63, "actual parsed quota");
            }
            Reached("first-byte:7B");
            Reached("eof");
            Check(Console.InputEncoding.CodePage == requested.CodePage &&
                BitConverter.ToString(Console.InputEncoding.GetPreamble()) == BitConverter.ToString(requested.GetPreamble()), "host input encoding changed");
            Check(Console.In.ReadLine() == "existing console input", "host console input reader changed");
            Check(GetConsoleCP() == consoleCodePage, "native console code page changed");
            NoChild();
        }
        finally { Console.InputEncoding = original; Console.SetIn(originalReader); }
    }
    private static int WithoutConsole(string[] args)
    {
        try
        {
            FreeConsole();
            Check(GetConsoleCP() == 0, "child still has a console");
            Encoding original = Console.InputEncoding;
            using (var source = new CodexQuotaSource(args[1], args[2]))
            {
                var result = source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult();
                Check(result.PlanLabel == "Pro" && result.Windows[0].RemainingPercent == 63, "console-free quota");
            }
            Check(Console.InputEncoding.Equals(original), "console-free encoding changed");
            File.WriteAllText(args[3], "PASS ConsoleCP=0; InputCP=" + original.CodePage + "; Preamble=" + BitConverter.ToString(original.GetPreamble()));
            return 0;
        }
        catch (Exception ex) { File.WriteAllText(args[3], "FAIL " + ex); return 1; }
    }
    private static bool HasStage(string stage)
    {
        try { return File.Exists(pidPath + ".trace") && Array.IndexOf(File.ReadAllLines(pidPath + ".trace"), stage) >= 0; }
        catch (IOException) { return false; }
    }
    private static void Reached(string stage) { Check(HasStage(stage), "server did not reach " + stage); }
    private static void WaitForStage(string stage, int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (!HasStage(stage) && elapsed.ElapsedMilliseconds < milliseconds) Thread.Sleep(20);
        Reached(stage);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Run(string name, Action test)
    {
        var elapsed = Stopwatch.StartNew();
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.GetType().Name + " " + ex.Message + " cause=" + ex.Data["CauseType"] + " stage=" + ex.Data["Stage"] + " stack=" + ex.Data["Stack"]); }
        Console.WriteLine("  elapsed=" + elapsed.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "s; trace=" + (File.Exists(pidPath + ".trace") ? String.Join(",", File.ReadAllLines(pidPath + ".trace")) : "none"));
    }
}
