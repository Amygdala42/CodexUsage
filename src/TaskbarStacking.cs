using System;
using System.Collections.Generic;

namespace CodexQuotaLite
{
    internal static class TaskbarStacking
    {
        internal static bool Repair(IntPtr widget, Func<IntPtr> findTaskbar,
            Func<IntPtr, IntPtr> previousWindow, Func<IntPtr, bool> raise)
        {
            if (widget == IntPtr.Zero) return false;
            IntPtr taskbar = findTaskbar();
            if (taskbar == IntPtr.Zero || taskbar == widget) return false;
            var visited = new HashSet<IntPtr>();
            visited.Add(widget);
            IntPtr current = widget;
            for (int count = 0; count < 4096; count++)
            {
                current = previousWindow(current);
                if (current == IntPtr.Zero || !visited.Add(current)) return false;
                if (current == taskbar)
                {
                    // Explorer may have replaced its taskbar while we were reading.
                    return findTaskbar() == taskbar && raise(widget);
                }
            }
            return false;
        }
        internal static bool IsDesktopReorder(uint eventType, IntPtr window, IntPtr desktop, int objectId, int childId)
        { return eventType == 0x8004 && desktop != IntPtr.Zero && window == desktop && objectId == -4 && childId == 0; }
    }

    internal sealed class TaskbarStackingQueue
    {
        private readonly Func<bool> eligible;
        private readonly Action<Action> post;
        private readonly Action repair;
        private bool queued;
        internal TaskbarStackingQueue(Func<bool> eligible, Action<Action> post, Action repair)
        { this.eligible = eligible; this.post = post; this.repair = repair; }
        internal void Request()
        {
            if (queued || !eligible()) return;
            queued = true;
            try
            {
                post(delegate {
                    queued = false;
                    if (eligible()) repair();
                });
            }
            catch (InvalidOperationException) { queued = false; }
        }
    }
}
