using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CodexQuotaLite;

// Pure geometry and GDI font measurements. No Form, Control or tray is created.
internal static class DetailsLayoutTests
{
    private static readonly float[] Scales = { .75f, 1f, 1.25f, 1.5f, 1.75f, 2f };
    private static Type helper;
    private static int passed, failed;
    private static int minimumSpare = Int32.MaxValue;
    private static readonly List<string> measurements = new List<string>();

    public static int Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout-compact");
        Directory.CreateDirectory(output);
        helper = typeof(UiText).Assembly.GetType("CodexQuotaLite.DetailsLayout");
        Run("the details page provides its production geometry without creating controls", delegate {
            Check(helper != null, "production DetailsLayout helper is missing");
        });
        if (helper == null) return Finish(output);
        foreach (float scaleValue in Scales)
        {
            float scale = scaleValue;
            foreach (int countValue in new[] { 0, 1, 2, 4, 7, 12 })
            {
                int count = countValue;
                Run("one row, margins and compact footer: count=" + count + " scale=" + scale, delegate {
                    object row = Call("ChoiceRow", count, scale);
                    Rectangle language = Bounds(row, "Language"), theme = Bounds(row, "Theme"), selector = Bounds(row, "Selector");
                    Check(language.Top == theme.Top && theme.Top == selector.Top, "three controls must share a row");
                    Check(language.Height == theme.Height && theme.Height == selector.Height && selector.Height == (int)Math.Round(28 * scale), "common 28-pixel logical height");
                    Check(language.Left >= Math.Floor(20 * scale), "left margin");
                    Check(selector.Right <= Math.Floor(360 * scale) - Math.Floor(20 * scale), "right margin");
                    Check(theme.Left - language.Right >= Math.Floor(8 * scale) && selector.Left - theme.Right >= Math.Floor(8 * scale), "eight-pixel logical gaps");
                    float footer = (float)Call("FooterY", count), height = (float)Call("LogicalHeight", count);
                    Check(Math.Abs(footer * scale - selector.Top - 46 * scale) <= 1, "footer follows the same row without the old extra height");
                    Check(height > footer && selector.Bottom < footer * scale, "controls fit above the footer");
                    Rectangle text = (Rectangle)Call("SelectorTextBounds", selector.Size);
                    RectangleF arrow = (RectangleF)Call("SelectorArrowBounds", selector.Size);
                    Check(text.Left >= Math.Floor(4 * scale) && arrow.Left - text.Right >= Math.Floor(2 * scale) && text.Top >= 0 && text.Bottom <= selector.Height, "text has border padding and a visible gap before the arrow");
                    Check(arrow.Left >= 0 && arrow.Right <= selector.Width && arrow.Top >= 0 && arrow.Bottom <= selector.Height, "arrow stays inside the selector");
                });
            }
            foreach (string planValue in new[] { "Plus", "Pro", "Pro multi-bucket" })
            foreach (string languageValue in new[] { "zh", "en" })
            foreach (bool darkValue in new[] { true, false })
            {
                string plan = planValue, language = languageValue; bool dark = darkValue;
                Run(plan + " " + language + " " + (dark ? "Light mode" : "Dark mode") + " scale=" + scale, delegate {
                    UiText.Language = language;
                    QuotaSnapshot snapshot = Sample(plan);
                    object row = Call("ChoiceRow", snapshot.Windows.Count, scale);
                    Rectangle languageBounds = Bounds(row, "Language"), themeBounds = Bounds(row, "Theme"), selectorBounds = Bounds(row, "Selector");
                    string family = (string)helper.GetField("ControlFontFamily", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                    float size = (float)helper.GetField("ControlFontSize", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                    using (var bitmap = new Bitmap(32, 32))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    using (var font = new Font(family, size * scale, FontStyle.Regular, GraphicsUnit.Pixel))
                    {
                        string context = plan + "," + language + "," + (dark ? "dark" : "light") + "," + scale.ToString(CultureInfo.InvariantCulture);
                        // MeasureText includes normal GDI glyph padding for native buttons;
                        // also reserve four logical pixels per side for their border/content inset.
                        int inset = (int)Math.Ceiling(4 * scale);
                        Measure(graphics, font, (string)Call("LanguageCaption"), new Size(languageBounds.Width - 2 * inset, languageBounds.Height - 2 * inset), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix, context + ",language");
                        Measure(graphics, font, (string)Call("ThemeCaption", dark), new Size(themeBounds.Width - 2 * inset, themeBounds.Height - 2 * inset), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix, context + ",theme");
                        Rectangle textBounds = (Rectangle)Call("SelectorTextBounds", selectorBounds.Size);
                        int popupWidth = (int)Call("PopupWidth", selectorBounds.Size, 1920);
                        int itemHeight = (int)Call("ChoiceItemHeight", scale);
                        int rowHeight = (int)Call("PopupRowHeight", font.Height, itemHeight);
                        Rectangle popupRow = (Rectangle)Call("PopupRowBounds", popupWidth, rowHeight, 0);
                        Rectangle popupText = (Rectangle)Call("PopupTextBounds", popupRow);
                        Rectangle popupCheck = (Rectangle)Call("PopupCheckBounds", popupRow);
                        Check(popupText.Left > popupRow.Left && popupText.Right <= popupCheck.Left && popupCheck.Right < popupRow.Right, "popup reserves padding and a separate checkmark region");
                        foreach (QuotaWindow window in snapshot.Windows)
                        {
                            Measure(graphics, font, UiText.WindowLabel(window.Label), textBounds.Size, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding, context + "," + window.Id);
                            Measure(graphics, font, UiText.WindowLabel(window.Label), popupText.Size, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding, context + ",popup-" + window.Id);
                        }
                    }
                });
            }
            Run("unknown long labels keep bounded ellipsis instead of smaller type at scale=" + scale, delegate {
                object row = Call("ChoiceRow", 2, scale);
                Rectangle selector = Bounds(row, "Selector");
                Rectangle text = (Rectangle)Call("SelectorTextBounds", selector.Size);
                TextFormatFlags flags = (TextFormatFlags)helper.GetField("SelectorTextFlags", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Check((flags & TextFormatFlags.EndEllipsis) != 0 && (flags & TextFormatFlags.SingleLine) != 0, "the production selector retains single-line ellipsis");
                string family = (string)helper.GetField("ControlFontFamily", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                float size = (float)helper.GetField("ControlFontSize", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                using (var bitmap = new Bitmap(selector.Width, selector.Height))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (var font = new Font(family, size * scale, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    string custom = "Unknown custom model with an intentionally very long quota window name";
                    Size required = TextRenderer.MeasureText(graphics, custom, font, new Size(Int32.MaxValue, Int32.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                    Check(required.Width > text.Width, "fixture must need truncation");
                    graphics.Clear(Color.White);
                    TextRenderer.DrawText(graphics, custom, font, text, Color.Black, flags);
                    int ink = 0;
                    for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
                    {
                        if (bitmap.GetPixel(x, y).ToArgb() == Color.White.ToArgb()) continue;
                        ink++;
                        Check(text.Contains(x, y), "long label drawing must not spill into the arrow or padding");
                    }
                    Check(ink > 0 && font.Size == size * scale, "truncated text remains visible at the normal font size");
                }
            });
            Run("popup width respects narrow work areas at scale=" + scale, delegate {
                Rectangle selector = Bounds(Call("ChoiceRow", 2, scale), "Selector");
                int width = (int)Call("PopupWidth", selector.Size, 180);
                Check(width > 0 && width <= 180 - 24, "popup stays inside the available working width");
            });
        }
        UiText.Language = "zh";
        return Finish(output);
    }

    private static QuotaSnapshot Sample(string plan)
    {
        string account = "{\"result\":{\"account\":{\"type\":\"chatgpt\",\"planType\":\"" + (plan == "Plus" ? "plus" : "pro") + "\"}}}";
        string primary = "{\"usedPercent\":25,\"windowDurationMins\":300,\"resetsAt\":1900000000}";
        string weekly = "{\"usedPercent\":37,\"windowDurationMins\":10080,\"resetsAt\":1900000000}";
        string codex = plan == "Pro" ? "\"primary\":" + weekly : "\"primary\":" + primary + ",\"secondary\":" + weekly;
        string extras = plan != "Pro multi-bucket" ? "" :
            ",\"codex_special\":{\"limitName\":\"codex\",\"primary\":" + primary + ",\"secondary\":" + weekly + "}" +
            ",\"model_other\":{\"limitName\":\"Other model\",\"primary\":" + primary + ",\"secondary\":" + weekly + "}" +
            ",\"model_x\":{\"limitName\":\"模型 X\",\"primary\":" + weekly + "}";
        return QuotaParser.Parse(account, "{\"result\":{\"rateLimitsByLimitId\":{\"codex\":{" + codex + "}" + extras + "}}}", DateTimeOffset.UtcNow);
    }
    private static void Measure(Graphics graphics, Font font, string text, Size available, TextFormatFlags flags, string context)
    {
        Size needed = TextRenderer.MeasureText(graphics, text, font, new Size(Int32.MaxValue, Int32.MaxValue), flags);
        int spare = available.Width - needed.Width;
        minimumSpare = Math.Min(minimumSpare, spare);
        measurements.Add(context + "," + font.Name + "," + font.Size.ToString(CultureInfo.InvariantCulture) + "," + text + "," + needed.Width + "," + needed.Height + "," + available.Width + "," + available.Height + "," + spare);
        Check(needed.Width <= available.Width && needed.Height <= available.Height,
            context + " text [" + text + "] needs " + needed + " but has " + available);
    }
    private static object Call(string name, params object[] values)
    { return helper.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, values); }
    private static Rectangle Bounds(object row, string field)
    { return (Rectangle)row.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Run(string name, Action test)
    { try { test(); passed++; Console.WriteLine("PASS " + name); } catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); } }
    private static int Finish(string output)
    {
        measurements.Insert(0, "plan,language,mode,scale,control,font,pixel-size,text,needed-width,needed-height,available-width,available-height,spare-width");
        File.WriteAllLines(Path.Combine(output, "fonts-measurements.csv"), measurements.ToArray());
        Console.WriteLine("DetailsLayout: " + passed + " passed, " + failed + " failed; minimum horizontal spare=" + (minimumSpare == Int32.MaxValue ? "not measured" : minimumSpare + "px"));
        return failed == 0 ? 0 : 1;
    }
}
