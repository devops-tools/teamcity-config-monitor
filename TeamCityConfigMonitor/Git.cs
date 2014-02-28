using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using LibGit2Sharp;

namespace TeamCityConfigMonitor
{
    class Git
    {
        private static readonly string GitConfigName = ConfigurationManager.AppSettings.Get("GitConfigName");
        private static readonly string GitConfigEmail = ConfigurationManager.AppSettings.Get("GitConfigEmail");
        public static readonly string Origin = ConfigurationManager.AppSettings.Get("GitRemoteRepository");
        static readonly string ConfigFolder = Path.Combine(Helpers.GetDataFolder(), "config");
        public static readonly string[] IgnoredExtensions = { ".1", ".2", ".3", ".new", ".bak", ".buildnumbers.properties" };

        private static Signature Committer
        {
            get { return new Signature(GitConfigName, GitConfigEmail, DateTimeOffset.Now); }
        }

        public string Root { get; private set; }

        static readonly object GitLock = new object();
        static Git _instance;
        public static Git Instance
        {
            get
            {
                lock (GitLock)
                    return _instance ?? (_instance = new Git());
            }
        }

        static readonly object RepoLock = new object();

        public void Init()
        {
            var gitIgnore = Path.Combine(ConfigFolder, ".gitignore");
            var ignoreFileExists = File.Exists(gitIgnore);
            File.WriteAllLines(gitIgnore, IgnoredExtensions.Select(x => string.Concat("*", x)));
            Logger.Log.Write(".gitIgnore {0} at: {1}", ignoreFileExists ? "updated" : "created", gitIgnore);

            if (!File.Exists(Path.Combine(ConfigFolder, ".git", "HEAD")))
            {
                try
                {
                    Root = Repository.Init(ConfigFolder);
                    Logger.Log.Write("Local git repository initialised at: {0}", Root);
                    using (var r = new Repository(Root))
                    {
                        r.Config.Set("user.name", GitConfigName);
                        r.Config.Set("user.email", GitConfigEmail);
                        Logger.Log.Write("Git config set. user.name: {0}, user.email: {0}", GitConfigName, GitConfigEmail);

                        r.Index.Stage(gitIgnore);
                        foreach (var ext in Watcher.IncludeFilters)
                            r.Index.Stage(Directory.GetFiles(ConfigFolder, string.Concat("*", ext),
                                SearchOption.AllDirectories));
                        Logger.Log.Write("{0} configuration entries discovered.", r.Index.Count - 1);
                        var message = string.Format("Discovery of TeamCity config. Host: {0}, Path: {1}.",
                            Environment.GetEnvironmentVariable("COMPUTERNAME"), ConfigFolder);
                        r.Commit(message, Committer);
                        Logger.Log.Write("Configuration added to local git repository with message:");
                        Logger.Log.Write("    {0}", message);
                        if (!string.IsNullOrWhiteSpace(Origin))
                        {
                            r.SyncRemoteBranch();
                            r.Network.Push(r.Head);
                            Logger.Log.Write("Configuration pushed to remote git repository.");
                        }
                    }
                }
                catch (NonFastForwardException e)
                {
                    Logger.Log.Write("The remote repository is out of sync with the local repository. Changes have not been synced to remote.", EventLogEntryType.Warning);
                    Logger.Log.Write(e);
                }
                catch (Exception e)
                {
                    Logger.Log.Write(e);
                    throw;
                }
            }
            else
            {
                Logger.Log.Write("Local git repository found at: {0}", ConfigFolder);
                AddChanges();
            }
        }

        public bool IsChangeOfInterest(string path)
        {
            lock (RepoLock)
            {
                if (Root == null)
                    Root = Repository.Init(ConfigFolder);
                return new Repository(Root).HasUnstagedChanges();
            }
        }

        public void AddChanges()
        {
            try
            {
                lock (RepoLock)
                {
                    if (Root == null)
                        Root = Repository.Init(ConfigFolder);
                    using (var r = new Repository(Root))
                    {
                        if (r.HasUnstagedChanges())
                        {
                            r.CommitUnstagedChanges(Committer);
                            if (!string.IsNullOrWhiteSpace(Origin) && r.Network.Remotes.Any())
                            {
                                r.SyncRemoteBranch();
                                r.Network.Push(r.Head);
                                Logger.Log.Write("Configuration pushed to remote git repository.");
                            }
                        }
                    }
                }
            }
            catch (NonFastForwardException e)
            {
                Logger.Log.Write("The remote repository is out of sync with the local repository. Changes have not been synced to remote.", EventLogEntryType.Warning);
                Logger.Log.Write(e);
            }
            catch (Exception e)
            {
                Logger.Log.Write(e);
                throw;
            }
        }
    }

