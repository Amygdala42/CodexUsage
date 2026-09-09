using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;

// Controlled process at the real stdio boundary. Rejects requests out of order.
internal static class FakeCodexServer
{
    public static int Main()
    {
        string scenario = Environment.GetEnvironmentVariable("QUOTA_FAKE_SCENARIO");
        string pidFile = Environment.GetEnvironmentVariable("QUOTA_FAKE_PID_FILE");
        if (!String.IsNullOrEmpty(pidFile)) File.WriteAllText(pidFile, Process.GetCurrentProcess().Id.ToString());
        if (scenario == "early-exit") return 2;
        var json = new JavaScriptSerializer();
        int stage = 0;
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var input = json.Deserialize<Dictionary<string, object>>(line);
            string method = (string)input["method"];
            object id = input.ContainsKey("id") ? input["id"] : null;
            if (stage == 0 && method == "initialize")
            {
                stage++;
                Console.WriteLine(json.Serialize(new { id = id, result = new { userAgent = "fake-test" } }));
            }
            else if (stage == 1 && method == "initialized") stage++;
            else if (stage == 2 && method == "account/read")
            {
                stage++;
                if (scenario == "hang") { Thread.Sleep(60000); return 3; }
                if (scenario == "api-error")
                    Console.WriteLine(json.Serialize(new { id = id, error = new { code = 401, message = "SECRET_MUST_NOT_LEAK" } }));
                else if (scenario == "signed-out")
                    Console.WriteLine(json.Serialize(new { id = id, result = new { account = (object)null, requiresOpenaiAuth = true } }));
                else
                    Console.WriteLine(json.Serialize(new { id = id, result = new { account = new { type = "chatgpt", planType = "pro", email = "private@example.test" }, requiresOpenaiAuth = true } }));
            }
            else if (stage == 3 && method == "account/rateLimits/read")
            {
                stage++;
                Console.WriteLine("{\"method\":\"account/updated\",\"params\":{\"planType\":\"pro\"}}");
                if (scenario == "malformed") Console.WriteLine("not-json");
                else Console.WriteLine(json.Serialize(new {
                    id = id, result = new { rateLimits = new {
                        limitId = "codex", limitName = (string)null,
                        primary = new { usedPercent = 37, windowDurationMins = 10080, resetsAt = 1900000000L },
                        secondary = (object)null, planType = "pro"
                    } }
                }));
            }
            else return 7;
            Console.Out.Flush();
        }
        return 0;
    }
}
