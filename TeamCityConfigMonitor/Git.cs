using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace TeamCityConfigMonitor
{
    class Git
    {
        public const string RemoteDefault = "origin";
        public const string BranchTeamCityDefault = "master";
        public const string BranchApprovedDefault = "approved";
        public static readonly string RemoteTeamCity = ConfigurationManager.AppSettings.Get("GitRemoteTeamCity") ?? RemoteDefault;
        public static readonly string RemoteApproved = ConfigurationManager.AppSettings.Get("GitRemoteApproved") ?? RemoteDefault;
        public static readonly string BranchLocalTeamCity = ConfigurationManager.AppSettings.Get("GitBranchLocalTeamCity") ?? BranchTeamCityDefault;
        public static readonly string BranchLocalApproved = ConfigurationManager.AppSettings.Get("GitBranchLocalApproved") ?? BranchApprovedDefault;
        public static readonly string BranchRemoteTeamCity = ConfigurationManager.AppSettings.Get("GitBranchRemoteTeamCity") ?? BranchTeamCityDefault;
        public static readonly string BranchRemoteApproved = ConfigurationManager.AppSettings.Get("GitBranchRemoteApproved") ?? BranchApprovedDefault;

        #region WatchService

        private static readonly string GitConfigName = ConfigurationManager.AppSettings.Get("GitConfigName");
        private static readonly string GitConfigEmail = ConfigurationManager.AppSettings.Get("GitConfigEmail");
        public static readonly string Origin = ConfigurationManager.AppSettings.Get("GitRemoteRepository");
        static readonly string TeamCityConfigFolder = Path.Combine(Helpers.GetDataFolder(), "config");
        public static readonly string[] IgnoredExtensions = { ".1", ".2", ".3", ".new", ".bak", ".buildnumbers.properties" };

        private static Signature Committer
        {
            get { return new Signature(GitConfigName, GitConfigEmail, DateTimeOffset.Now); }
        }

        public string TeamCityRoot { get; private set; }

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

        static readonly object TeamCityRepoLock = new object();
        static readonly object MergeTestRepoLock = new object();

        public void InitWatcher()
        {
            var gitIgnore = Path.Combine(TeamCityConfigFolder, ".gitignore");
            var ignoreFileExists = File.Exists(gitIgnore);
            File.WriteAllLines(gitIgnore, IgnoredExtensions.Select(x => string.Concat("*", x)));
            Logger.Log.Write(MonitorServiceHost.Service.WatchService, ".gitIgnore {0} at: {1}", ignoreFileExists ? "updated" : "created", gitIgnore);

            if (!File.Exists(Path.Combine(TeamCityConfigFolder, ".git", "HEAD")))
            {
                try
                {
                    TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                    Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Local git repository initialised at: {0}", TeamCityRoot);
                    using (var r = new Repository(TeamCityRoot))
                    {
                        r.Config.Set("user.name", GitConfigName);
                        r.Config.Set("user.email", GitConfigEmail);
                        Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Git config set. user.name: {0}, user.email: {0}", GitConfigName, GitConfigEmail);

                        r.Index.Stage(gitIgnore);
                        foreach (var ext in Watcher.IncludeFilters)
                            r.Index.Stage(Directory.GetFiles(TeamCityConfigFolder, string.Concat("*", ext),
                                SearchOption.AllDirectories));
                        Logger.Log.Write(MonitorServiceHost.Service.WatchService, "{0} configuration entries discovered.", r.Index.Count - 1);
                        var message = string.Format("Discovery of TeamCity config. Host: {0}, Path: {1}.",
                            Environment.GetEnvironmentVariable("COMPUTERNAME"), TeamCityConfigFolder);
                        r.Commit(message, Committer);
                        Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Configuration added to local git repository with message:");
                        Logger.Log.Write(MonitorServiceHost.Service.WatchService, "    {0}", message);
                        if (!string.IsNullOrWhiteSpace(Origin))
                        {
                            r.TrackRemoteBranch(BranchLocalTeamCity, BranchRemoteTeamCity, RemoteTeamCity);
                            r.Network.Push(r.Branches[BranchLocalTeamCity]);
                            Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Configuration pushed to remote git repository.");
                        }
                    }
                }
                catch (NonFastForwardException e)
                {
                    Logger.Log.Write(MonitorServiceHost.Service.WatchService, "The remote repository is out of sync with the local repository. Changes have not been synced to remote.", EventLogEntryType.Warning);
                    Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
                }
                catch (Exception e)
                {
                    Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
                    throw;
                }
            }
            else
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Local git repository found at: {0}", TeamCityConfigFolder);
                RemoveNewlyIgnored();
                AddChanges();
            }
        }

        public bool IsChangeOfInterest(string path)
        {
            lock (TeamCityRepoLock)
            {
                if (TeamCityRoot == null)
                    TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                return new Repository(TeamCityRoot).HasUnstagedChanges();
            }
        }

        public void AddChanges()
        {
            try
            {
                lock (TeamCityRepoLock)
                {
                    if (TeamCityRoot == null)
                        TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                    using (var r = new Repository(TeamCityRoot))
                    {
                        if (r.HasUnstagedChanges())
                        {
                            r.CommitUnstagedChanges(Committer);
                            if (!string.IsNullOrWhiteSpace(Origin) && r.Network.Remotes.Any())
                            {
                                r.TrackRemoteBranch(BranchLocalTeamCity, BranchRemoteTeamCity, RemoteTeamCity);
                                r.Network.Push(r.Branches[BranchLocalTeamCity]);
                                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "Configuration pushed to remote git repository.");
                            }
                        }
                    }
                }
            }
            catch (NonFastForwardException e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "The remote repository is out of sync with the local repository. Changes have not been synced to remote.", EventLogEntryType.Warning);
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
            }
            catch (Exception e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
                throw;
            }
        }

        private void RemoveNewlyIgnored()
        {
            try
            {
                lock (TeamCityRepoLock)
                {
                    if (TeamCityRoot == null)
                        TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                    using (var r = new Repository(TeamCityRoot))
                        r.RemoveIgnoredPaths(Committer);
                }
            }
            catch (Exception e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
                throw;
            }
        }

        #endregion


        #region PollService

        public void CreateApproved()
        {
            try
            {
                lock (TeamCityRepoLock)
                {
                    if (TeamCityRoot == null)
                        TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                    using (var r = new Repository(TeamCityRoot))
                    {
                        //we should only hit this once in the life of the repo
                        if (r.Branches.All(x => x.Name != BranchLocalApproved))
                        {
                            //create the approved branch, push it upstream.
                            r.CreateBranch(BranchLocalApproved);
                            Logger.Log.Write(MonitorServiceHost.Service.PollService, "Created local 'approved' branch as: '{0}'.", BranchLocalApproved);
                            r.TrackRemoteBranch(BranchLocalApproved, BranchRemoteApproved, RemoteApproved);
                            r.TrackRemoteBranch(BranchLocalTeamCity, BranchRemoteTeamCity, RemoteTeamCity);
                            r.Network.Push(r.Branches.Where(x => x.CanonicalName.StartsWith("refs/heads")));
                            Logger.Log.Write(MonitorServiceHost.Service.PollService, "Pushed local 'approved' branch ({0}) to remote: '{1}' as branch '{2}' with tracking enabled.", BranchLocalApproved, RemoteApproved, BranchRemoteApproved);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.PollService, e);
                throw;
            }
        }

        public void PullApproved()
        {
            try
            {
                var mergeWorks = false;
                foreach (var dir in Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "teamcity-config-monitor")))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception exception)
                    {
                        Logger.Log.Write(MonitorServiceHost.Service.PollService, string.Format("Failed to delete {0}. Delete it manually!", dir), EventLogEntryType.Warning);
                    }
                }
                var mergeTestPath = Path.Combine(Path.GetTempPath(), "teamcity-config-monitor", Guid.NewGuid().ToString());
                lock (MergeTestRepoLock)
                {
                    if (!string.IsNullOrWhiteSpace(Origin))
                    {
                        var mergeTestRoot = Repository.Clone(Origin, mergeTestPath);
                        Logger.Log.Write(MonitorServiceHost.Service.PollService, "Cloned {0} to temp merge test: {1}.", RemoteApproved, mergeTestPath);
                        using (var r = new Repository(mergeTestRoot))
                        {
                            var remoteApprovedCanonical = string.Format("refs/remotes/{0}/{1}", RemoteApproved, BranchRemoteApproved);
                            if (r.Branches.Any(x => x.CanonicalName == remoteApprovedCanonical)
                                && r.Branches.Any(x => x.Name == BranchLocalTeamCity))
                            {
                                r.Checkout(r.Branches[BranchLocalTeamCity]);

                                r.Config.Set("user.name", GitConfigName);
                                r.Config.Set("user.email", GitConfigEmail);

                                var mergeResult = r.Merge(r.Branches[remoteApprovedCanonical].Tip, Committer);
                                if (mergeResult.Status == MergeStatus.Conflicts)
                                    Logger.Log.Write(MonitorServiceHost.Service.PollService, string.Format("Failed to merge branch #{0} with #{1} in merge test repo. Pull from {2}#{3} will be aborted.", BranchLocalApproved, BranchLocalTeamCity, RemoteApproved, BranchRemoteApproved), EventLogEntryType.Warning);
                                else
                                {
                                    mergeWorks = true;
                                    Logger.Log.Write(MonitorServiceHost.Service.PollService, "Merged branch #{0} with #{1} in merge test repo. Pull from {2}#{3} will follow.", BranchLocalApproved, BranchLocalTeamCity, RemoteApproved, BranchRemoteApproved);
                                }
                            }
                            else
                            {
                                Logger.Log.Write(MonitorServiceHost.Service.PollService, "Missing branches, no can merge!", EventLogEntryType.Warning);
                            }
                        }
                    }
                }
                if (mergeWorks)
                {
                    lock (TeamCityRepoLock)
                    {
                        if (TeamCityRoot == null)
                            TeamCityRoot = Repository.Init(TeamCityConfigFolder);
                        using (var r = new Repository(TeamCityRoot))
                        {
                            //todo: if this fucks up teamcity, got to stop the teamcity service here...
                            r.Checkout(r.Branches[BranchLocalApproved]);
                            r.Fetch(RemoteApproved);
                            r.Checkout(r.Branches[BranchLocalTeamCity]);
                            var mergeResult = r.Merge(r.Branches[BranchLocalApproved].Tip, Committer);
                            if (mergeResult.Status == MergeStatus.Conflicts)
                                Logger.Log.Write(MonitorServiceHost.Service.PollService, string.Format("Failed to merge branch #{0} with #{1} in merge test repo. Pull from {2}#{3} will be aborted.", BranchLocalApproved, BranchLocalTeamCity, RemoteApproved, BranchRemoteApproved), EventLogEntryType.Warning);
                            else
                                Logger.Log.Write(MonitorServiceHost.Service.PollService, "Merged branch #{0} with #{1} in merge test repo. Pull from {2}#{3} will follow.", BranchLocalApproved, BranchLocalTeamCity, RemoteApproved, BranchRemoteApproved);
                            //todo: and restart teamcity service here...
                        }
                    }
                }
            }
            catch (NonFastForwardException e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, "The remote repository is out of sync with the local repository. Changes have not been synced to remote.", EventLogEntryType.Warning);
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
            }
            catch (Exception e)
            {
                Logger.Log.Write(MonitorServiceHost.Service.WatchService, e);
                throw;
            }
        }

        #endregion
    }
}
