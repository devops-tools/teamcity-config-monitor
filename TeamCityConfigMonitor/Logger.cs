using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace TeamCityConfigMonitor
{
    class Logger
    {
        public const string ServiceName = "WatchService";
        private static Logger _instance;
        public static Logger Log { get { return _instance ?? (_instance = new Logger()); } }

        private EventLog _eventLog;

        Logger()
        {
            Install();
        }

        public void Uninstall()
        {
            if (EventLog.SourceExists(ServiceName))
                EventLog.DeleteEventSource(ServiceName);
        }

        public void Install()
        {
            if (!Environment.UserInteractive)
            {
                if (!EventLog.SourceExists(ServiceName))
                {
                    EventLog.CreateEventSource(ServiceName, string.Concat(ServiceName, "Log"));
                    return;
                }
                _eventLog = new EventLog
                {
                    Source = ServiceName,
                    EnableRaisingEvents = true
                };
            }
        }

        public void Write(string entry, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            if (Environment.UserInteractive)
            {
                var consoleColors = new Dictionary<EventLogEntryType, ConsoleColor> {
                    { EventLogEntryType.Warning, ConsoleColor.DarkYellow },
                    { EventLogEntryType.Error, ConsoleColor.DarkRed },
                    { EventLogEntryType.FailureAudit, ConsoleColor.Red },
                    { EventLogEntryType.SuccessAudit, ConsoleColor.DarkGreen },
                    { EventLogEntryType.Information, ConsoleColor.Green },
                };
                Console.ForegroundColor = consoleColors[entryType];
                Console.Write("{0} ", DateTimeOffset.Now);
                Console.ResetColor();
                Console.Write("{0}\n", entry);
            }
            else
                _eventLog.WriteEntry(entry, entryType);
        }

        public void Write(string entry, params object[] args)
        {
            Write(string.Format(entry, args));
        }

        public void Write(Exception exception)
        {
            if (Environment.UserInteractive)
                Console.WriteLine("{0} {1}\n{2}", DateTimeOffset.Now, exception.Message, exception.StackTrace);
            else
                _eventLog.WriteEntry(string.Concat(exception.Message, "\n", exception.StackTrace), EventLogEntryType.Error);
        }
    }
}
