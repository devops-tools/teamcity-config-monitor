using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;

namespace TeamCityConfigMonitor
{
    public class Watcher
    {
        public static readonly string ConfigFolder = Path.Combine(Helpers.GetDataFolder(), "config");
        public static readonly string[] IncludeFilters = { ".xml", ".xsd", ".properties", ".dist", ".dtd", ".ftl" };

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Watch()
        {
            var watchers = new List<FileSystemWatcher>();
            foreach (var w in IncludeFilters.Select(filter => new FileSystemWatcher
            {
                Path = ConfigFolder,
                NotifyFilter = NotifyFilters.LastWrite
                             | NotifyFilters.LastAccess
                             | NotifyFilters.FileName
                             | NotifyFilters.DirectoryName,
                Filter = string.Concat("*", filter)
            }))
            {
                w.Changed += OnChanged;
                w.Created += OnChanged;
                w.Deleted += OnChanged;
                w.EnableRaisingEvents = true;
                w.IncludeSubdirectories = true;
                watchers.Add(w);
            }
        }
        
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath.ToLower());
            if (IncludeFilters.Contains(ext) && Git.Instance.IsChangeOfInterest(e.FullPath))
                Git.Instance.AddChanges();
        }
    }
}

