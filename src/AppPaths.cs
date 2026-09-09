using System;
using System.IO;

namespace CodexQuotaLite
{
    internal sealed class AppPaths
    {
        public string SupportDirectory { get; private set; }
        public string SettingsFile { get; private set; }
        public string ErrorLog { get; private set; }

        public static AppPaths Resolve(string executableDirectory)
        {
            string folder = Path.GetFullPath(executableDirectory);
            // Every copy is portable; no parent workspace or machine-specific path is used.
            string support = Path.Combine(folder, "env");
            return new AppPaths {
                SupportDirectory = support,
                SettingsFile = Path.Combine(support, "config", "CodexQuotaLite", "settings.json"),
                ErrorLog = Path.Combine(support, "logs", "CodexQuotaLite", "application.log")
            };
        }

        public void Prepare()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
            Directory.CreateDirectory(Path.GetDirectoryName(ErrorLog));
            Directory.CreateDirectory(Path.Combine(SupportDirectory, "tmp", "CodexQuotaLite"));
        }
    }
}
