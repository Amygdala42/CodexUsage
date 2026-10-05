using System;

namespace CodexQuotaLite
{
    internal sealed class HoverDismissState
    {
        private DateTimeOffset? outsideSince;
        private bool keyboardInteraction;

        internal void KeyboardUsed() { keyboardInteraction = true; outsideSince = null; }
        internal void PointerUsed() { if (keyboardInteraction) outsideSince = null; keyboardInteraction = false; }
        internal void Reset() { keyboardInteraction = false; outsideSince = null; }

        internal bool ShouldDismiss(DateTimeOffset now, bool visible, bool inside, bool menuOpen)
        { return ShouldDismiss(now, visible, inside, menuOpen, false); }

        internal bool ShouldDismiss(DateTimeOffset now, bool visible, bool inside, bool menuOpen, bool keyboardFocused)
        {
            if (!visible) Reset();
            // ShowAnchored activates the form for mouse users too. Focus holds it
            // open only after actual keyboard input, including dropdown navigation.
            if (keyboardInteraction && keyboardFocused)
            {
                outsideSince = null;
                return false;
            }
            if (!keyboardFocused) keyboardInteraction = false;
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