    public static class GitExtensions
    {
        public static void SyncRemoteBranch(this Repository repository)
        {
            var remote = repository.Network.Remotes.Any(x => x.Name == "origin")
                ? repository.Network.Remotes["origin"]
                : repository.Network.Remotes.Add("origin", Git.Origin);
            var canonicalName = repository.Head.CanonicalName;
            repository.Branches.Update(repository.Head,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = canonicalName);
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
                Logger.Log.Write("{0} configuration changes discovered.", paths.Count());
                var cd = GetCommitDetails(paths, key);
                if (cd.Author != null)
                    repository.Commit(cd.Message, cd.Author, committer);
                else
                    repository.Commit(cd.Message, committer);
                Logger.Log.Write("Configuration changes committed to local git repository with author: '{0}', and message:\n{1}", (cd.Author != null) ? cd.Author.Name : "null", cd.Message);
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
                Logger.Log.Write("Configuration changes committed to local git repository with message:\n{0}", message);
            }
        }

        private static CommitDetails GetCommitDetails(IEnumerable<string> paths, string pool)
        {
            var messages = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "Untracked",
                    new Dictionary<string, string>
                    {
                        { "Project", "{0}, added by: {1}." },
                        { "Default", "{0} configuration file addition{1} detected." }
                    }
                },
                {
                    "Modified",
                    new Dictionary<string, string>
                    {
                        { "Project", "{0}, modified by: {1}." },
                        { "Default", "{0} configuration file modification{1} detected." }
                    }
                },
                {
                    "Missing",
                    new Dictionary<string, string>
                    {
                        { "Project", "{0}, deleted by: {1}." },
                        { "Default", "{0} configuration file deletion{1} detected." }
                    }
                }
            };
            var enumerable = paths as string[] ?? paths.ToArray();
            if (enumerable.Any(x => x.StartsWith("projects") && Path.GetFileName(Path.GetDirectoryName(x)) == "buildTypes"))
            {
                var configId = Path.GetFileNameWithoutExtension(enumerable.First(x => x.EndsWith(".xml")));
                return new CommitDetails(messages[pool]["Project"], configId);
            }
            return new CommitDetails(string.Format(messages[pool]["Default"], enumerable.Count(), enumerable.Count() == 1 ? string.Empty : "s"));
        }

        class CommitDetails
        {
            public CommitDetails(string message, string configId = null)
            {
                Message = string.Format(message, configId, "Unknown user");
                if (!string.IsNullOrWhiteSpace(configId))
                {
                    try
                    {
                        var auditEntry = Helpers.ReadEndTokens(ConfigurationManager.AppSettings.Get("TeamCityAuditLog"), 5, Encoding.UTF8, Environment.NewLine).Last(x => x.Contains(string.Format("id={0},", configId)));
                        var userId = auditEntry.Split(new[] { "id=" }, StringSplitOptions.RemoveEmptyEntries).Last().TrimEnd('"', '}', ' ');
                        Author = GetAuthor(userId);
                        if (Author != null)
                            Message = string.Format(message, configId, Author.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Write(ex);
                        Author = null;
                    }
                }
            }

            public string Message { get; private set; }
            public Signature Author { get; private set; }

            private Signature GetAuthor(string userId)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var up = string.Format("{0}:{1}",
                            ConfigurationManager.AppSettings.Get("TeamCityUsername"),
                            ConfigurationManager.AppSettings.Get("TeamCityPassword"));
                        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(up), Base64FormattingOptions.None);
                        client.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                        var user = XDocument.Load(client.OpenRead(string.Concat(ConfigurationManager.AppSettings.Get("TeamCityUrl").TrimEnd('/'), "/httpAuth/app/rest/users/id:", userId))).Root;
                        return user != null
                            ? new Signature(user.Attribute("name").Value, user.Attribute("email").Value, DateTimeOffset.Now)
                            : null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Write(ex);
                    return null;
                }
            }
        }
    }
}
