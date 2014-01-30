using System;
using System.ServiceProcess;

namespace TeamCityConfigMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                Console.Write("Watching TeamCity config. ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Press any key to stop.\n");
                Console.ResetColor();
                Git.Instance.InitWatcher();
                Watcher.Watch();
                Poller.Poll();
                Console.ReadKey();
            }
            else
            {
                try
                {
                    var services = new ServiceBase[]
                    {
                        new WatchService(),
                        new PollService()
                    };
                    ServiceBase.Run(services);
                }
                catch (Exception exception)
                {
                    Logger.Log.Write(MonitorServiceHost.Service.ServiceHost, exception);
                    throw;
                }
            }
        }
    }
}
