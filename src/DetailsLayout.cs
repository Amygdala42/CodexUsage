using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    // Shared by the real details form and the non-window font/geometry checks.
    internal static class DetailsLayout
    {
        internal const string ControlFontFamily = "Microsoft YaHei UI";
        internal const float ControlFontSize = 11;
        internal const TextFormatFlags SelectorTextFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

        internal struct Row
        {
            internal Rectangle Language, Theme, Selector;
        }

        internal static float CardsHeight(int count)
        { return Math.Max(70, count * 74 + Math.Max(0, count - 1) * 8); }
        internal static float ChoiceY(int count) { return 84 + CardsHeight(count) + 48; }
        internal static float FooterY(int count) { return ChoiceY(count) + 46; }
        internal static float LogicalHeight(int count) { return FooterY(count) + 44; }

        internal static Row ChoiceRow(int count, float scale)
        {
            float top = ChoiceY(count);
            return new Row {
                Language = Rectangle.Round(new RectangleF(20 * scale, top * scale, 60 * scale, 28 * scale)),
                Theme = Rectangle.Round(new RectangleF(88 * scale, top * scale, 88 * scale, 28 * scale)),
                Selector = Rectangle.Round(new RectangleF(184 * scale, top * scale, 156 * scale, 28 * scale))
            };
        }

        internal static Rectangle SelectorTextBounds(Size size)
        {
            int pad = Math.Max(3, (int)Math.Round(size.Height * 5f / 28));
            int gap = Math.Max(2, (int)Math.Round(size.Height * 2f / 28));
            int right = (int)Math.Floor(SelectorArrowBounds(size).Left - gap);
            return new Rectangle(pad, 0, Math.Max(1, right - pad), size.Height);
        }
        internal static RectangleF SelectorArrowBounds(Size size)
        {
            float x = size.Width - size.Height * .53f, y = size.Height * .47f;
            float arrow = Math.Max(3, size.Height * .12f);
            return new RectangleF(x - arrow, y - arrow / 2, arrow * 2, arrow);
        }
        internal static int PopupWidth(Size selector, int workingWidth)
        { return Math.Min(Math.Max(1, workingWidth - 24), Math.Max(selector.Width, (int)Math.Round(232f * selector.Height / 28))); }
        internal static int ChoiceItemHeight(float scale) { return (int)(21 * scale); }
        internal static int PopupRowHeight(int fontHeight, int itemHeight)
        { return Math.Max(fontHeight + 10, itemHeight + 6); }
        internal static Rectangle PopupRowBounds(int width, int rowHeight, int row)
        { return new Rectangle(4, 4 + row * rowHeight, width - 8, rowHeight); }
        internal static Rectangle PopupTextBounds(Rectangle row)
        { return new Rectangle(row.Left + 10, row.Top, row.Width - 34, row.Height); }
        internal static Rectangle PopupCheckBounds(Rectangle row)
        { return new Rectangle(row.Right - 24, row.Top, 20, row.Height); }
        internal static string LanguageCaption() { return UiText.T("English", "中文"); }
        internal static string ThemeCaption(bool dark)
        { return dark ? UiText.T("浅色模式", "Light mode") : UiText.T("深色模式", "Dark mode"); }
    }
}
