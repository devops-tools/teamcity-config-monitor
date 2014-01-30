using System.ServiceProcess;

namespace TeamCityConfigMonitor
{
    public partial class WatchService : ServiceBase
    {
        public WatchService()
        {
            AutoLog = false;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log.Write(MonitorServiceHost.Service.WatchService, "{0} started.", GetType().Name);
            Git.Instance.InitWatcher();
            Watcher.Watch();
        }

        protected override void OnStop()
        {
            Logger.Log.Write(MonitorServiceHost.Service.WatchService, "{0} stopped.", GetType().Name);
        }
    }
}
