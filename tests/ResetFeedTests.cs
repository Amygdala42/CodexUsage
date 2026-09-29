using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodexQuotaLite;

[assembly: AssemblyVersion("7.8.9.0")]

internal static class ResetFeedTests
{
    private const string Endpoint = "https://codex-resets.com/api/v1/status";
    private const string Good = "{\"data\":{\"latest_reset\":{\"announced_at\":\"2026-09-12T08:09:17Z\",\"reset_type\":\"regular\",\"source\":{\"url\":\"https://x.com/example/status/123\"}}}}";
    private static readonly RequestFactory Factory = new RequestFactory();
    private static string root;
    private static int passed;
    private static int failed;

    public static int Main(string[] args)
    {
        root = Path.Combine(args.Length == 0 ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reset-feed-support") : args[0], Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        // Intercept HTTPS only inside this test process. An unexpected endpoint is rejected,
        // never sent to the Internet. Production parsing, files and HttpWebRequest stay real.
        Check(WebRequest.RegisterPrefix("https://", Factory), "register offline request factory");
        Run("valid cache restores an announcement", delegate {
            ResetFeed feed = Seed("restore"); Check(feed.Cached && !feed.Failed && feed.Latest.Type == "regular", "restore state");
            Check(feed.Latest.Time == new DateTimeOffset(2026, 9, 12, 8, 9, 17, TimeSpan.Zero), "restore UTC time");
        });
        foreach (string invalid in new[] { "broken", new string('x', 262145) }) {
            string body = invalid; Run("invalid startup cache is ignored (" + body.Length + " characters)", delegate {
                string path = CachePath(); File.WriteAllText(path, body); ResetFeed feed = new ResetFeed(path);
                Check(feed.Latest == null && !feed.Cached && !feed.Failed, "invalid cache ignored");
            });
        }
        Run("successful response writes an exact cache and prevents immediate refresh", delegate {
            string path = CachePath(); ResetFeed feed = new ResetFeed(path); string request = Respond(feed, 200, Good, null);
            Check(!feed.Cached && !feed.Failed && feed.Latest.Type == "regular", "fresh announcement");
            Check(File.ReadAllText(path) == Good && !File.Exists(path + ".tmp"), "atomic cache contents");
            Check(request.Contains("User-Agent: CodexUsage/7.8.9"), "app identifies its own assembly version");
            CheckDelay(feed, 900);
        });
        Run("successful response replaces an existing cache", delegate {
            string path = CachePath(); File.WriteAllText(path, Good); ResetFeed feed = new ResetFeed(path);
            string banked = Good.Replace("regular", "banked"); Respond(feed, 200, banked, null);
            Check(File.ReadAllText(path) == banked && !File.Exists(path + ".tmp") && feed.Latest.Type == "banked", "cache replaced");
        });
        Run("scheduled reset is never presented as completed", delegate {
            ResetFeed feed = Seed("scheduled"); Respond(feed, 200, "{\"data\":{\"latest_reset\":null,\"scheduled_reset\":{\"reset_type\":\"regular\"}}}", null);
            Check(!feed.Failed && !feed.Cached && feed.Latest == null, "no invented completed event");
        });
        foreach (string invalid in new[] { "broken", "{\"data\":{}}", Good.Replace("2026-09-12T08:09:17Z", "bad-date") }) {
            string body = invalid; Run("invalid response preserves previous announcement (" + body.Length + " characters)", delegate {
                string path = CachePath(); File.WriteAllText(path, Good); ResetFeed feed = new ResetFeed(path); Respond(feed, 200, body, null);
                Check(feed.Failed && feed.Cached && feed.Latest.Type == "regular" && File.ReadAllText(path) == Good, "old data retained");
            });
        }
        Run("unsafe announcement URL falls back to the public site", delegate {
            ResetFeed feed = new ResetFeed(CachePath()); Respond(feed, 200, Good.Replace("https://x.com/example/status/123", "file:///C:/unsafe.exe"), null);
            Check(!feed.Failed && feed.Latest.Url == "https://codex-resets.com/", "unsafe URL rejected");
        });
        foreach (int status in new[] { 204, 302, 404, 500, 503 }) {
            int code = status; Run("HTTP " + code + " retains cache without following redirects", delegate {
                ResetFeed feed = Seed("status"); Respond(feed, code, "", Headers("Location", "http://127.0.0.1:1/never-followed"));
                Check(feed.Failed && feed.Cached && feed.Latest.Type == "regular", "HTTP failure retains data");
            });
        }
        Run("304 before any successful response is rejected", delegate {
            ResetFeed feed = Seed("first304"); Respond(feed, 304, "", null); Check(feed.Failed && feed.Cached, "disk cache alone cannot validate 304");
        });
        Run("304 validates the prior result instead of reporting a service failure", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("ETag", "\"v1\"")); Due(feed);
            string request = Respond(feed, 304, "", null);
            Check(request.Contains("If-None-Match: \"v1\""), "conditional request sent");
            Check(!feed.Failed && !feed.Cached && feed.Latest.Type == "regular", "304 is a successful validation");
        });
        Run("304 updates ETag for the following conditional request", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("ETag", "\"v1\"")); Due(feed); Respond(feed, 304, "", Headers("ETag", "\"v2\"")); Due(feed);
            string request = Respond(feed, 304, "", null); Check(request.Contains("If-None-Match: \"v2\""), "updated validator used");
        });
        Run("304 without ETag retains the established validator", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("ETag", "\"v1\"")); Due(feed); Respond(feed, 304, "", null); Due(feed);
            Check(Respond(feed, 304, "", null).Contains("If-None-Match: \"v1\""), "validator retained");
            Check(!feed.Failed, "repeated 304 remains healthy");
        });
        Run("304 can validate a known empty announcement", delegate {
            ResetFeed feed = new ResetFeed(CachePath()); Respond(feed, 200, "{\"data\":{\"latest_reset\":null}}", Headers("ETag", "\"empty\"")); Due(feed);
            Respond(feed, 304, "", null); Check(!feed.Failed && !feed.Cached && feed.Latest == null, "empty result also validates");
        });
        Run("304 applies updated Cache-Control", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("Cache-Control", "max-age=1800")); Due(feed);
            Respond(feed, 304, "", Headers("Cache-Control", "max-age=3600")); CheckDelay(feed, 3600);
        });
        Run("304 without Cache-Control retains the prior freshness period", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("Cache-Control", "max-age=1800")); Due(feed);
            Respond(feed, 304, "", null); CheckDelay(feed, 1800);
        });
        Run("new 200 without ETag clears the obsolete validator", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("ETag", "\"v1\"")); Due(feed); Respond(feed, 200, Good, null); Due(feed);
            Check(!Respond(feed, 200, Good, null).Contains("If-None-Match:"), "old validator cleared");
        });
        foreach (string header in new[] { "public, max-age=1800", "public, Max-Age=1800", "max-age=\"1800\"" }) {
            string value = header; Run("Cache-Control freshness: " + value, delegate { ResetFeed feed = NewSuccessfulFeed(Headers("Cache-Control", value)); CheckDelay(feed, 1800); });
        }
        Run("short Cache-Control preserves the minimum polling interval", delegate {
            ResetFeed feed = NewSuccessfulFeed(Headers("Cache-Control", "max-age=60")); CheckDelay(feed, 900);
        });
        Run("invalid response does not commit its long Cache-Control lifetime", delegate {
            ResetFeed feed = Seed("bad-cache-policy"); Respond(feed, 200, "broken", Headers("Cache-Control", "max-age=86400"));
            Check(feed.Failed && feed.Cached, "invalid response fails"); CheckDelay(feed, 900);
        });
        foreach (int status in new[] { 429, 503 }) {
            int code = status;
            Run("HTTP " + code + " honors numeric Retry-After", delegate {
                ResetFeed feed = Seed("retry-seconds"); Respond(feed, code, "", Headers("Retry-After", "3600"));
                Check(feed.Failed && feed.Cached, "failure retains cache"); CheckDelay(feed, 3600);
            });
            Run("HTTP " + code + " honors HTTP-date Retry-After", delegate {
                ResetFeed feed = Seed("retry-date"); Respond(feed, code, "", Headers("Retry-After", DateTime.UtcNow.AddHours(1).ToString("R", CultureInfo.InvariantCulture)));
                Check(feed.Failed && feed.Cached, "failure retains cache"); CheckDelay(feed, 3600);
            });
        }
        foreach (string value in new[] { "nonsense", "-3", "999999999999999999999999999", "Tue, 01 Jan 2019 00:00:00 GMT" }) {
            string retry = value; Run("invalid or expired Retry-After retains minimum interval: " + retry, delegate {
                ResetFeed feed = Seed("bad-retry"); Respond(feed, 429, "", Headers("Retry-After", retry)); CheckDelay(feed, 900);
            });
        }
        Run("Content-Length over the response limit is rejected", delegate {
            ResetFeed feed = Seed("large-length"); Respond(feed, 200, new string('x', 262145), null); Check(feed.Failed && feed.Cached, "large response rejected");
        });
        Run("chunked response over the limit is rejected", delegate {
            ResetFeed feed = Seed("large-chunked");
            using (var server = new LocalServer(200, new string('x', 262145), null, true, Stall.None)) { Send(feed, server); }
            Check(feed.Failed && feed.Cached, "stream size limit enforced");
        });
        Run("cache write failure does not discard a fresh announcement", delegate {
            string blocker = Path.Combine(root, Guid.NewGuid().ToString("N")); File.WriteAllText(blocker, "owned fixture");
            ResetFeed feed = new ResetFeed(Path.Combine(blocker, "cache.json")); Respond(feed, 200, Good, null);
            Check(!feed.Failed && !feed.Cached && feed.Latest.Type == "regular" && File.ReadAllText(blocker) == "owned fixture", "nonfatal cache write failure");
        });
        Run("pre-cancellation neither sends a request nor blocks a later refresh", delegate {
            ResetFeed feed = Seed("pre-cancel"); int before = Factory.Count;
            using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); feed.RefreshAsync(cancel.Token).GetAwaiter().GetResult(); }
            Check(Factory.Count == before && !feed.Failed && feed.Cached, "pre-cancellation leaves state intact");
            Respond(feed, 200, Good, null); Check(!feed.Failed && !feed.Cached, "later request is allowed");
        });
        Run("header cancellation preserves healthy state and allows immediate retry", delegate { CancellationScenario(Stall.Headers, false); });
        Run("body cancellation preserves healthy state and allows immediate retry", delegate { CancellationScenario(Stall.Body, false); });
        Run("body cancellation does not relabel a previously fresh announcement as cached", delegate { CancellationScenario(Stall.Body, true); });
        Run("concurrent refresh sends only one request", delegate {
            ResetFeed feed = new ResetFeed(CachePath()); int before = Factory.Count;
            using (var server = new LocalServer(200, Good, null, false, Stall.Headers)) {
                Factory.Target = server.Url; Task first = feed.RefreshAsync(CancellationToken.None); Check(server.Accepted.WaitOne(3000), "first request reached server");
                feed.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult(); Check(Factory.Count == before + 1, "second refresh suppressed");
                server.Release.Set(); Complete(first, 5000); Check(!feed.Failed, "first refresh completes");
            }
        });
        Run("stalled headers reach the production timeout and preserve cache", delegate { TimeoutScenario(Stall.Headers); });
        Run("stalled body reaches the production read timeout and preserves cache", delegate { TimeoutScenario(Stall.Body); });
        // root is the unique GUID child created by this invocation; never remove its parent.
        try { Directory.Delete(root, true); }
        catch (IOException error) { failed++; Console.WriteLine("FAIL fixture cleanup: " + error.Message); }
        catch (UnauthorizedAccessException error) { failed++; Console.WriteLine("FAIL fixture cleanup: " + error.Message); }
        Console.WriteLine("ResetFeed: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }

    private static void CancellationScenario(Stall stall, bool fresh)
    {
        ResetFeed feed = fresh ? NewSuccessfulFeed(null) : Seed("cancel"); Due(feed); bool wasCached = feed.Cached;
        using (var server = new LocalServer(200, Good, Headers("Cache-Control", "max-age=86400"), false, stall))
        using (var cancel = new CancellationTokenSource()) {
            Factory.Target = server.Url; Task task = feed.RefreshAsync(cancel.Token);
            Check((stall == Stall.Body ? server.BodyStarted : server.Accepted).WaitOne(3000), "request reached cancellation point");
            cancel.Cancel(); Complete(task, 3000);
            Check(!feed.Failed && feed.Cached == wasCached && feed.Latest.Type == "regular", "cancellation preserves healthy announcement state");
        }
        int before = Factory.Count; Respond(feed, 200, Good.Replace("regular", "banked"), null);
        Check(Factory.Count == before + 1 && !feed.Failed && feed.Latest.Type == "banked", "cancellation permits immediate retry");
    }
    private static void TimeoutScenario(Stall stall)
    {
        ResetFeed feed = Seed("timeout"); var clock = Stopwatch.StartNew();
        using (var server = new LocalServer(200, Good, null, false, stall)) { Factory.Target = server.Url; Complete(feed.RefreshAsync(CancellationToken.None), 16000); }
        Check(clock.Elapsed.TotalSeconds >= 9 && clock.Elapsed.TotalSeconds < 16 && feed.Failed && feed.Cached, "bounded timeout with cached fallback");
    }
    private static ResetFeed NewSuccessfulFeed(Dictionary<string, string> headers) { ResetFeed feed = new ResetFeed(CachePath()); Respond(feed, 200, Good, headers); Check(!feed.Failed, "initial 200 succeeded"); return feed; }
    private static ResetFeed Seed(string label) { string path = CachePath(); File.WriteAllText(path, Good); return new ResetFeed(path); }
    private static string CachePath() { string directory = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); return Path.Combine(directory, "cache.json"); }
    private static Dictionary<string, string> Headers(string name, string value) { return new Dictionary<string, string> { { name, value } }; }
    private static string Respond(ResetFeed feed, int status, string body, Dictionary<string, string> headers)
    { using (var server = new LocalServer(status, body, headers, false, Stall.None)) { Send(feed, server); return server.Request; } }
    private static void Send(ResetFeed feed, LocalServer server) { Factory.Target = server.Url; Complete(feed.RefreshAsync(CancellationToken.None), 5000); Check(server.HeadersSent.WaitOne(1000), "fixture response was sent"); }
    private static void Complete(Task task, int milliseconds) { Check(((IAsyncResult)task).AsyncWaitHandle.WaitOne(milliseconds), "refresh completion bound"); task.GetAwaiter().GetResult(); }
    // Move only the private scheduling timestamp: a test clock without product-only hooks.
    // Assertions below observe whether a real refresh/request happens, not field equality.
    private static void Due(ResetFeed feed) { typeof(ResetFeed).GetField("nextRequest", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(feed, DateTimeOffset.MinValue); }
    private static void AdvanceClock(ResetFeed feed, int seconds) { FieldInfo field = typeof(ResetFeed).GetField("nextRequest", BindingFlags.Instance | BindingFlags.NonPublic); field.SetValue(feed, ((DateTimeOffset)field.GetValue(feed)).AddSeconds(-seconds)); }
    private static void CheckDelay(ResetFeed feed, int seconds)
    {
        using (var server = new LocalServer(200, Good, null, false, Stall.None)) {
            Factory.Target = server.Url; int before = Factory.Count; AdvanceClock(feed, seconds - 20); Complete(feed.RefreshAsync(CancellationToken.None), 3000);
            Check(Factory.Count == before, "request stays deferred until the advertised interval");
            AdvanceClock(feed, 40); Complete(feed.RefreshAsync(CancellationToken.None), 5000);
            Check(Factory.Count == before + 1 && !feed.Failed, "request resumes after the interval");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Run(string name, Action test)
    {
        var clock = Stopwatch.StartNew();
        try { test(); passed++; Console.WriteLine("PASS " + name + " (" + clock.ElapsedMilliseconds + " ms)"); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.GetType().Name + " - " + error.Message); }
    }
    private sealed class RequestFactory : IWebRequestCreate
    {
        internal Uri Target; internal int Count;
        public WebRequest Create(Uri uri)
        {
            if (uri.AbsoluteUri != Endpoint || Target == null || !Target.IsLoopback) throw new InvalidOperationException("Unexpected test request; external network is disabled");
            Interlocked.Increment(ref Count); HttpWebRequest request = WebRequest.CreateHttp(Target); request.Proxy = null; return request;
        }
    }
    private enum Stall { None, Headers, Body }
    private sealed class LocalServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Task worker;
        private TcpClient client;
        internal readonly ManualResetEvent Accepted = new ManualResetEvent(false);
        internal readonly ManualResetEvent HeadersSent = new ManualResetEvent(false);
        internal readonly ManualResetEvent BodyStarted = new ManualResetEvent(false);
        internal readonly ManualResetEvent Release = new ManualResetEvent(false);
        internal readonly Uri Url;
        internal string Request = "";
        internal LocalServer(int status, string body, Dictionary<string, string> headers, bool chunked, Stall stall)
        {
            listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); Url = new Uri("http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/status");
            worker = Task.Run(delegate {
                try {
                    client = listener.AcceptTcpClient(); client.ReceiveTimeout = 5000;
                    using (NetworkStream stream = client.GetStream()) {
                        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true)) {
                            var request = new StringBuilder(); string line;
                            while ((line = reader.ReadLine()) != null && line.Length != 0) request.AppendLine(line);
                            Request = request.ToString();
                        }
                        Accepted.Set(); if (stall == Stall.Headers) Release.WaitOne();
                        byte[] data = Encoding.UTF8.GetBytes(body);
                        var head = new StringBuilder("HTTP/1.1 " + status + " Test\r\nConnection: close\r\nContent-Type: application/json; charset=utf-8\r\n");
                        head.Append(chunked ? "Transfer-Encoding: chunked\r\n" : "Content-Length: " + data.Length + "\r\n");
                        if (headers != null) foreach (var header in headers) head.Append(header.Key + ": " + header.Value + "\r\n");
                        Write(stream, head.Append("\r\n").ToString()); stream.Flush(); HeadersSent.Set();
                        if (stall == Stall.Body) {
                            stream.Write(data, 0, 1); stream.Flush(); BodyStarted.Set(); Release.WaitOne(); stream.Write(data, 1, data.Length - 1);
                        } else {
                            if (chunked) Write(stream, data.Length.ToString("X", CultureInfo.InvariantCulture) + "\r\n");
                            stream.Write(data, 0, data.Length); if (chunked) Write(stream, "\r\n0\r\n\r\n");
                        }
                        stream.Flush();
                    }
                } catch (IOException) { } catch (SocketException) { } catch (ObjectDisposedException) { }
            });
        }
        private static void Write(Stream stream, string value) { byte[] bytes = Encoding.ASCII.GetBytes(value); stream.Write(bytes, 0, bytes.Length); }
        public void Dispose()
        {
            Release.Set(); listener.Stop(); if (client != null) client.Close();
            Check(worker.Wait(3000), "local response worker terminates");
            Accepted.Dispose(); HeadersSent.Dispose(); BodyStarted.Dispose(); Release.Dispose();
        }
    }
}
