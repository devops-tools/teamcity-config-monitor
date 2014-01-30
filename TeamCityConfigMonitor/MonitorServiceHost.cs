using System;
using System.ComponentModel;

namespace TeamCityConfigMonitor
{
    public static class MonitorServiceHost
    {
        public enum Service
        {
            [Description("ServiceHost")]
            [Service(Name = "TeamCityConfigMonitor", Description = "TeamCity Config Monitor")]
            ServiceHost,

            [Description("WatchService")]
            [Service(Name = "WatchService", Description = "Monitors TeamCity configuration directory and commits changes to Git source control")]
            WatchService,

            [Description("PollService")]
            [Service(Name = "PollService", Description = "Monitors TeamCity remote Git repository and merges changes to configuration directory")]
            PollService
        }
    }

    public class ServiceAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}