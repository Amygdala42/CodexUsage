using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal static class Theme
    {
        private static readonly Palette Dark = new Palette("dark",
            Color.FromArgb(16, 23, 36), Color.FromArgb(25, 35, 51), Color.FromArgb(48, 63, 82),
            Color.FromArgb(238, 245, 250), Color.FromArgb(150, 169, 188), Color.FromArgb(70, 224, 196),
            Color.FromArgb(106, 163, 255), Color.FromArgb(245, 193, 117),
            Color.FromArgb(24, 31, 39), Color.FromArgb(91, 165, 245), Color.FromArgb(63, 137, 232), Color.FromArgb(66, 77, 86));
        private static readonly Palette Light = new Palette("light",
            Color.FromArgb(229, 229, 229), Color.FromArgb(239, 239, 239), Color.FromArgb(216, 224, 234),
            Color.FromArgb(31, 42, 55), Color.FromArgb(79, 96, 114), Color.FromArgb(0, 107, 97),
            Color.FromArgb(32, 86, 170), Color.FromArgb(139, 82, 0),
            Color.FromArgb(232, 232, 232), Color.FromArgb(43, 103, 183), Color.FromArgb(32, 82, 153), Color.FromArgb(174, 185, 199));
        private static Palette current = Dark;

        internal static void Apply(string mode) { current = mode == "light" ? Light : Dark; }
        internal static string Mode { get { return current.Mode; } }
        internal static bool IsDark { get { return current == Dark; } }
        internal static Color Background { get { return current.Background; } }
        internal static Color Card { get { return current.Card; } }
        internal static Color Border { get { return current.Border; } }
        internal static Color Text { get { return current.Text; } }
        internal static Color Muted { get { return current.Muted; } }
        internal static Color Aqua { get { return current.Aqua; } }
        internal static Color Blue { get { return current.Blue; } }
        internal static Color Warning { get { return current.Warning; } }
        internal static Color WidgetSurface { get { return current.WidgetSurface; } }
        internal static Color WidgetQuotaColor { get { return current.WidgetQuotaColor; } }
        internal static Color WidgetTimeColor { get { return current.WidgetTimeColor; } }
        internal static Color WidgetBorder { get { return current.WidgetBorder; } }

        private sealed class Palette
        {
            internal readonly string Mode;
            internal readonly Color Background, Card, Border, Text, Muted, Aqua, Blue, Warning;
            internal readonly Color WidgetSurface, WidgetQuotaColor, WidgetTimeColor, WidgetBorder;
            internal Palette(string mode, Color background, Color card, Color border, Color text, Color muted,
                Color aqua, Color blue, Color warning, Color widgetSurface, Color widgetQuotaColor, Color widgetTimeColor, Color widgetBorder)
            {
                Mode = mode; Background = background; Card = card; Border = border; Text = text; Muted = muted;
                Aqua = aqua; Blue = blue; Warning = warning; WidgetSurface = widgetSurface;
                WidgetQuotaColor = widgetQuotaColor; WidgetTimeColor = widgetTimeColor; WidgetBorder = widgetBorder;
            }
        }

        internal static GraphicsPath Round(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static void Rounded(Graphics g, RectangleF bounds, float radius, Color color, Color? border)
        {
            using (GraphicsPath path = Round(bounds, radius))
            {
                using (Brush brush = new SolidBrush(color)) g.FillPath(brush, path);
                if (border.HasValue) using (Pen pen = new Pen(border.Value)) g.DrawPath(pen, path);
            }
        }

        internal static void Write(Graphics g, string value, float x, float y, float width, float height, float size, Color color, bool bold, float scale)
        {
            using (Font font = new Font("Microsoft YaHei UI", size * scale, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
            {
                TextRenderer.DrawText(g, value ?? "", font, Rectangle.Round(new RectangleF(x * scale, y * scale, width * scale, height * scale)), color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
        }

        internal static Rectangle Clamp(Rectangle bounds, Rectangle area)
        {
            bounds.Width = Math.Min(bounds.Width, area.Width);
            bounds.Height = Math.Min(bounds.Height, area.Height);
            bounds.X = Math.Max(area.Left, Math.Min(bounds.X, area.Right - bounds.Width));
            bounds.Y = Math.Max(area.Top, Math.Min(bounds.Y, area.Bottom - bounds.Height));
            return bounds;
        }

        internal static string ResetText(QuotaWindow window, DateTimeOffset now)
        {
            if (window == null || !window.ResetsAtUtc.HasValue) return UiText.T("重置时间未知", "Reset unknown");
            if (window.IsResetPending(now)) return UiText.T("已到重置时间 · 待更新", "Reset reached · Pending");
            TimeSpan left = window.ResetsAtUtc.Value - now;
            if (UiText.English)
            {
                if (left.TotalDays >= 1) return String.Format("Resets in {0}d {1}h", (int)left.TotalDays, left.Hours);
                if (left.TotalHours >= 1) return String.Format("Resets in {0}h {1}m", (int)left.TotalHours, left.Minutes);
                return String.Format("Resets in {0}m", Math.Max(1, (int)Math.Ceiling(left.TotalMinutes)));
            }
            if (left.TotalDays >= 1) return String.Format("{0}天{1}小时后重置", (int)left.TotalDays, left.Hours);
            if (left.TotalHours >= 1) return String.Format("{0}小时{1}分后重置", (int)left.TotalHours, left.Minutes);
            return String.Format("{0}分钟后重置", Math.Max(1, (int)Math.Ceiling(left.TotalMinutes)));
        }

        internal static string Percent(double? value) { return value.HasValue ? value.Value.ToString("0") + "%" : "—"; }

        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);
        internal static Icon CreateIcon()
        {
            using (Bitmap bitmap = WidgetRenderer.RenderGlyph(32))
            {
                IntPtr handle = bitmap.GetHicon();
                try { using (Icon icon = Icon.FromHandle(handle)) return (Icon)icon.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
    }
}
