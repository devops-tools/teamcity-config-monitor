using System.ServiceProcess;

namespace TeamCityConfigMonitor
{
    partial class PollService : ServiceBase
    {
        private readonly Poller _poller;
        public PollService()
        {
            AutoLog = false;
            InitializeComponent();
            _poller = new Poller();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log.Write(MonitorServiceHost.Service.PollService, "{0} starting.", GetType().Name);
            _poller.Start();
            Logger.Log.Write(MonitorServiceHost.Service.PollService, "{0} started.", GetType().Name);
        }

        protected override void OnStop()
        {
            Logger.Log.Write(MonitorServiceHost.Service.PollService, "{0} stopping.", GetType().Name);
            _poller.Stop();
            Logger.Log.Write(MonitorServiceHost.Service.PollService, "{0} stopped.", GetType().Name);
        }
    }
}
