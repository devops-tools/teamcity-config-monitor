using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TeamCityConfigMonitor
{
    class Logger
    {
        private static Logger _instance;
        public static Logger Log { get { return _instance ?? (_instance = new Logger()); } }

        private Dictionary<MonitorServiceHost.Service, EventLog> _eventLog;

        Logger()
        {
            Install();
        }

        public void Uninstall()
        {
            foreach (var serviceName in Enum.GetValues(typeof(MonitorServiceHost.Service)).Cast<MonitorServiceHost.Service>().Select(x => x.Get("Name")))
                if (EventLog.SourceExists(serviceName))
                    EventLog.DeleteEventSource(serviceName);
        }

        public void Install()
        {
            if (!Environment.UserInteractive)
            {
                foreach (var serviceName in Enum.GetValues(typeof(MonitorServiceHost.Service)).Cast<MonitorServiceHost.Service>().Select(x => x.Get("Name")))
                {
                    if (!EventLog.SourceExists(serviceName))
                        EventLog.CreateEventSource(serviceName, MonitorServiceHost.Service.ServiceHost.Get("Name"));
                }
                _eventLog = new Dictionary<MonitorServiceHost.Service, EventLog>
                {
                    { MonitorServiceHost.Service.WatchService, new EventLog { Source = MonitorServiceHost.Service.WatchService.ToString(), EnableRaisingEvents = true } },
                    { MonitorServiceHost.Service.PollService, new EventLog { Source = MonitorServiceHost.Service.PollService.ToString(), EnableRaisingEvents = true } }
                };
            }
        }

        public void Write(MonitorServiceHost.Service source, string entry, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            if (Environment.UserInteractive)
            {
                var consoleColors = new Dictionary<EventLogEntryType, ConsoleColor> {
                    { EventLogEntryType.Warning, ConsoleColor.Yellow },
                    { EventLogEntryType.Error, ConsoleColor.Red },
                    { EventLogEntryType.FailureAudit, ConsoleColor.DarkYellow },
                    { EventLogEntryType.SuccessAudit, ConsoleColor.DarkGreen },
                    { EventLogEntryType.Information, ConsoleColor.Green },
                };
                Console.ForegroundColor = consoleColors[entryType];
                Console.Write("{0} ", DateTimeOffset.Now);
                Console.ResetColor();
                Console.Write("{0}\n", entry);
            }
            else
                _eventLog[source].WriteEntry(entry, entryType);
        }

        public void Write(MonitorServiceHost.Service source, string entry, params object[] args)
        {
            Write(source, string.Format(entry, args));
        }

        public void Write(MonitorServiceHost.Service source, Exception exception)
        {
            if (Environment.UserInteractive)
                Write(source, exception.ToMessage(), EventLogEntryType.Error);
            else
                _eventLog[source].WriteEntry(exception.ToMessage(), EventLogEntryType.Error);
        }
    }
}
