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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Press any key to stop.\n");
                Console.ResetColor();
                Git.Instance.Init();
                Watcher.Watch();
                Console.ReadKey();
            }
            else
            {
                try
                {
                    ServiceBase.Run(new WatchService());
                }
                catch (Exception exception)
                {
                    Logger.Log.Write(exception);
                    throw;
                }
            }
        }
    }
}
