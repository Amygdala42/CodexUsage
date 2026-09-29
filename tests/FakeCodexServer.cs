using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

// Controlled process at the real stdio boundary. Rejects requests out of order.
internal static class FakeCodexServer
{
    public static int Main()
    {
        string scenario = Environment.GetEnvironmentVariable("QUOTA_FAKE_SCENARIO");
        string pidFile = Environment.GetEnvironmentVariable("QUOTA_FAKE_PID_FILE");
        if (!String.IsNullOrEmpty(pidFile)) File.WriteAllText(pidFile, Process.GetCurrentProcess().Id.ToString());
        Trace(pidFile, "started");
        if (scenario == "early-exit") return 2;
        var json = new JavaScriptSerializer();
        int stage = 0;
        // Inspect the actual wire before a StreamReader can silently consume a BOM.
        Stream inputStream = Console.OpenStandardInput();
        int firstByte = inputStream.ReadByte();
        Trace(pidFile, "first-byte:" + firstByte.ToString("X2"));
        if (firstByte != 0x7b) return 9;
        using (var reader = new StreamReader(inputStream, new UTF8Encoding(false, true), false))
        {
            string line = "{" + reader.ReadLine();
            while (line != null)
            {
                var input = json.Deserialize<Dictionary<string, object>>(line);
                string method = (string)input["method"];
                object id = input.ContainsKey("id") ? input["id"] : null;
                if (stage == 0 && method == "initialize")
                {
                    Trace(pidFile, "initialize");
                    var parameters = (Dictionary<string, object>)input["params"];
                    var client = (Dictionary<string, object>)parameters["clientInfo"];
                    Trace(pidFile, "client-version:" + client["version"]);
                    stage++;
                    Console.WriteLine(json.Serialize(new { id = id, result = new { userAgent = "fake-test" } }));
                }
                else if (stage == 1 && method == "initialized") { Trace(pidFile, "initialized"); stage++; }
                else if (stage == 2 && method == "account/read")
                {
                    stage++;
                    Trace(pidFile, "account/read");
                    if (scenario == "hang") { Trace(pidFile, "hang"); Thread.Sleep(60000); return 3; }
                    if (scenario == "api-error")
                    {
                        Trace(pidFile, "api-error");
                        Console.WriteLine(json.Serialize(new { id = id, error = new { code = 401, message = "SECRET_MUST_NOT_LEAK" } }));
                    }
                    else if (scenario == "signed-out")
                    {
                        Trace(pidFile, "signed-out");
                        Console.WriteLine(json.Serialize(new { id = id, result = new { account = (object)null, requiresOpenaiAuth = true } }));
                    }
                    else
                        Console.WriteLine(json.Serialize(new { id = id, result = new { account = new { type = "chatgpt", planType = "pro", email = "private@example.test" }, requiresOpenaiAuth = true } }));
                }
                else if (stage == 3 && method == "account/rateLimits/read")
                {
                    stage++;
                    Trace(pidFile, "account/rateLimits/read");
                    Console.WriteLine("{\"method\":\"account/updated\",\"params\":{\"planType\":\"pro\"}}");
                    if (scenario == "malformed") { Trace(pidFile, "malformed"); Console.WriteLine("not-json"); }
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
                line = reader.ReadLine();
            }
            Trace(pidFile, "eof");
        }
        return 0;
    }
    private static void Trace(string pidFile, string value)
    {
        if (!String.IsNullOrEmpty(pidFile)) File.AppendAllText(pidFile + ".trace", value + Environment.NewLine);
    }
}
