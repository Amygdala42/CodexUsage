using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal static class TaskbarPlacement
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; public Rectangle Rectangle { get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); } } }
        [StructLayout(LayoutKind.Sequential)] private struct AppBarData { public uint Size; public IntPtr Window; public uint Callback; public uint Edge; public NativeRect Rect; public IntPtr Parameter; }
        [DllImport("shell32.dll")] private static extern UIntPtr SHAppBarMessage(uint message, ref AppBarData data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        private delegate bool EnumChildCallback(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumChildCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder value, int count);

        internal static bool TryRead(out Rectangle bounds, out bool visible, out Rectangle notification)
        {
            bounds = Rectangle.Empty; notification = Rectangle.Empty; visible = false;
            AppBarData data = new AppBarData(); data.Size = (uint)Marshal.SizeOf(typeof(AppBarData));
            if (SHAppBarMessage(5, ref data) == UIntPtr.Zero) return false;
            bounds = data.Rect.Rectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;
            IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
            NativeRect actual;
            if (taskbar != IntPtr.Zero && IsWindowVisible(taskbar) && GetWindowRect(taskbar, out actual))
            {
                Rectangle displayed = Rectangle.Intersect(actual.Rectangle, Screen.FromRectangle(bounds).Bounds);
                visible = displayed.Width > 4 && displayed.Height > 4;
                IntPtr tray = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
                if (tray == IntPtr.Zero)
                {
                    EnumChildWindows(taskbar, delegate(IntPtr child, IntPtr ignored) {
                        StringBuilder name = new StringBuilder(80);
                        GetClassName(child, name, name.Capacity);
                        if (name.ToString() != "TrayNotifyWnd") return true;
                        tray = child; return false;
                    }, IntPtr.Zero);
                }
                NativeRect trayRect;
                if (tray != IntPtr.Zero && GetWindowRect(tray, out trayRect))
                    notification = Rectangle.Intersect(trayRect.Rectangle, bounds);
            }
            return true;
        }

        internal static Rectangle Place(Rectangle taskbar, Size requested, Rectangle notification)
        {
            int margin = Math.Min(3, Math.Max(0, Math.Min(taskbar.Width, taskbar.Height) / 4));
            int width = Math.Max(1, Math.Min(requested.Width, taskbar.Width - margin * 2));
            int height = Math.Max(1, Math.Min(requested.Height, taskbar.Height - margin * 2));
            bool horizontal = taskbar.Width >= taskbar.Height;
            bool hasTray = notification.Width > 0 && notification.Height > 0 && taskbar.IntersectsWith(notification);
            int gap = Math.Max(4, Math.Min(12, Math.Min(taskbar.Width, taskbar.Height) / 12));
            // TrayNotifyWnd includes the chevron and notification icons on this
            // shell. Keep the fixed strip just outside its leading edge.
            int anchor = hasTray ? (horizontal ? notification.Left : notification.Top)
                : horizontal ? taskbar.Right - Math.Min(taskbar.Width / 3, taskbar.Height * 5) : taskbar.Bottom - Math.Min(taskbar.Height / 3, taskbar.Width * 5);
            int x = horizontal ? anchor - gap - width : taskbar.Left + (taskbar.Width - width) / 2;
            int y = horizontal ? taskbar.Top + (taskbar.Height - height) / 2 : anchor - gap - height;
            return Theme.Clamp(new Rectangle(x, y, width, height), Rectangle.Inflate(taskbar, -margin, -margin));
        }

        internal static bool ForegroundIsFullscreen(IntPtr widget, IntPtr details)
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero || foreground == widget || foreground == details) return false;
            StringBuilder className = new StringBuilder(64);
            GetClassName(foreground, className, className.Capacity);
            string kind = className.ToString();
            if (kind == "Shell_TrayWnd" || kind == "Progman" || kind == "WorkerW") return false;
            NativeRect native;
            if (!GetWindowRect(foreground, out native)) return false;
            Rectangle screen = Screen.FromRectangle(native.Rectangle).Bounds;
            return CoversScreen(native.Rectangle, screen, IsZoomed(foreground));
        }

        internal static bool CoversScreen(Rectangle window, Rectangle screen, bool maximized)
        {
            // With an auto-hide taskbar a normal maximized window can cover the
            // entire display. It must still permit the strip when the bar is revealed.
            return !maximized && window.Left <= screen.Left && window.Top <= screen.Top && window.Right >= screen.Right && window.Bottom >= screen.Bottom;
        }

        internal static void KeepAboveTaskbar(IntPtr window)
        {
            // Explorer can reorder its own topmost taskbar on interaction.
            // Preserve our placement without taking keyboard focus.
            SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x0013);
        }
    }

    // Out-of-context notifications run on our UI thread; no code is injected
    // into Explorer and no keyboard or mouse input is intercepted.
    internal sealed class ForegroundMonitor : IDisposable
    {
        private delegate void WinEventCallback(IntPtr hook, uint eventType, IntPtr window,
            int objectId, int childId, uint threadId, uint time);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint first, uint last, IntPtr module,
            WinEventCallback callback, uint process, uint thread, uint flags);
        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hook);
        private IntPtr hook;
        private GCHandle callbackRoot;
        private bool disposed;

        internal ForegroundMonitor(Action changed)
        {
            WinEventCallback callback = delegate {
                if (!disposed) changed();
            };
            callbackRoot = GCHandle.Alloc(callback);
            // EVENT_SYSTEM_FOREGROUND, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS.
            hook = SetWinEventHook(3, 3, IntPtr.Zero, callback, 0, 0, 2);
            // If hooks are unavailable, the existing timer remains a fallback.
            if (hook == IntPtr.Zero) callbackRoot.Free();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (hook != IntPtr.Zero)
            {
                if (UnhookWinEvent(hook) && callbackRoot.IsAllocated) callbackRoot.Free();
                hook = IntPtr.Zero;
            }
        }
    }
}
