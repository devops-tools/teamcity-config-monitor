using System;
using System.Configuration;
using System.IO;

namespace TeamCityConfigMonitor
{
    public static class Helpers
    {
        private static string _dataDir;
        public static string GetDataFolder()
        {
            if (!string.IsNullOrWhiteSpace(_dataDir))
                return _dataDir;
            _dataDir = ConfigurationManager.AppSettings.Get("TeamCityDataDir");
            if (!string.IsNullOrWhiteSpace(_dataDir) && File.Exists(Path.Combine(_dataDir, "config", "id.properties")))
            {
                Logger.Log.Write("TeamCity data directory path from configuration validated. Using: {0}", _dataDir);
                return _dataDir;
            }
            Logger.Log.Write("TeamCity data directory path from configuration failed validation. Using: {0}", string.IsNullOrWhiteSpace(_dataDir) ? "App.Config/AppSettings/TeamCityDataDir is not set." : _dataDir);
            _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JetBrains", "TeamCity");
            if (File.Exists(Path.Combine(_dataDir, "config", "id.properties")))
            {
                Logger.Log.Write("TeamCity data directory found at default install path. Using: {0}", _dataDir);
                return _dataDir;
            }
            throw new ApplicationException("Failed to determine path to TeamCity data directory.");
        }
    }
}