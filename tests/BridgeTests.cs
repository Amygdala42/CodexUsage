using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodexQuotaLite;

internal static class BridgeTests
{
    private static string fakePath;
    private static string supportPath;
    private static string pidPath;
    private static int passed;
    private static int failed;

    public static int Main(string[] args)
    {
        fakePath = args[0]; supportPath = args[1];
        Directory.CreateDirectory(supportPath);
        pidPath = Path.Combine(supportPath, "fake.pid");
        Environment.SetEnvironmentVariable("QUOTA_FAKE_PID_FILE", pidPath);
        Run("ordered handshake and notices return actual parsed quota", delegate {
            Scenario("success");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                var result = source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult();
                Check(result.PlanLabel == "Pro", "plan");
                Check(result.Windows.Count == 1 && result.Windows[0].RemainingPercent == 63, "remaining quota");
            }
            NoChild();
        });
        Run("API errors do not reveal service details", delegate {
            Scenario("api-error");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "SECRET_MUST_NOT_LEAK");
            NoChild();
        });
        Run("sign out gives a login action", delegate {
            Scenario("signed-out");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            {
                try { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
                catch (InvalidOperationException ex) { Check(ex.Message.Contains("登录"), "login guidance"); }
            }
            NoChild();
        });
        Run("cancellation interrupts stalled server and closes owned process", delegate {
            Scenario("hang");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
            using (var cancel = new CancellationTokenSource(800))
            {
                var elapsed = Stopwatch.StartNew();
                try { source.FetchAsync(cancel.Token).GetAwaiter().GetResult(); throw new Exception("unexpected success"); }
                catch (OperationCanceledException) { }
                Check(elapsed.Elapsed.TotalSeconds < 5, "cancellation bound");
            }
            NoChild();
        });
        Run("unexpected process exit fails promptly", delegate {
            Scenario("early-exit");
            var elapsed = Stopwatch.StartNew();
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "private@example.test");
            Check(elapsed.Elapsed.TotalSeconds < 5, "early exit bound");
            NoChild();
        });
        Run("malformed protocol data is rejected safely", delegate {
            Scenario("malformed");
            using (var source = new CodexQuotaSource(fakePath, supportPath))
                ExpectFailure(delegate { source.FetchAsync(CancellationToken.None).GetAwaiter().GetResult(); }, "not-json");
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
            NoChild();
        });
        Run("dispose cancels an active refresh", delegate {
            Scenario("hang");
            var source = new CodexQuotaSource(fakePath, supportPath);
            var task = source.FetchAsync(CancellationToken.None);
            Thread.Sleep(300);
            source.Dispose();
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
    }
    private static void NoChild()
    {
        if (!File.Exists(pidPath)) return;
        int id = Int32.Parse(File.ReadAllText(pidPath));
        try { using (var child = Process.GetProcessById(id)) { Check(child.HasExited, "query process leaked"); } }
        catch (ArgumentException) { }
    }
    private static void ExpectFailure(Action call, string forbidden)
    {
        try { call(); throw new Exception("unexpected success"); }
            catch (InvalidOperationException ex) { Check(!ex.Message.Contains(forbidden), "sensitive detail leaked"); Check(!ex.Data.Contains("CauseType"), "unexpected internal failure: " + ex.Data["CauseType"]); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.GetType().Name + " " + ex.Message + " cause=" + ex.Data["CauseType"] + " stage=" + ex.Data["Stage"] + " stack=" + ex.Data["Stack"]); }
    }
}
