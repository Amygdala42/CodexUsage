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
        private int cacheSeconds = 900;
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
        internal async Task RefreshAsync(CancellationToken token, bool force = false)
        {
            if (running || token.IsCancellationRequested || (!force && DateTimeOffset.UtcNow < nextRequest)) return;
            running = true;
            DateTimeOffset previousRequest = nextRequest;
            nextRequest = DateTimeOffset.UtcNow.AddMinutes(15);
            try
            {
                string newEtag = etag;
                int newCacheSeconds = cacheSeconds;
                string json = await Task.Run(delegate {
                    var request = (HttpWebRequest)WebRequest.Create("https://codex-resets.com/api/v1/status");
                    request.Timeout = 10000; request.ReadWriteTimeout = 10000;
                    request.AllowAutoRedirect = false;
                    request.UserAgent = "CodexUsage/" + typeof(ResetFeed).Assembly.GetName().Version.ToString(3);
                    // Explicit refresh revalidates now; automatic requests keep the
                    // existing freshness/retry schedule and HTTP cache behavior.
                    if (force)
                    {
                        // Bypass the Framework cache so it cannot remove our no-cache
                        // header when there is no entry in its own HTTP cache.
                        request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
                        request.Headers[HttpRequestHeader.CacheControl] = "no-cache";
                    }
                    if (etag != null) request.Headers[HttpRequestHeader.IfNoneMatch] = etag;
                    using (token.Register(request.Abort))
                    {
                        HttpWebResponse response;
                        try { response = (HttpWebResponse)request.GetResponse(); }
                        catch (WebException error)
                        {
                            response = error.Response as HttpWebResponse;
                            if (response == null) throw;
                            // Some HTTP stacks surface 304 as ProtocolError, while Framework
                            // normally returns it. Both must take the same validation path.
                            if (response.StatusCode != HttpStatusCode.NotModified)
                            {
                                using (response)
                                {
                                    if ((int)response.StatusCode == 429 || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                                        nextRequest = RetryTime(response.Headers["Retry-After"]);
                                }
                                throw;
                            }
                        }
                        using (response)
                        {
                            bool notModified = response.StatusCode == HttpStatusCode.NotModified;
                            if ((notModified && !hasResponse) || (!notModified && response.StatusCode != HttpStatusCode.OK))
                                throw new IOException("Unexpected status");
                            newEtag = notModified ? response.Headers["ETag"] ?? etag : response.Headers["ETag"];
                            newCacheSeconds = FreshnessSeconds(response.Headers["Cache-Control"], notModified ? cacheSeconds : 900);
                            if (notModified) return null;
                            if (response.ContentLength > 262144) throw new IOException("Response too large");
                            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                            {
                                var text = new StringBuilder(); var buffer = new char[4096]; int count;
                                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                                { token.ThrowIfCancellationRequested(); text.Append(buffer, 0, count); if (text.Length > 262144) throw new IOException("Response too large"); }
                                return text.ToString();
                            }
                        }
                    }
                }, token);
                if (token.IsCancellationRequested) return;
                if (json != null)
                {
                    Latest = ResetAnnouncement.Parse(json); hasResponse = true;
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
                // Commit response metadata only after a complete, valid response. A bad
                // body must not postpone retries using an untrusted long freshness value.
                etag = newEtag; cacheSeconds = newCacheSeconds;
                nextRequest = DateTimeOffset.UtcNow.AddSeconds(cacheSeconds);
                Cached = false; Failed = false;
            }
            catch (Exception error)
            {
                if (!(error is WebException) && !(error is IOException) && !(error is ArgumentException) &&
                    !(error is InvalidOperationException) && !(error is FormatException) && !(error is OperationCanceledException)) throw;
                if (token.IsCancellationRequested) return;
                Cached = Latest != null; Failed = true;
            }
            finally
            {
                if (token.IsCancellationRequested) nextRequest = previousRequest;
                running = false;
            }
        }
        private static int FreshnessSeconds(string cacheControl, int previous)
        {
            if (cacheControl == null) return previous;
            var maxAge = System.Text.RegularExpressions.Regex.Match(cacheControl,
                @"(?:^|,)\s*max-age\s*=\s*""?(\d+)""?\s*(?:,|$)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            int seconds;
            return maxAge.Success && Int32.TryParse(maxAge.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out seconds)
                ? Math.Max(900, seconds) : 900;
        }
        private static DateTimeOffset RetryTime(string retryAfter)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset minimum = now.AddMinutes(15);
            int seconds;
            if (Int32.TryParse(retryAfter, NumberStyles.None, CultureInfo.InvariantCulture, out seconds))
                return now.AddSeconds(Math.Max(900, seconds));
            DateTimeOffset date;
            if (DateTimeOffset.TryParse(retryAfter, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date) && date > minimum)
                return date;
            return minimum;
        }
    }
}
