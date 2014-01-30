using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp;

namespace TeamCityConfigMonitor
{
    public static class Extensions
    {
        #region Logger

        public static string ToMessage(this Exception exception, bool isInnerException = false)
        {
            var message = new StringBuilder();
            if (!isInnerException)
                message.AppendFormat("Faulting application name: {0}, version: {1}, time stamp: {2}, path: {3}\n",
                    Assembly.GetEntryAssembly().FullName,
                    Assembly.GetEntryAssembly().GetName().Version,
                    DateTimeOffset.Now,
                    Process.GetCurrentProcess().MainModule.FileName);
            message.AppendFormat("Source: {0}, Exception: {1}, Message: {2}\n",
                exception.Source,
                exception.GetType(),
                exception.Message);
            message.AppendLine(exception.StackTrace);
            if (exception.InnerException != null)
                message.AppendLine(exception.InnerException.ToMessage());
            return message.ToString();
        }

        #endregion

        #region enums

        public static string Get(this MonitorServiceHost.Service service, string property)
        {
            var hostAttributes = (ServiceAttribute[])typeof(Enum).GetCustomAttributes(typeof(ServiceAttribute), true);
            switch (property)
            {
                case "Description":
                    return hostAttributes.Any() ? service.ToString() : hostAttributes.First().Description;
                case "Name":
                    return hostAttributes.Any() ? service.ToString() : hostAttributes.First().Name;
            }
            return null;
        }

        #endregion

        #region Git

        public static void TrackRemoteBranch(this Repository repository, string localBranchName, string remoteBranchName, string remoteName)
        {
            var remote = repository.Network.Remotes.Any(x => x.Name == remoteName)
                ? repository.Network.Remotes[remoteName]
                : repository.Network.Remotes.Add(remoteName, Git.Origin);
            //var canonicalName = repository.Branches[localBranchName].CanonicalName;
            repository.Branches.Update(repository.Branches[localBranchName],
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = string.Concat("refs/heads/", remoteBranchName));
        }

        public static bool HasUnstagedChanges(this Repository repository)
        {
            var status = repository.Index.RetrieveStatus();
            return status.Modified.Union(status.Untracked).Union(status.Missing).Any();
        }

        public static void CommitUnstagedChanges(this Repository repository, Signature committer)
        {
            var status = repository.Index.RetrieveStatus();
            var changes = new Dictionary<string, IEnumerable<StatusEntry>>
            {
                { "Untracked", status.Untracked },
                { "Modified", status.Modified },
                { "Missing", status.Missing }
            };
            foreach (var key in changes.Keys.Where(x => changes[x].Any()))
            {
                var paths = changes[key]
                    .Select(x => x.FilePath)
                    .ToArray();
                repository.Index.Stage(paths);
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "{0} configuration changes discovered.", paths.Count());
                var message = GetMessage(paths, key);
                repository.Commit(message, committer);
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Configuration changes committed to local git repository with message:\n{0}", message);
            }
        }

        public static void RemoveIgnoredPaths(this Repository repository, Signature committer)
        {
            var paths = repository.Index.Select(x => x.Path).Where(x => Git.IgnoredExtensions.Any(x.EndsWith)).ToArray();
            if (paths.Any())
            {
                repository.Index.Remove(paths, false);
                var message = string.Format("Removed {0} previously indexed, but now ignored, paths from source control.", paths.Count());
                repository.Commit(message, committer);
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Configuration changes committed to local git repository with message:\n{0}", message);
            }
        }

        private static string GetMessage(IEnumerable<string> paths, string pool)
        {
            var messages = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "Untracked",
                    new Dictionary<string, string>
                    {
                        { "Project", "New build configuration detected: {0}." },
                        { "Default", "{0} configuration file addition{1} detected." }
                    }
                },
                {
                    "Modified",
                    new Dictionary<string, string>
                    {
                        { "Project", "Modified build configuration detected: {0}." },
                        { "Default", "{0} configuration file modification{1} detected." }
                    }
                },
                {
                    "Missing",
                    new Dictionary<string, string>
                    {
                        { "Project", "Deleted build configuration detected: {0}." },
                        { "Default", "{0} configuration file deletion{1} detected." }
                    }
                }
            };
            var enumerable = paths as string[] ?? paths.ToArray();
            if (enumerable.Any(x => x.StartsWith("projects") && Path.GetFileName(Path.GetDirectoryName(x)) == "buildTypes"))
            {
                return string.Format(messages[pool]["Project"], Path.GetFileNameWithoutExtension(enumerable.First(x => x.EndsWith(".xml"))));
            }
            return string.Format(messages[pool]["Default"], enumerable.Count(), enumerable.Count() == 1 ? string.Empty : "s");
        }

        #endregion
    }
}