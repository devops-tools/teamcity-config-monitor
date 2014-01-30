using System;
using System.Configuration;
using System.Security.Permissions;
using System.Threading;


namespace TeamCityConfigMonitor
{
    public class Poller
    {
        public static void Poll()
        {
            var poller = new Poller();
            poller.Start();
        }

        private Thread _worker;
        private AutoResetEvent _reset;
        private static readonly TimeSpan PollFrequency = new TimeSpan(0, 0, Convert.ToInt32(ConfigurationManager.AppSettings.Get("GitPollFrequency")));

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Start()
        {
            _worker = new Thread(Work);
            _reset = new AutoResetEvent(false);
            _worker.Start();
        }

        private void Work()
        {
            Git.Instance.CreateApproved();
            while (!_reset.WaitOne(PollFrequency))
            {
                Git.Instance.PullApproved();
            }
        }

        public void Stop()
        {
            _reset.Set();
            _worker.Join();
        }
    }
}