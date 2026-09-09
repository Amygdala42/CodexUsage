using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal static class Theme
    {
        internal static readonly Color Background = Color.FromArgb(16, 23, 36);
        internal static readonly Color Card = Color.FromArgb(25, 35, 51);
        internal static readonly Color Border = Color.FromArgb(48, 63, 82);
        internal static readonly Color Text = Color.FromArgb(238, 245, 250);
        internal static readonly Color Muted = Color.FromArgb(150, 169, 188);
        internal static readonly Color Aqua = Color.FromArgb(70, 224, 196);
        internal static readonly Color Blue = Color.FromArgb(106, 163, 255);
        internal static readonly Color Warning = Color.FromArgb(245, 193, 117);

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
