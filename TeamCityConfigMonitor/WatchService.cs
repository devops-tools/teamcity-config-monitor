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
            Logger.Log.Write("{0} started.", Logger.ServiceName);
            Git.Instance.Init();
            Watcher.Watch();
        }

        protected override void OnStop()
        {
            Logger.Log.Write("{0} stopped.", Logger.ServiceName);
        }
    }
}
