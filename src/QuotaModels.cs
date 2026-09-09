using System;
using System.Collections.Generic;

namespace CodexQuotaLite
{
    public sealed class QuotaWindow
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public double? UsedPercent { get; set; }
        public int? WindowMinutes { get; set; }
        public DateTimeOffset? ResetsAtUtc { get; set; }

        public double? RemainingPercent
        {
            get
            {
                if (!UsedPercent.HasValue ||
                    double.IsNaN(UsedPercent.Value) ||
                    double.IsInfinity(UsedPercent.Value))
                {
                    return null;
                }

                return ClampPercent(100.0 - UsedPercent.Value);
            }
        }

        public double? GetTimeRemainingPercent(DateTimeOffset now)
        {
            if (!WindowMinutes.HasValue || WindowMinutes.Value <= 0 || !ResetsAtUtc.HasValue)
            {
                return null;
            }

            double remainingMinutes = (ResetsAtUtc.Value.ToUniversalTime() - now.ToUniversalTime()).TotalMinutes;
            return ClampPercent(remainingMinutes * 100.0 / WindowMinutes.Value);
        }

        public bool IsResetPending(DateTimeOffset now)
        {
            return ResetsAtUtc.HasValue && now.ToUniversalTime() >= ResetsAtUtc.Value.ToUniversalTime();
        }

        private static double ClampPercent(double value)
        {
            if (value < 0.0)
            {
                return 0.0;
            }

            if (value > 100.0)
            {
                return 100.0;
            }

            return value;
        }
    }

    public sealed class QuotaSnapshot
    {
        public string PlanLabel { get; set; }
        public List<QuotaWindow> Windows { get; set; }
        public DateTimeOffset FetchedAtUtc { get; set; }

        public QuotaSnapshot()
        {
            Windows = new List<QuotaWindow>();
        }
    }
}
