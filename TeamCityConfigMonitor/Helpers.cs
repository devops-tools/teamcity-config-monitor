using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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

        /// <summary>
        /// Reads the end tokens.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="numberOfTokens">The number of tokens.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="tokenSeparator">The token separator.</param>
        /// <returns></returns>
        /// <remarks>http://stackoverflow.com/a/398512/68115</remarks>
        public static IEnumerable<string> ReadEndTokens(string path, Int64 numberOfTokens, Encoding encoding, string tokenSeparator)
        {
            var sizeOfChar = encoding.GetByteCount("\n");
            var buffer = encoding.GetBytes(tokenSeparator);
            while (true)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Int64 tokenCount = 0;
                        var endPosition = fs.Length / sizeOfChar;

                        for (Int64 position = sizeOfChar; position < endPosition; position += sizeOfChar)
                        {
                            fs.Seek(-position, SeekOrigin.End);
                            fs.Read(buffer, 0, buffer.Length);

                            if (encoding.GetString(buffer) == tokenSeparator)
                            {
                                tokenCount++;
                                if (tokenCount == numberOfTokens)
                                {
                                    var returnBuffer = new byte[fs.Length - fs.Position];
                                    fs.Read(returnBuffer, 0, returnBuffer.Length);
                                    return encoding.GetString(returnBuffer).Split(new[] { tokenSeparator }, StringSplitOptions.None);
                                }
                            }
                        }
                        fs.Seek(0, SeekOrigin.Begin);
                        buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        return encoding.GetString(buffer).Split(new[] { tokenSeparator }, StringSplitOptions.None);
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}