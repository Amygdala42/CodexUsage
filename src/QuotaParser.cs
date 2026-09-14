using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace CodexQuotaLite
{
    public static class QuotaParser
    {
        private static readonly DateTimeOffset UnixEpoch =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static QuotaSnapshot Parse(
            string accountResponseJson,
            string limitsResponseJson,
            DateTimeOffset fetchedAtUtc)
        {
            IDictionary<string, object> accountResult = ParseResultEnvelope(accountResponseJson, true);
            IDictionary<string, object> account = AsObject(GetValue(accountResult, "account"));
            if (account == null)
            {
                throw new InvalidOperationException("未检测到已登录的 ChatGPT 账号，请先在 Codex 中登录后重试。");
            }

            string accountType = GetString(account, "type");
            if (!string.Equals(accountType, "chatgpt", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(accountType, "chatgptAuthTokens", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("当前账号不能读取订阅额度，请先在 Codex 中登录 ChatGPT 账号后重试。");
            }

            IDictionary<string, object> limitsResult = ParseResultEnvelope(limitsResponseJson, false);
            QuotaSnapshot snapshot = new QuotaSnapshot();
            snapshot.PlanLabel = FormatPlanLabel(GetString(account, "planType"));
            snapshot.FetchedAtUtc = fetchedAtUtc.ToUniversalTime();

            IDictionary<string, object> buckets = AsObject(GetValue(limitsResult, "rateLimitsByLimitId"));
            if (buckets != null && buckets.Count > 0)
            {
                List<ParsedBucket> parsedBuckets = new List<ParsedBucket>();
                foreach (KeyValuePair<string, object> entry in buckets)
                {
                    IDictionary<string, object> bucketObject = AsObject(entry.Value);
                    if (bucketObject == null)
                    {
                        continue;
                    }

                    string bucketId = string.IsNullOrWhiteSpace(entry.Key)
                        ? GetString(bucketObject, "limitId")
                        : entry.Key;
                    if (string.IsNullOrWhiteSpace(bucketId))
                    {
                        continue;
                    }

                    if (IsSpark(bucketId) || IsSpark(GetString(bucketObject, "limitId")) ||
                        IsSpark(GetString(bucketObject, "limitName")))
                    {
                        continue;
                    }

                    ParsedBucket bucket = new ParsedBucket();
                    bucket.Id = bucketId.Trim();
                    bucket.Name = CleanText(GetString(bucketObject, "limitName"));
                    if (string.IsNullOrEmpty(bucket.Name))
                    {
                        bucket.Name = bucket.Id;
                    }

                    AddWindow(bucket.Windows, bucketObject, bucket.Id, bucket.Name, "primary", IsMainBucket(bucket.Id));
                    AddWindow(bucket.Windows, bucketObject, bucket.Id, bucket.Name, "secondary", IsMainBucket(bucket.Id));
                    SortWindows(bucket.Windows);
                    parsedBuckets.Add(bucket);
                }

                parsedBuckets.Sort(CompareBuckets);
                foreach (ParsedBucket bucket in parsedBuckets)
                {
                    snapshot.Windows.AddRange(bucket.Windows);
                }
            }
            else
            {
                IDictionary<string, object> legacy = AsObject(GetValue(limitsResult, "rateLimits"));
                if (legacy != null && !IsSpark(GetString(legacy, "limitId")) &&
                    !IsSpark(GetString(legacy, "limitName")))
                {
                    AddWindow(snapshot.Windows, legacy, "legacy", null, "primary", true);
                    AddWindow(snapshot.Windows, legacy, "legacy", null, "secondary", true);
                    SortWindows(snapshot.Windows);
                }
            }

            return snapshot;
        }

        private static bool IsSpark(string value)
        {
            return value != null &&
                (value.IndexOf("spark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 string.Equals(value.Trim(), "codex_bengalfox", StringComparison.OrdinalIgnoreCase));
        }

        private static IDictionary<string, object> ParseResultEnvelope(string json, bool accountEnvelope)
        {
            string safeError = accountEnvelope
                ? "无法读取 Codex 账号信息，请确认已登录后重试。"
                : "无法读取 Codex 额度，请稍后重试。";

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException(safeError);
            }

            IDictionary<string, object> envelope;
            try
            {
                envelope = new JavaScriptSerializer().DeserializeObject(json) as IDictionary<string, object>;
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException(safeError);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(safeError);
            }

            if (envelope == null ||
                (envelope.ContainsKey("error") && envelope["error"] != null))
            {
                throw new InvalidOperationException(safeError);
            }

            IDictionary<string, object> result = AsObject(GetValue(envelope, "result"));
            if (result == null)
            {
                throw new InvalidOperationException(safeError);
            }

            return result;
        }

        private static void AddWindow(
            List<QuotaWindow> destination,
            IDictionary<string, object> container,
            string bucketId,
            string bucketName,
            string slot,
            bool mainBucket)
        {
            IDictionary<string, object> value = AsObject(GetValue(container, slot));
            if (value == null)
            {
                return;
            }

            QuotaWindow window = new QuotaWindow();
            window.Id = bucketId + ":" + slot;
            window.UsedPercent = ReadPercent(GetValue(value, "usedPercent"));
            window.WindowMinutes = ReadPositiveInt(GetValue(value, "windowDurationMins"));
            window.ResetsAtUtc = ReadUnixTime(GetValue(value, "resetsAt"));
            window.Label = FormatWindowLabel(window.WindowMinutes, mainBucket, bucketName);
            destination.Add(window);
        }

        private static string FormatWindowLabel(int? minutes, bool mainBucket, string bucketName)
        {
            string longDuration;
            string compactDuration;
            if (!minutes.HasValue)
            {
                longDuration = "未知周期额度";
                compactDuration = "未知周期";
            }
            else if (minutes.Value == 300)
            {
                longDuration = "5 小时额度";
                compactDuration = "5小时";
            }
            else if (minutes.Value == 10080)
            {
                longDuration = "每周额度";
                compactDuration = "每周";
            }
            else if (minutes.Value % 1440 == 0)
            {
                int days = minutes.Value / 1440;
                longDuration = days.ToString(CultureInfo.InvariantCulture) + " 天额度";
                compactDuration = days.ToString(CultureInfo.InvariantCulture) + "天";
            }
            else if (minutes.Value % 60 == 0)
            {
                int hours = minutes.Value / 60;
                longDuration = hours.ToString(CultureInfo.InvariantCulture) + " 小时额度";
                compactDuration = hours.ToString(CultureInfo.InvariantCulture) + "小时";
            }
            else
            {
                longDuration = minutes.Value.ToString(CultureInfo.InvariantCulture) + " 分钟额度";
                compactDuration = minutes.Value.ToString(CultureInfo.InvariantCulture) + "分钟";
            }

            if (mainBucket)
            {
                return longDuration;
            }

            return bucketName + " · " + compactDuration;
        }

        private static int CompareBuckets(ParsedBucket left, ParsedBucket right)
        {
            bool leftMain = IsMainBucket(left.Id);
            bool rightMain = IsMainBucket(right.Id);
            if (leftMain != rightMain)
            {
                return leftMain ? -1 : 1;
            }

            int nameOrder = string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
            if (nameOrder != 0)
            {
                return nameOrder;
            }

            return string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        }

        private static void SortWindows(List<QuotaWindow> windows)
        {
            windows.Sort(delegate(QuotaWindow left, QuotaWindow right)
            {
                int leftRank = WindowRank(left.WindowMinutes);
                int rightRank = WindowRank(right.WindowMinutes);
                int rankOrder = leftRank.CompareTo(rightRank);
                if (rankOrder != 0)
                {
                    return rankOrder;
                }

                int leftMinutes = left.WindowMinutes.HasValue ? left.WindowMinutes.Value : int.MaxValue;
                int rightMinutes = right.WindowMinutes.HasValue ? right.WindowMinutes.Value : int.MaxValue;
                int durationOrder = leftMinutes.CompareTo(rightMinutes);
                if (durationOrder != 0)
                {
                    return durationOrder;
                }

                return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
        }

        private static int WindowRank(int? minutes)
        {
            if (minutes == 300)
            {
                return 0;
            }

            if (minutes == 10080)
            {
                return 1;
            }

            return 2;
        }

        private static bool IsMainBucket(string id)
        {
            return string.Equals(id, "codex", StringComparison.OrdinalIgnoreCase);
        }

        private static double? ReadPercent(object value)
        {
            double number;
            if (!TryReadDouble(value, out number))
            {
                return null;
            }

            if (number < 0.0)
            {
                return 0.0;
            }

            if (number > 100.0)
            {
                return 100.0;
            }

            return number;
        }

        private static int? ReadPositiveInt(object value)
        {
            double number;
            if (!TryReadDouble(value, out number) ||
                number <= 0.0 ||
                number > int.MaxValue ||
                Math.Truncate(number) != number)
            {
                return null;
            }

            return (int)number;
        }

        private static DateTimeOffset? ReadUnixTime(object value)
        {
            double number;
            if (!TryReadDouble(value, out number) ||
                Math.Truncate(number) != number ||
                number < -62135596800.0 ||
                number > 253402300799.0)
            {
                return null;
            }

            try
            {
                long seconds = Convert.ToInt64(number, CultureInfo.InvariantCulture);
                return UnixEpoch.AddTicks(checked(seconds * TimeSpan.TicksPerSecond));
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static bool TryReadDouble(object value, out double number)
        {
            number = 0.0;
            if (value == null || value is string || value is bool || value is char)
            {
                return false;
            }

            TypeCode code = Type.GetTypeCode(value.GetType());
            if (code != TypeCode.Byte && code != TypeCode.SByte &&
                code != TypeCode.UInt16 && code != TypeCode.UInt32 && code != TypeCode.UInt64 &&
                code != TypeCode.Int16 && code != TypeCode.Int32 && code != TypeCode.Int64 &&
                code != TypeCode.Single && code != TypeCode.Double && code != TypeCode.Decimal)
            {
                return false;
            }

            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return !double.IsNaN(number) && !double.IsInfinity(number);
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        private static string FormatPlanLabel(string value)
        {
            string plan = CleanText(value);
            if (string.IsNullOrEmpty(plan))
            {
                return "未知套餐";
            }

            if (string.Equals(plan, "plus", StringComparison.OrdinalIgnoreCase))
            {
                return "Plus";
            }

            if (string.Equals(plan, "pro", StringComparison.OrdinalIgnoreCase))
            {
                return "Pro";
            }

            if (string.Equals(plan, "free", StringComparison.OrdinalIgnoreCase))
            {
                return "Free";
            }

            if (string.Equals(plan, "team", StringComparison.OrdinalIgnoreCase))
            {
                return "Team";
            }

            if (string.Equals(plan, "business", StringComparison.OrdinalIgnoreCase))
            {
                return "Business";
            }

            if (string.Equals(plan, "enterprise", StringComparison.OrdinalIgnoreCase))
            {
                return "Enterprise";
            }

            return plan;
        }

        private static string CleanText(string value)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            char[] characters = new char[trimmed.Length];
            int length = 0;
            foreach (char character in trimmed)
            {
                if (!char.IsControl(character))
                {
                    characters[length++] = character;
                }
            }

            return length == 0 ? null : new string(characters, 0, length);
        }

        private static string GetString(IDictionary<string, object> source, string name)
        {
            object value = GetValue(source, name);
            return value as string;
        }

        private static object GetValue(IDictionary<string, object> source, string name)
        {
            object value;
            return source != null && source.TryGetValue(name, out value) ? value : null;
        }

        private static IDictionary<string, object> AsObject(object value)
        {
            return value as IDictionary<string, object>;
        }

        private sealed class ParsedBucket
        {
            public readonly List<QuotaWindow> Windows = new List<QuotaWindow>();
            public string Id;
            public string Name;
        }
    }
}
