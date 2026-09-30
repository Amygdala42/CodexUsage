using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using CodexQuotaLite;

// Pure colour and bitmap tests: do not construct Forms, Controls, tray icons or windows.
internal static class ThemeTests
{
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);
    private static int passed;
    private static int failed;
    private static string output;

    public static int Main(string[] args)
    {
        output = args.Length == 0 ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme-render") : Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        Run("default theme is dark", delegate { Check(Mode() == "dark" && IsDark(), "dark is the initial mode"); });
        Run("light mode changes the actual widget and glyph", delegate {
            SetMode("dark");
            using (Bitmap dark = Render(1, Sample(), false, false, ""))
            using (Bitmap darkGlyph = WidgetRenderer.RenderGlyph(32)) {
                SetMode("light");
                using (Bitmap light = Render(1, Sample(), false, false, ""))
                using (Bitmap lightGlyph = WidgetRenderer.RenderGlyph(32)) {
                    dark.Save(Path.Combine(output, "widget-dark.png"), ImageFormat.Png); light.Save(Path.Combine(output, "widget-light.png"), ImageFormat.Png);
                    darkGlyph.Save(Path.Combine(output, "glyph-dark.png"), ImageFormat.Png); lightGlyph.Save(Path.Combine(output, "glyph-light.png"), ImageFormat.Png);
                    Check(DifferentPixels(dark, light) > dark.Width * dark.Height / 2, "light changes most widget pixels");
                    Check(DifferentPixels(darkGlyph, lightGlyph) > 300, "light changes the real glyph");
                    Check(Luminance(light.GetPixel(4, 20)) > .8 && Luminance(dark.GetPixel(4, 20)) < .1, "surface changes from dark to light");
                    Check(Mode() == "light" && !IsDark(), "reported mode follows rendered light palette");
                }
            }
        });
        foreach (float scale in new[] { 1f, 1.5f, 2f }) {
            float selectedScale = scale; Run("theme round trip preserves widget geometry and pixels at scale " + scale, delegate {
                SetMode("dark"); using (Bitmap before = Render(selectedScale, Sample(), false, false, "")) {
                    SetMode("light"); using (Bitmap light = Render(selectedScale, Sample(), false, false, "")) {
                        Check(before.Size == light.Size && SameAlpha(before, light), "same size and rounded transparency mask");
                        Check(light.GetPixel(0, 0).A == 0 && light.GetPixel(light.Width - 1, light.Height - 1).A == 0, "transparent rounded corners");
                        Check(DifferentPixels(before, light) > before.Width * before.Height / 2, "rendered palette changes at this scale");
                    }
                    SetMode("dark"); using (Bitmap after = Render(selectedScale, Sample(), false, false, "")) Check(DifferentPixels(before, after) == 0, "dark pixels restored exactly");
                }
            });
        }
        foreach (int size in new[] { 16, 32, 48 }) {
            int selectedSize = size; Run("glyph round trip preserves geometry at " + size + " pixels", delegate {
                SetMode("dark"); using (Bitmap before = WidgetRenderer.RenderGlyph(selectedSize)) {
                    SetMode("light"); using (Bitmap light = WidgetRenderer.RenderGlyph(selectedSize)) {
                        Check(before.Size == light.Size && SameAlpha(before, light), "glyph transparency mask retained");
                        Check(DifferentPixels(before, light) > selectedSize * selectedSize / 3, "glyph uses current theme");
                        Check(light.GetPixel(0, 0).A == 0, "glyph corner stays transparent");
                    }
                    SetMode("dark"); using (Bitmap after = WidgetRenderer.RenderGlyph(selectedSize)) Check(DifferentPixels(before, after) == 0, "dark glyph restored exactly");
                }
            });
        }
        foreach (string invalid in new string[] { null, "", "LIGHT", "auto", "invalid" }) {
            string value = invalid; Run("unsupported mode falls back to dark: " + (invalid ?? "null"), delegate {
                SetMode("dark"); using (Bitmap expected = Render(1, Sample(), false, false, "")) {
                    SetMode("light"); SetMode(value);
                    using (Bitmap actual = Render(1, Sample(), false, false, "")) Check(DifferentPixels(expected, actual) == 0, "invalid input restores dark rendering");
                    Check(Mode() == "dark" && IsDark(), "invalid input reports dark");
                }
            });
        }
        foreach (string selectedMode in new[] { "dark", "light" }) {
            string mode = selectedMode;
            Run(mode + " semantic text meets 4.5:1 contrast on its actual surfaces", delegate {
                SetMode(mode); Check(Mode() == mode, "requested mode is active");
                double minimum = Double.MaxValue;
                foreach (Color background in new[] { Theme.Background, Theme.Card, WidgetRenderer.Surface }) {
                    foreach (Color foreground in new[] { Theme.Text, Theme.Muted, Theme.Aqua, Theme.Blue, Theme.Warning })
                        minimum = Math.Min(minimum, RequireContrast(foreground, background, mode + " content text"));
                }
                minimum = Math.Min(minimum, RequireContrast(Theme.Text, Theme.Border, mode + " selected menu text"));
                minimum = Math.Min(minimum, RequireContrast(Theme.Aqua, Theme.Border, mode + " selection checkmark"));
                minimum = Math.Min(minimum, RequireContrast(WidgetRenderer.TimeColor, WidgetRenderer.Surface, mode + " countdown"));
                Console.WriteLine("CONTRAST " + mode + " minimum=" + minimum.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ":1");
            });
            foreach (string language in new[] { "zh", "en" }) {
                string lang = language; Run(mode + " renders all states in " + language + " without changing quota data", delegate {
                    SetMode(mode); UiText.Language = lang; QuotaWindow window = Sample();
                    string id = window.Id, label = window.Label; double? used = window.UsedPercent; int? minutes = window.WindowMinutes; DateTimeOffset? reset = window.ResetsAtUtc;
                    using (Bitmap normal = Render(1, window, false, false, ""))
                    using (Bitmap stale = Render(1, window, true, false, ""))
                    using (Bitmap error = Render(1, window, false, false, "offline"))
                    using (Bitmap unknown = Render(1, null, false, false, ""))
                    using (Bitmap busy = Render(1, null, false, true, "")) {
                        Check(DifferentPixels(normal, stale) > 20, "stale state is visible");
                        Check(DifferentPixels(normal, error) > 20, "error state is visible");
                        Check(DifferentPixels(unknown, busy) > 5, "busy state is visible");
                        Check(window.Id == id && window.Label == label && window.UsedPercent == used && window.WindowMinutes == minutes && window.ResetsAtUtc == reset && window.RemainingPercent == 63, "rendering does not mutate quota input");
                    }
                    QuotaWindow pending = Sample(); pending.ResetsAtUtc = Now;
                    using (Bitmap normal = Render(1, window, false, false, ""))
                    using (Bitmap reached = Render(1, pending, false, false, "")) Check(DifferentPixels(normal, reached) > 20, "reset pending is visible");
                });
            }
        }
        Run("both disk colours and the widget border follow the chosen theme", delegate {
            QuotaWindow full = Sample(); full.UsedPercent = 0; full.ResetsAtUtc = Now.AddDays(7);
            SetMode("dark"); using (Bitmap dark = Render(2, full, false, false, "")) {
                SetMode("light"); using (Bitmap light = Render(2, full, false, false, "")) {
                    Check(ColorDistance(dark.GetPixel(28, 20), light.GetPixel(28, 20)) > 100, "quota disk switches its actual fill colour");
                    Check(ColorDistance(dark.GetPixel(28, 60), light.GetPixel(28, 60)) > 80, "time disk switches its actual fill colour");
                    Check(ColorDistance(dark.GetPixel(1, 40), light.GetPixel(1, 40)) > 150, "widget outline switches its actual stroke colour");
                }
            }
        });
        Run("quota proportions remain visible after a theme switch", delegate {
            foreach (string mode in new[] { "dark", "light" }) {
                SetMode(mode); QuotaWindow full = Sample(), empty = Sample(); full.UsedPercent = 0; empty.UsedPercent = 100;
                using (Bitmap fullImage = Render(2, full, false, false, ""))
                using (Bitmap emptyImage = Render(2, empty, false, false, "")) {
                    Check(ColorDistance(fullImage.GetPixel(28, 20), WidgetRenderer.QuotaColor) < 5, "full disk is the active quota colour");
                    Check(ColorDistance(emptyImage.GetPixel(28, 20), Theme.Border) < 5, "empty disk remains its track colour");
                    Check(DifferentPixels(fullImage, emptyImage) > 100, "full and empty quota remain distinct");
                }
            }
        });
        SavePreview(); SetMode("dark"); UiText.Language = "zh";
        Console.WriteLine("Theme: " + passed + " passed, " + failed + " failed");
        Console.WriteLine("Pure bitmap previews: " + output);
        return failed == 0 ? 0 : 1;
    }

    // Reflection lets the first red run exercise the old renderer before the new API
    // exists. Missing switching behaves as the old dark-only implementation; the real
    // light render, mode and round-trip assertions above then fail rather than failing compilation.
    private static void SetMode(string mode) { MethodInfo method = typeof(Theme).GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic); if (method != null) method.Invoke(null, new object[] { mode }); }
    private static string Mode() { PropertyInfo property = typeof(Theme).GetProperty("Mode", BindingFlags.Static | BindingFlags.NonPublic); return property == null ? "dark" : (string)property.GetValue(null, null); }
    private static bool IsDark() { PropertyInfo property = typeof(Theme).GetProperty("IsDark", BindingFlags.Static | BindingFlags.NonPublic); return property == null || (bool)property.GetValue(null, null); }
    private static QuotaWindow Sample() { return new QuotaWindow { Id = "weekly", Label = "每周额度", UsedPercent = 37, WindowMinutes = 10080, ResetsAtUtc = Now.AddHours(120) }; }
    private static Bitmap Render(float scale, QuotaWindow window, bool stale, bool busy, string error)
    { return WidgetRenderer.Render(new Size((int)(86 * scale), (int)(40 * scale)), window, stale, busy, error, Now); }
    private static int DifferentPixels(Bitmap left, Bitmap right)
    { Check(left.Size == right.Size, "bitmap sizes match"); int count = 0; for (int y = 0; y < left.Height; y++) for (int x = 0; x < left.Width; x++) if (left.GetPixel(x, y).ToArgb() != right.GetPixel(x, y).ToArgb()) count++; return count; }
    private static bool SameAlpha(Bitmap left, Bitmap right)
    { for (int y = 0; y < left.Height; y++) for (int x = 0; x < left.Width; x++) if (left.GetPixel(x, y).A != right.GetPixel(x, y).A) return false; return true; }
    private static int ColorDistance(Color left, Color right) { return Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B); }
    private static double Luminance(Color color) { return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B); }
    private static double Linear(byte component) { double value = component / 255.0; return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4); }
    private static double RequireContrast(Color foreground, Color background, string label)
    {
        double front = Luminance(foreground), back = Luminance(background);
        double ratio = (Math.Max(front, back) + .05) / (Math.Min(front, back) + .05);
        Check(ratio >= 4.5, label + " requires 4.5:1, got " + ratio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)); return ratio;
    }
    private static void SavePreview()
    {
        using (var image = new Bitmap(1000, 660, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(image)) {
            graphics.Clear(Color.FromArgb(226, 232, 239)); graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int column = 0; column < 2; column++) {
                SetMode(column == 0 ? "dark" : "light"); UiText.Language = "en"; int left = 20 + column * 490;
                Theme.Rounded(graphics, new RectangleF(left, 20, 470, 620), 18, Theme.Background, Theme.Border);
                PreviewText(graphics, column == 0 ? "DARK" : "LIGHT", left + 24, 42, 23, Theme.Text);
                PreviewText(graphics, "Bitmap render preview - no application window", left + 24, 77, 12, Theme.Muted);
                Theme.Rounded(graphics, new RectangleF(left + 22, 110, 426, 122), 12, Theme.Card, Theme.Border);
                PreviewText(graphics, "Weekly quota", left + 38, 125, 17, Theme.Text); PreviewText(graphics, "63% remaining", left + 38, 157, 22, Theme.Aqua);
                PreviewText(graphics, "Resets in 5d 0h", left + 38, 194, 13, Theme.Blue);
                string[] labels = { "Normal", "Stale", "Error", "Sync" };
                for (int row = 0; row < 4; row++) {
                    int top = 264 + row * 83; PreviewText(graphics, labels[row], left + 24, top + 19, 15, row == 1 || row == 2 ? Theme.Warning : Theme.Muted);
                    using (Bitmap widget = Render(1.5f, row == 3 ? null : Sample(), row == 1, row == 3, row == 2 ? "offline" : "")) graphics.DrawImageUnscaled(widget, left + 165, top);
                    if (row == 0) using (Bitmap glyph = WidgetRenderer.RenderGlyph(48)) graphics.DrawImageUnscaled(glyph, left + 343, top + 7);
                }
            }
            image.Save(Path.Combine(output, "theme-comparison.png"), ImageFormat.Png);
        }
    }
    private static void PreviewText(Graphics graphics, string text, float x, float y, float size, Color color)
    { using (var font = new Font("Microsoft YaHei UI", size, FontStyle.Regular, GraphicsUnit.Pixel)) using (var brush = new SolidBrush(color)) graphics.DrawString(text, font, brush, x, y); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Run(string name, Action test)
    { try { test(); passed++; Console.WriteLine("PASS " + name); } catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); } }
}
