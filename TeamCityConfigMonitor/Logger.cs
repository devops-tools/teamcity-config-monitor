using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace TeamCityConfigMonitor
{
    class Logger
    {
        public const string EventLogName = "TeamCity Config Monitor";
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
                    EventLog.CreateEventSource(ServiceName, EventLogName);
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
                _eventLog.WriteEntry(entry, entryType);
        }

        public void Write(string entry, params object[] args)
        {
            Write(string.Format(entry, args));
        }

        public void Write(Exception exception)
        {
            if (Environment.UserInteractive)
                Write(exception.ToMessage(), EventLogEntryType.Error);
            else
                _eventLog.WriteEntry(exception.ToMessage(), EventLogEntryType.Error);
        }
    }

    public static class Extensions
    {
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
    }
}
