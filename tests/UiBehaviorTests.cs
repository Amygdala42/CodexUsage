using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CodexQuotaLite;

internal static class UiBehaviorTests
{
    private static int run, failed;
    private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
    [STAThread]
    public static int Main()
    {
        Check("Mouse departure still dismisses at 500ms despite activation", MouseDeparture);
        Check("Keyboard activity keeps focused details open outside pointer", KeyboardKeepsOpen);
        Check("Keyboard focus loss restarts mouse dismissal delay", KeyboardFocusLoss);
        Check("Resumed pointer activity releases keyboard hold", PointerResumes);
        Check("Continuous mouse motion outside does not extend 500ms grace", PointerDoesNotExtendGrace);
        Check("Menus suspend dismissal and restart departure delay", MenuDelay);
        Check("Hidden details clear keyboard modality", HiddenResets);
        Check("Fullscreen on another monitor leaves widget visible", OtherScreen);
        Check("Same-screen and spanning fullscreen hide widget", SameScreen);
        Check("Maximized windows and shell never hide widget", MaximizedAndShell);
        Check("Details exposes sign-in and refresh failures in readable tooltip", ErrorTooltip);
        Check("Settings failure stays visible alongside refresh reason", SettingsTooltip);
        Check("Successful refresh clears obsolete detailed error", TooltipClears);
        Check("Details receives Tab and selector keyboard input", KeyboardEvents);
        Check("Long error tooltip wraps and draws with the active theme", TooltipWrapAndTheme);
        Check("Menu Enter preprocessing opens details in keyboard mode", delegate { MenuKeyboardOpens(Keys.Enter); });
        Check("Menu Space preprocessing opens details in keyboard mode", delegate { MenuKeyboardOpens(Keys.Space); });
        Check("Menu mouse click preserves ordinary hover dismissal", MenuMouseOpens);
        Console.WriteLine("RESULT: {0} passed, {1} failed", run - failed, failed);
        return failed == 0 ? 0 : 1;
    }
    private static void Check(string name, Action action)
    { run++; try { action(); Console.WriteLine("PASS: " + name); } catch (Exception e) { failed++; Console.WriteLine("FAIL: " + name + " - " + e.GetBaseException().Message); } }
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static object Call(object instance, string method, params object[] values)
    {
        MethodInfo found = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(found != null, "Missing production behavior: " + method);
        return found.Invoke(instance, values);
    }
    private static bool Dismiss(HoverDismissState state, int milliseconds, bool visible, bool inside, bool menu, bool focused)
    {
        MethodInfo found = typeof(HoverDismissState).GetMethod("ShouldDismiss", BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(DateTimeOffset), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }, null);
        Assert(found != null, "Missing keyboard-aware dismissal behavior");
        return (bool)found.Invoke(state, new object[] { Start.AddMilliseconds(milliseconds), visible, inside, menu, focused });
    }
    private static void MouseDeparture()
    {
        HoverDismissState state = new HoverDismissState();
        Assert(!Dismiss(state, 0, true, false, false, true), "Starts grace period");
        Assert(!Dismiss(state, 499, true, false, false, true), "Retains 499ms grace");
        Assert(Dismiss(state, 500, true, false, false, true), "Activation alone must not hold open");
    }
    private static void KeyboardKeepsOpen()
    {
        HoverDismissState state = new HoverDismissState();
        state.ShouldDismiss(Start, true, false, false);
        Call(state, "KeyboardUsed");
        Assert(!Dismiss(state, 700, true, false, false, true), "Keyboard input cancels pending departure");
        Assert(!Dismiss(state, 10000, true, false, false, true), "Focused keyboard interaction remains open");
    }
    private static void KeyboardFocusLoss()
    {
        HoverDismissState state = new HoverDismissState(); Call(state, "KeyboardUsed");
        Assert(!Dismiss(state, 0, true, false, false, true), "Keyboard held");
        Assert(!Dismiss(state, 100, true, false, false, false), "Focus loss starts grace");
        Assert(Dismiss(state, 600, true, false, false, false), "Focus loss releases hold");
    }
    private static void PointerResumes()
    {
        HoverDismissState state = new HoverDismissState(); Call(state, "KeyboardUsed");
        Assert(!Dismiss(state, 0, true, false, false, true), "Keyboard held");
        Call(state, "PointerUsed");
        Assert(!Dismiss(state, 100, true, false, false, true), "Mouse restores grace");
        Assert(Dismiss(state, 600, true, false, false, true), "Mouse mode dismisses even with focus");
    }
    private static void PointerDoesNotExtendGrace()
    {
        HoverDismissState state = new HoverDismissState();
        Assert(!Dismiss(state, 0, true, false, false, true), "Mouse departure starts grace");
        Call(state, "PointerUsed");
        Assert(!Dismiss(state, 100, true, false, false, true), "Mouse movement before grace");
        Call(state, "PointerUsed");
        Assert(Dismiss(state, 500, true, false, false, true), "Movement outside must retain original departure time");
    }
    private static void MenuDelay()
    {
        HoverDismissState state = new HoverDismissState();
        Assert(!Dismiss(state, 0, true, false, false, false), "Starts grace");
        Assert(!Dismiss(state, 900, true, false, true, false), "Menu holds details");
        Assert(!Dismiss(state, 1000, true, false, false, false), "Menu close restarts grace");
        Assert(Dismiss(state, 1500, true, false, false, false), "Menu behavior preserved");
    }
    private static void HiddenResets()
    {
        HoverDismissState state = new HoverDismissState(); Call(state, "KeyboardUsed");
        Assert(!Dismiss(state, 0, false, false, false, true), "Hidden resets");
        Assert(!Dismiss(state, 100, true, false, false, true), "Reopen grace");
        Assert(Dismiss(state, 600, true, false, false, true), "Keyboard hold does not leak into next opening");
    }
    private static bool Fullscreen(Rectangle window, Rectangle screen, bool maximized, string kind)
    {
        MethodInfo found = typeof(TaskbarPlacement).GetMethod("ForegroundCoversWidgetScreen", BindingFlags.Static | BindingFlags.NonPublic);
        Assert(found != null, "Missing widget-screen fullscreen classification");
        return (bool)found.Invoke(null, new object[] { window, screen, maximized, kind });
    }
    private static void OtherScreen()
    {
        Assert(!Fullscreen(new Rectangle(1920, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1080), false, "Video"), "Other screen ignored");
        Assert(!Fullscreen(new Rectangle(-1920, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1080), false, "Video"), "Negative-coordinate screen ignored");
    }
    private static void SameScreen()
    {
        Assert(Fullscreen(new Rectangle(-1920, 0, 1920, 1080), new Rectangle(-1920, 0, 1920, 1080), false, "Video"), "Same negative screen fullscreen");
        Assert(Fullscreen(new Rectangle(-1920, 0, 3840, 1080), new Rectangle(0, 0, 1920, 1080), false, "Video"), "Spanning fullscreen covers widget screen");
        Assert(!Fullscreen(new Rectangle(0, 0, 1920, 1040), new Rectangle(0, 0, 1920, 1080), false, "Video"), "Partial display is not fullscreen");
    }
    private static void MaximizedAndShell()
    {
        Rectangle screen = new Rectangle(0, 0, 1920, 1080);
        Assert(!Fullscreen(screen, screen, true, "Video"), "Auto-hide maximized window allowed");
        foreach (string kind in new[] { "Shell_TrayWnd", "Progman", "WorkerW" })
            Assert(!Fullscreen(screen, screen, false, kind), "Shell excluded: " + kind);
    }
    private static T Field<T>(object instance, string name)
    { FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); Assert(field != null, "Missing configured control: " + name); return (T)field.GetValue(instance); }
    private static void VerifyTip(DetailsForm form, string fragment)
    {
        Label status = Field<Label>(form, "status");
        ToolTip tip = Field<ToolTip>(form, "statusTip");
        Assert(status.Enabled && tip.Active && tip.ShowAlways, "Status tooltip must be available");
        Assert(tip.GetToolTip(status).Contains(fragment), "Visible tip must contain actionable detail");
        Assert(status.AccessibleDescription.Contains(fragment), "Accessibility keeps same detail");
        Assert(tip.OwnerDraw, "Long details use measured wrapping");
    }
    private static void ErrorTooltip()
    {
        foreach (string language in new[] { "zh", "en" })
        using (DetailsForm form = new DetailsForm(new AppSettings { Language = language }))
        {
            VerifyTip(form, language == "zh" ? "登录" : "Sign in");
            form.SetState(null, null, false, false, "diagnostic " + new string('x', 500), null);
            VerifyTip(form, "diagnostic " + new string('x', 500));
        }
    }
    private static void SettingsTooltip()
    {
        using (DetailsForm form = new DetailsForm(new AppSettings { Language = "en" }))
        { form.SetState(null, null, false, false, "refresh cause", "settings cause"); VerifyTip(form, "settings cause"); VerifyTip(form, "refresh cause"); }
    }
    private static void TooltipClears()
    {
        using (DetailsForm form = new DetailsForm(new AppSettings()))
        {
            form.SetState(null, null, false, false, "obsolete error", null);
            form.SetState(new QuotaSnapshot(), null, false, false, null, null);
            Assert(!Field<ToolTip>(form, "statusTip").GetToolTip(Field<Label>(form, "status")).Contains("obsolete error"), "Successful refresh removes old error");
        }
    }
    private static void KeyboardEvents()
    {
        using (DetailsForm form = new DetailsForm(new AppSettings()))
        {
            EventInfo input = typeof(DetailsForm).GetEvent("KeyboardInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(input != null, "Missing details keyboard modality event");
            int seen = 0; EventHandler handler = delegate { seen++; };
            input.GetAddMethod(true).Invoke(form, new object[] { handler });
            Message message = new Message();
            Call(form, "ProcessCmdKey", message, Keys.Tab);
            Assert(seen == 1, "Tab enters keyboard mode before focus navigation");
            UiDarkChoice choice = Field<UiDarkChoice>(form, "windowChoice");
            choice.Items.Add("one"); choice.Items.Add("two"); choice.Enabled = true;
            Call(choice, "OnKeyDown", new KeyEventArgs(Keys.Down));
            Assert(choice.SelectedIndex == 0 && seen == 2, "Selector keyboard input reaches details");
        }
    }
    private static void TooltipWrapAndTheme()
    {
        foreach (string mode in new[] { "dark", "light" })
        using (DetailsForm form = new DetailsForm(new AppSettings { Language = "en", ThemeMode = mode }))
        {
            Label status = Field<Label>(form, "status");
            ToolTip tip = Field<ToolTip>(form, "statusTip");
            form.SetState(null, null, false, false, "Refresh failed. Sign in to Codex and retry. " + new string('x', 500), null);
            PopupEventArgs popup = new PopupEventArgs(form, status, false, Size.Empty);
            Call(form, "StatusTipPopup", tip, popup);
            Assert(popup.ToolTipSize.Width <= 740 && popup.ToolTipSize.Height > status.Font.Height * 2, "Long detail wraps within a bounded readable width");
            using (Bitmap bitmap = new Bitmap(popup.ToolTipSize.Width, popup.ToolTipSize.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                DrawToolTipEventArgs draw = new DrawToolTipEventArgs(graphics, form, status,
                    new Rectangle(Point.Empty, popup.ToolTipSize), tip.GetToolTip(status), Color.White, Color.Black, status.Font);
                Call(form, "StatusTipDraw", tip, draw);
                Assert(bitmap.GetPixel(bitmap.Width - 3, bitmap.Height - 3).ToArgb() == Theme.Card.ToArgb(), "Tooltip background follows active theme");
                Assert(bitmap.GetPixel(0, 0).ToArgb() == Theme.Border.ToArgb(), "Tooltip border follows active theme");
            }
        }
    }
    private static void MenuKeyboardOpens(Keys key)
    {
        HoverDismissState state = new HoverDismissState();
        using (ContextMenuStrip menu = new ContextMenuStrip())
        using (UiKeyboardMenuItem item = new UiKeyboardMenuItem("Details", null))
        {
            int clicked = 0, keyDown = 0, keyboard = 0;
            menu.KeyDown += delegate { keyDown++; };
            item.KeyboardInvoked += delegate { keyboard++; state.KeyboardUsed(); };
            item.Click += delegate { Assert(keyboard == 1, "Keyboard source must be available before opening callback"); clicked++; };
            menu.Items.Add(item);
            bool handled = (bool)Call(item, "ProcessDialogKey", key);
            Assert(handled && clicked == 1, "Actual menu preprocessing must activate item");
            Assert(keyDown == 0, "Preprocessed activation does not raise menu KeyDown");
            Assert(!Dismiss(state, 0, true, false, false, true), "Opening starts hover observation");
            Assert(!Dismiss(state, 500, true, false, false, true), "Keyboard activation must keep focused details open");
        }
    }
    private static void MenuMouseOpens()
    {
        HoverDismissState state = new HoverDismissState();
        using (ContextMenuStrip menu = new ContextMenuStrip())
        using (UiKeyboardMenuItem item = new UiKeyboardMenuItem("Details", null))
        {
            int clicked = 0, keyboard = 0;
            item.KeyboardInvoked += delegate { keyboard++; state.KeyboardUsed(); };
            item.Click += delegate { clicked++; };
            menu.Items.Add(item);
            Assert((bool)Call(item, "ProcessDialogKey", Keys.Enter) && keyboard == 1, "First keyboard activation");
            state.Reset();item.PerformClick();
            Assert(clicked == 2 && keyboard == 1, "Subsequent mouse command activates without inherited keyboard source");
            Assert(!Dismiss(state, 0, true, false, false, true), "Mouse opening starts grace");
            Assert(Dismiss(state, 500, true, false, false, true), "Mouse command retains departure dismissal");
        }
    }
}
