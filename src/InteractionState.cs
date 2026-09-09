using System;

namespace CodexQuotaLite
{
    internal sealed class HoverDismissState
    {
        private DateTimeOffset? outsideSince;

        internal bool ShouldDismiss(DateTimeOffset now, bool visible, bool inside, bool menuOpen)
        {
            if (!visible || inside || menuOpen)
            {
                outsideSince = null;
                return false;
            }
            if (!outsideSince.HasValue || now < outsideSince.Value) outsideSince = now;
            return now - outsideSince.Value >= TimeSpan.FromMilliseconds(500);
        }
    }

    internal sealed class TaskbarVisibilityState
    {
        private int hiddenReadings;
        internal bool Observe(bool visible)
        {
            hiddenReadings = visible ? 0 : Math.Min(2, hiddenReadings + 1);
            return hiddenReadings < 2;
        }
    }
}
