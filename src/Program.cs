using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("CodexUsage")]
[assembly: AssemblyDescription("Codex plan, quota and reset time widget")]
[assembly: AssemblyCompany("Amygdala42")]
[assembly: AssemblyProduct("CodexUsage")]
[assembly: AssemblyVersion("1.0.4.0")]
[assembly: AssemblyFileVersion("1.0.4.0")]

namespace CodexQuotaLite
{
    internal static class Program
    {
        [STAThread]
        public static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppPaths paths = AppPaths.Resolve(AppDomain.CurrentDomain.BaseDirectory);
            UiText.Language = new SettingsStore(paths.SettingsFile).Load().Language;
            bool ownsMutex;
            using (var singleInstance = new Mutex(true, "Local\\CodexQuotaLite.PersonalWidget.v01", out ownsMutex))
            {
                if (!ownsMutex)
                {
                    MessageBox.Show(UiText.T("CodexUsage 已在运行。可右键系统托盘中的 CodexUsage 图标，选择显示浮条。", "CodexUsage is already running. Right-click its tray icon and choose Show widget."), UiText.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }
                try
                {
                    paths.Prepare();
                    UiText.Language = new SettingsStore(paths.SettingsFile).Load().Language;
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) {
                        RecordError(paths, e.Exception);
                        MessageBox.Show(UiText.T("窗口遇到异常，请重新打开 CodexUsage。", "An unexpected error occurred. Please reopen CodexUsage."), UiText.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Application.Exit();
                    };
                    var store = new SettingsStore(paths.SettingsFile);
                    using (var source = new CodexQuotaSource(null, paths.SupportDirectory))
                    using (var context = new QuotaApplicationContext(source, store, store.Load(), delegate(Rectangle bar, Rectangle tray, Rectangle widget) { RecordPlacement(paths, bar, tray, widget); }))
                        Application.Run(context);
                    return 0;
                }
                catch (Exception exception)
                {
                    RecordError(paths, exception);
                    MessageBox.Show(UiText.T("无法启动 CodexUsage。请检查用户应用数据目录的访问权限，并确认已安装 .NET Framework。", "CodexUsage could not start. Check access to your local application data folder and the .NET Framework installation."), UiText.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 1;
                }
                finally { singleInstance.ReleaseMutex(); }
            }
        }

        private static void RecordError(AppPaths paths, Exception exception)
        {
            try
            {
                // Exception type only: never persist tokens, account JSON, service messages, or email.
                File.AppendAllText(paths.ErrorLog, DateTimeOffset.UtcNow.ToString("O") + " " + exception.GetType().FullName + Environment.NewLine);
            }
            catch { }
        }

        private static void RecordPlacement(AppPaths paths, Rectangle bar, Rectangle tray, Rectangle widget)
        {
            try
            {
                var data = new {
                    utc = DateTimeOffset.UtcNow.ToString("O"), notificationAreaDetected = !tray.IsEmpty,
                    taskbar = new { x = bar.X, y = bar.Y, width = bar.Width, height = bar.Height },
                    notification = new { x = tray.X, y = tray.Y, width = tray.Width, height = tray.Height },
                    widget = new { x = widget.X, y = widget.Y, width = widget.Width, height = widget.Height }
                };
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(paths.ErrorLog), "placement.json"), new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(data));
            }
            catch { }
        }
    }
}
