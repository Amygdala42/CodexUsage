using System;
using System.IO;

namespace CodexQuotaLite
{
    internal sealed class AppPaths
    {
        public string SupportDirectory { get; private set; }
        public string SettingsFile { get; private set; }
        public string ErrorLog { get; private set; }
        private string legacySettingsFile;

        public static AppPaths Resolve(string executableDirectory)
        {
            return Resolve(executableDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        internal static AppPaths Resolve(string executableDirectory, string localApplicationData)
        {
            string folder = Path.GetFullPath(executableDirectory);
            string support = Path.Combine(Path.GetFullPath(localApplicationData), "CodexUsage");
            return new AppPaths {
                SupportDirectory = support,
                SettingsFile = Path.Combine(support, "settings.json"),
                ErrorLog = Path.Combine(support, "logs", "application.log"),
                legacySettingsFile = Path.Combine(folder, "env", "config", "CodexQuotaLite", "settings.json")
            };
        }

        public void Prepare()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
            Directory.CreateDirectory(Path.GetDirectoryName(ErrorLog));
            Directory.CreateDirectory(Path.Combine(SupportDirectory, "tmp", "CodexQuotaLite"));
            // Import only known settings, never logs, credentials or arbitrary files.
            // Keep the old directory intact; it may contain unrelated user files.
            if (!File.Exists(SettingsFile) && File.Exists(legacySettingsFile))
                new SettingsStore(SettingsFile).Save(new SettingsStore(legacySettingsFile).Load());
        }
    }
}
