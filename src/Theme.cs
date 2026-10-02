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
            Color.FromArgb(20, 27, 38), Color.FromArgb(29, 40, 56), Color.FromArgb(43, 58, 78),
            Color.FromArgb(230, 237, 245), Color.FromArgb(164, 178, 196), Color.FromArgb(58, 190, 215),
            Color.FromArgb(51, 154, 197), Color.FromArgb(230, 182, 106),
            Color.FromArgb(24, 34, 48), Color.FromArgb(58, 190, 215), Color.FromArgb(51, 154, 197), Color.FromArgb(51, 154, 197), Color.FromArgb(75, 94, 119));
        private static readonly Palette Light = new Palette("light",
            Color.FromArgb(229, 229, 229), Color.FromArgb(239, 239, 239), Color.FromArgb(214, 220, 229),
            Color.FromArgb(35, 48, 68), Color.FromArgb(82, 97, 118), Color.FromArgb(47, 104, 70),
            Color.FromArgb(53, 107, 76), Color.FromArgb(132, 82, 12),
            Color.FromArgb(232, 232, 232), Color.FromArgb(37, 102, 59), Color.FromArgb(75, 136, 95), Color.FromArgb(53, 107, 76), Color.FromArgb(167, 179, 196));
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
        internal static Color WidgetTimeTextColor { get { return current.WidgetTimeTextColor; } }
        internal static Color WidgetBorder { get { return current.WidgetBorder; } }

        private sealed class Palette
        {
            internal readonly string Mode;
            internal readonly Color Background, Card, Border, Text, Muted, Aqua, Blue, Warning;
            internal readonly Color WidgetSurface, WidgetQuotaColor, WidgetTimeColor, WidgetTimeTextColor, WidgetBorder;
            internal Palette(string mode, Color background, Color card, Color border, Color text, Color muted,
                Color aqua, Color blue, Color warning, Color widgetSurface, Color widgetQuotaColor, Color widgetTimeColor, Color widgetTimeTextColor, Color widgetBorder)
            {
                Mode = mode; Background = background; Card = card; Border = border; Text = text; Muted = muted;
                Aqua = aqua; Blue = blue; Warning = warning; WidgetSurface = widgetSurface;
                WidgetQuotaColor = widgetQuotaColor; WidgetTimeColor = widgetTimeColor; WidgetTimeTextColor = widgetTimeTextColor; WidgetBorder = widgetBorder;
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
