using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CodexQuotaLite
{
    internal sealed class ResetAnnouncement
    {
        internal DateTimeOffset Time;
        internal string Type;
        internal string Url;
        internal static ResetAnnouncement Parse(string json)
        {
            var root = new JavaScriptSerializer { MaxJsonLength = 262144 }.DeserializeObject(json) as IDictionary<string, object>;
            var data = Object(root, "data");
            if (data == null || !data.ContainsKey("latest_reset")) throw new FormatException("Missing reset data");
            if (data["latest_reset"] == null) return null;
            var reset = Object(data, "latest_reset");
            DateTimeOffset time;
            if (reset == null || !DateTimeOffset.TryParse(Text(reset, "announced_at"), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out time)) throw new FormatException("Invalid reset time");
            string type = Text(reset, "reset_type");
            if (type != "regular" && type != "banked") type = "unknown";
            string url = Text(Object(reset, "source"), "url");
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                !String.IsNullOrEmpty(uri.UserInfo) || (uri.Host != "x.com" && uri.Host != "codex-resets.com"))
                url = "https://codex-resets.com/";
            return new ResetAnnouncement { Time = time.ToUniversalTime(), Type = type, Url = url };
        }
        private static IDictionary<string, object> Object(IDictionary<string, object> source, string key)
        { object value; return source != null && source.TryGetValue(key, out value) ? value as IDictionary<string, object> : null; }
        private static string Text(IDictionary<string, object> source, string key)
        { object value; return source != null && source.TryGetValue(key, out value) ? value as string : null; }
        internal string Caption(bool english, bool cached)
        {
            string kind = Type == "regular" ? (english ? "Regular" : "即时重置") :
                Type == "banked" ? (english ? "Banked" : "赠送重置次数") : (english ? "Unknown" : "类型未知");
            return (english ? "Latest reset  " : "最近重置公告  ") + Time.ToLocalTime().ToString("MM-dd HH:mm") +
                "  " + kind + (cached ? (english ? " · cached" : " · 缓存") : "");
        }
    }

    internal sealed class ResetFeed
    {
        private readonly string cachePath;
        private string etag;
        private bool running;
        private bool hasResponse;
        private DateTimeOffset nextRequest;
        internal ResetAnnouncement Latest { get; private set; }
        internal bool Cached { get; private set; }
        internal bool Failed { get; private set; }
        internal ResetFeed(string path)
        {
            cachePath = path;
            try { if (File.Exists(path)) { Latest = ResetAnnouncement.Parse(File.ReadAllText(path)); Cached = true; } }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (FormatException) { }
        }
        internal async Task RefreshAsync(CancellationToken token)
        {
            if (running || token.IsCancellationRequested || DateTimeOffset.UtcNow < nextRequest) return;
            running = true;
            nextRequest = DateTimeOffset.UtcNow.AddMinutes(15);
            try
            {
                string newEtag = etag;
                string json = await Task.Run(delegate {
                    var request = (HttpWebRequest)WebRequest.Create("https://codex-resets.com/api/v1/status");
                    request.Timeout = 10000; request.ReadWriteTimeout = 10000;
                    request.AllowAutoRedirect = false;
                    request.UserAgent = "CodexUsage/1.0.3";
                    if (etag != null) request.Headers[HttpRequestHeader.IfNoneMatch] = etag;
                    using (token.Register(request.Abort))
                    {
                        try
                        {
                            using (var response = (HttpWebResponse)request.GetResponse())
                            {
                                if (response.StatusCode != HttpStatusCode.OK) throw new IOException("Unexpected status");
                                if (response.ContentLength > 262144) throw new IOException("Response too large");
                                newEtag = response.Headers["ETag"];
                                var maxAge = System.Text.RegularExpressions.Regex.Match(response.Headers["Cache-Control"] ?? "", @"(?:^|,)\s*max-age=(\d+)");
                                int seconds;
                                if (maxAge.Success && Int32.TryParse(maxAge.Groups[1].Value, out seconds))
                                    nextRequest = DateTimeOffset.UtcNow.AddSeconds(Math.Max(900, seconds));
                                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    var text = new StringBuilder(); var buffer = new char[4096]; int count;
                                    while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                                    { token.ThrowIfCancellationRequested(); text.Append(buffer, 0, count); if (text.Length > 262144) throw new IOException("Response too large"); }
                                    return text.ToString();
                                }
                            }
                        }
                        catch (WebException error)
                        {
                            using (var response = error.Response as HttpWebResponse)
                            {
                                if (response != null && response.StatusCode == HttpStatusCode.NotModified && hasResponse) return null;
                                if (response != null && (int)response.StatusCode == 429)
                                {
                                    int seconds;
                                    if (Int32.TryParse(response.Headers["Retry-After"], out seconds) && seconds > 0)
                                        nextRequest = DateTimeOffset.UtcNow.AddSeconds(Math.Max(900, seconds));
                                }
                            }
                            throw;
                        }
                    }
                }, token);
                if (token.IsCancellationRequested) return;
                if (json != null)
                {
                    Latest = ResetAnnouncement.Parse(json); etag = newEtag; hasResponse = true;
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                        string temp = cachePath + ".tmp";
                        File.WriteAllText(temp, json, new UTF8Encoding(false));
                        if (File.Exists(cachePath)) File.Replace(temp, cachePath, null); else File.Move(temp, cachePath);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                Cached = false; Failed = false;
            }
            catch (Exception error)
            {
                if (!(error is WebException) && !(error is IOException) && !(error is ArgumentException) &&
                    !(error is InvalidOperationException) && !(error is FormatException) && !(error is OperationCanceledException)) throw;
                Cached = Latest != null; Failed = true;
            }
            finally { running = false; }
        }
    }
}
