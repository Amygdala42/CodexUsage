using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;

namespace CodexQuotaLite
{
    internal static class WidgetRenderer
    {
        internal const int LogicalWidth = 86, LogicalHeight = 40;
        internal const float ValueFontSize = 14;
        internal static Color Surface { get { return Theme.WidgetSurface; } }
        internal static Color QuotaColor { get { return Theme.WidgetQuotaColor; } }
        internal static Color TimeColor { get { return Theme.WidgetTimeColor; } }

        internal static string CompactTime(QuotaWindow window, DateTimeOffset now)
        {
            if (window == null || !window.ResetsAtUtc.HasValue) return "—";
            if (window.IsResetPending(now)) return UiText.T("待更新", "Wait");
            TimeSpan left = window.ResetsAtUtc.Value - now;
            return left.TotalHours >= 1 ? left.TotalHours.ToString("0.0", CultureInfo.InvariantCulture) + "h"
                : Math.Max(1, (int)Math.Ceiling(left.TotalMinutes)).ToString(CultureInfo.InvariantCulture) + "m";
        }

        internal static Bitmap Render(Size size, QuotaWindow window, bool stale, bool busy, string error, DateTimeOffset now)
        {
            const int samples = 4;
            using (var large = new Bitmap(size.Width * samples, size.Height * samples, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(large))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float scale = size.Height / (float)LogicalHeight * samples;
                g.ScaleTransform(scale, scale);
                float width = size.Width * samples / scale;
                Theme.Rounded(g, new RectangleF(.5f, .5f, width - 1, LogicalHeight - 1), 8, Surface, Theme.WidgetBorder);
                bool pending = window != null && window.IsResetPending(now);
                DrawDisk(g, new RectangleF(7, 3, 14, 14), pending || window == null ? null : window.RemainingPercent, stale || pending ? Theme.Muted : QuotaColor);
                DrawDisk(g, new RectangleF(7, 23, 14, 14), window == null ? null : window.GetTimeRemainingPercent(now), stale ? Theme.Muted : TimeColor);
                string amount = pending ? UiText.T("待更新", "Wait") : window == null ? "—" : Theme.Percent(window.RemainingPercent);
                DrawText(g, amount, new RectangleF(28, 0, width - 35, 20), ValueFontSize, FontStyle.Bold, Theme.Text);
                string time = busy && window == null ? UiText.T("更新中", "Sync") : !String.IsNullOrEmpty(error) ? UiText.T("失败", "Retry") : stale ? UiText.T("已过期", "Stale") : CompactTime(window, now);
                DrawText(g, time, new RectangleF(28, 20, width - 35, 20), ValueFontSize, FontStyle.Bold, stale || !String.IsNullOrEmpty(error) ? Theme.Warning : TimeColor);
                g.ResetTransform();
                return Reduce(large, size);
            }
        }

        internal static Bitmap RenderGlyph(int size)
        {
            using (var large = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(large))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(size / 8f, size / 8f);
                using (var brush = new SolidBrush(Surface)) g.FillEllipse(brush, .5f, .5f, 31, 31);
                DrawDisk(g, new RectangleF(10, 3, 12, 12), 75, QuotaColor);
                DrawDisk(g, new RectangleF(10, 17, 12, 12), 200 / 3.6, TimeColor);
                g.ResetTransform();
                return Reduce(large, new Size(size, size));
            }
        }

        private static Bitmap Reduce(Bitmap source, Size size)
        {
            var result = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(result))
            using (var attributes = new ImageAttributes())
            {
                g.Clear(Color.Transparent); g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source, new Rectangle(Point.Empty, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return result;
        }

        private static void DrawDisk(Graphics g, RectangleF bounds, double? percent, Color color)
        {
            using (var brush = new SolidBrush(Theme.Border)) g.FillEllipse(brush, bounds);
            if (!percent.HasValue || percent.Value <= 0) return;
            using (var brush = new SolidBrush(color))
            {
                if (percent.Value >= 100) g.FillEllipse(brush, bounds);
                else g.FillPie(brush, bounds.X, bounds.Y, bounds.Width, bounds.Height, -90, (float)percent.Value * 3.6f);
            }
        }

        private static void DrawText(Graphics g, string text, RectangleF bounds, float size, FontStyle style, Color color)
        {
            using (var font = new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat(StringFormat.GenericTypographic))
            {
                format.FormatFlags = StringFormatFlags.NoWrap; format.Trimming = StringTrimming.EllipsisCharacter;
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Far;
                g.DrawString(text, font, brush, bounds, format);
            }
        }
    }
}
