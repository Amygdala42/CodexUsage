using System;
using System.Collections.Generic;
using CodexQuotaLite;

// Native reads/writes and UI posting are injected; no windows or controls exist.
internal static class TaskbarStackingTests
{
    private static int passed, failed;
    private static readonly IntPtr Widget = new IntPtr(10), Shell = new IntPtr(20), Desktop = new IntPtr(30);
    public static int Main()
    {
        Run("100 stable fallback ticks perform zero native writes", delegate {
            var view = new DesktopState(); view.Order(Widget, Shell);
            for (int i = 0; i < 100; i++) view.Repair();
            Equal(0, view.Writes, "already above Shell");
        });
        Run("unrelated windows above the widget do not cause writes", delegate {
            var view = new DesktopState(); view.Order(new IntPtr(99), Widget, Shell); view.Repair(); Equal(0, view.Writes, "not every higher window is the taskbar");
        });
        Run("Shell reordering is repaired immediately once", delegate {
            var view = new DesktopState(); view.Order(Shell, Widget); view.Repair(); Equal(1, view.Writes, "repair before any timer"); view.Repair(); Equal(1, view.Writes, "repair feedback must not write again");
        });
        Run("failed writes remain retryable", delegate {
            var view = new DesktopState(); view.Order(Shell, Widget); view.WriteSucceeds = false;
            Check(!view.Repair(), "failed result is reported"); Equal(1, view.Writes, "first attempt");
            view.WriteSucceeds = true; Check(view.Repair(), "next observation retries"); view.Repair(); Equal(2, view.Writes, "only failed attempt and successful retry");
        });
        Run("successful return without changed order does not suppress retry", delegate {
            var view = new DesktopState(); view.Order(Shell, Widget); view.UpdateOrderOnWrite = false; view.Repair(); view.Repair(); Equal(2, view.Writes, "recheck actual order each time");
        });
        Run("missing taskbar does not guess", delegate {
            var view = new DesktopState(); view.Taskbar = IntPtr.Zero; view.Repair(); Equal(0, view.Writes, "unknown Shell");
        });
        Run("null widget does not read or write", delegate {
            int reads = 0, writes = 0;
            TaskbarStacking.Repair(IntPtr.Zero, delegate { reads++; return Shell; }, delegate(IntPtr w) { reads++; return Shell; }, delegate(IntPtr w) { writes++; return true; });
            Equal(0, reads + writes, "invalid target");
        });
        Run("an incomplete order does not guess", delegate {
            var view = new DesktopState(); view.Order(Widget); view.Repair(); Equal(0, view.Writes, "Shell not observed above widget");
        });
        Run("cyclic native reads terminate without writes", delegate {
            var view = new DesktopState(); view.Previous[Widget] = new IntPtr(99); view.Previous[new IntPtr(99)] = Widget; view.Repair();
            Equal(0, view.Writes, "cyclic observation"); Check(view.Reads <= 3, "cycle stops promptly");
        });
        Run("unbounded native chains terminate without writes", delegate {
            int reads = 0, writes = 0;
            TaskbarStacking.Repair(Widget, delegate { return Shell; }, delegate(IntPtr w) { reads++; return new IntPtr(100 + reads); }, delegate(IntPtr w) { writes++; return true; });
            Check(reads > 0 && reads <= 4096, "bounded traversal"); Equal(0, writes, "no inferred Shell");
        });
        Run("replacement Shell handle is discovered on the next check", delegate {
            var view = new DesktopState(); view.Order(Widget, Shell); view.Repair();
            view.Taskbar = new IntPtr(21); view.Order(view.Taskbar, Widget); view.Repair(); Equal(1, view.Writes, "new Shell is respected");
        });
        Run("Shell changing during observation defers repair", delegate {
            int finds = 0, writes = 0;
            TaskbarStacking.Repair(Widget, delegate { return ++finds == 1 ? Shell : new IntPtr(21); }, delegate(IntPtr w) { return Shell; }, delegate(IntPtr w) { writes++; return true; });
            Equal(0, writes, "obsolete Shell observation");
        });
        Run("only desktop client self reorder notifications are accepted", delegate {
            Check(TaskbarStacking.IsDesktopReorder(0x8004, Desktop, Desktop, -4, 0), "observed desktop OBJID_CLIENT event");
            Check(!TaskbarStacking.IsDesktopReorder(0x8004, Shell, Desktop, -4, 0), "child-container reorder is unrelated");
            Check(!TaskbarStacking.IsDesktopReorder(0x8004, Desktop, Desktop, 0, 0), "wrong object");
            Check(!TaskbarStacking.IsDesktopReorder(0x8004, Desktop, Desktop, -4, 1), "wrong child");
            Check(!TaskbarStacking.IsDesktopReorder(3, Desktop, Desktop, -4, 0), "foreground uses its existing listener");
            Check(!TaskbarStacking.IsDesktopReorder(0x8004, IntPtr.Zero, IntPtr.Zero, -4, 0), "unknown desktop");
        });
        Run("a burst of Shell events posts one immediate repair", delegate {
            var view = new DesktopState(); view.Order(Shell, Widget); var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return true; }, posted.Enqueue, delegate { view.Repair(); });
            for (int i = 0; i < 100; i++) queue.Request();
            Equal(1, posted.Count, "coalesced UI work"); Equal(0, view.Writes, "native writes wait for UI dispatch");
            posted.Dequeue()(); Equal(1, view.Writes, "repair without a timer tick");
        });
        Run("queued repair feedback never repeats an already successful write", delegate {
            var view = new DesktopState(); view.Order(Shell, Widget); var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return true; }, posted.Enqueue, delegate { view.Repair(); });
            queue.Request(); posted.Dequeue()(); queue.Request(); posted.Dequeue()(); Equal(1, view.Writes, "already above Shell");
        });
        Run("ineligible repairs do not post or write", delegate {
            int writes = 0; var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return false; }, posted.Enqueue, delegate { writes++; });
            queue.Request(); Equal(0, posted.Count + writes, "ineligible window");
        });
        Run("state changes after posting are rechecked before writing", delegate {
            bool allowed = true; int repairs = 0; var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return allowed; }, posted.Enqueue, delegate { repairs++; });
            queue.Request(); allowed = false; posted.Dequeue()(); Equal(0, repairs, "pending shutdown or hiding");
            allowed = true; queue.Request(); posted.Dequeue()(); Equal(1, repairs, "pending flag is cleared after a skipped callback");
        });
        Run("closing a menu permits repair after suppressed events", delegate {
            bool menuOpen = true; var view = new DesktopState(); view.Order(Shell, Widget); var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return !menuOpen; }, posted.Enqueue, delegate { view.Repair(); });
            queue.Request(); Equal(0, posted.Count, "do not raise over a menu");
            menuOpen = false; queue.Request(); posted.Dequeue()(); Equal(1, view.Writes, "closed menu can resume repair");
        });
        Run("a failed UI post leaves future repair retryable", delegate {
            bool failPost = true; int repairs = 0; var posted = new Queue<Action>();
            var queue = new TaskbarStackingQueue(delegate { return true; }, delegate(Action work) {
                if (failPost) throw new InvalidOperationException("handle no longer available"); posted.Enqueue(work);
            }, delegate { repairs++; });
            queue.Request(); failPost = false; queue.Request(); Equal(1, posted.Count, "retry posts again"); posted.Dequeue()(); Equal(1, repairs, "retry repaired");
        });
        Run("an event during repair is queued without recursive writes", delegate {
            int depth = 0, maxDepth = 0, repairs = 0; var posted = new Queue<Action>(); TaskbarStackingQueue queue = null;
            queue = new TaskbarStackingQueue(delegate { return true; }, posted.Enqueue, delegate {
                depth++; maxDepth = Math.Max(maxDepth, depth); repairs++; if (repairs == 1) queue.Request(); depth--;
            });
            queue.Request(); posted.Dequeue()(); Equal(1, posted.Count, "new event retained"); posted.Dequeue()();
            Equal(2, repairs, "both observations handled"); Equal(1, maxDepth, "no recursive native write");
        });
        Console.WriteLine("TaskbarStacking: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
    private sealed class DesktopState
    {
        internal IntPtr Taskbar = Shell;
        internal readonly Dictionary<IntPtr, IntPtr> Previous = new Dictionary<IntPtr, IntPtr>();
        internal int Writes, Reads;
        internal bool WriteSucceeds = true, UpdateOrderOnWrite = true;
        internal void Order(params IntPtr[] windows)
        {
            Previous.Clear(); IntPtr previous = IntPtr.Zero;
            foreach (IntPtr window in windows) { Previous[window] = previous; previous = window; }
        }
        internal bool Repair()
        {
            return TaskbarStacking.Repair(Widget, delegate { return Taskbar; }, delegate(IntPtr window) {
                Reads++; IntPtr previous; return Previous.TryGetValue(window, out previous) ? previous : IntPtr.Zero;
            }, delegate(IntPtr window) {
                Check(window == Widget, "only our widget may be changed"); Writes++;
                if (WriteSucceeds && UpdateOrderOnWrite) Order(Widget, Taskbar);
                return WriteSucceeds;
            });
        }
    }
    private static void Equal(int expected, int actual, string why) { Check(expected == actual, why + ": expected " + expected + ", got " + actual); }
    private static void Check(bool value, string why) { if (!value) throw new InvalidOperationException(why); }
    private static void Run(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); } }
}
