using System;
using System.Threading;
using System.Timers;
using Topshelf;

namespace DummyServiceWithLogRolling
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            HostFactory.Run(x =>
            {
                x.Service<DummyService>(s =>
                {
                    s.ConstructUsing(name => new DummyService());
                    s.WhenStarted(tc =>
                    {
                        tc.Start();
                    });
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsNetworkService();
                x.StartAutomatically();

                x.SetDescription("DummyServiceWithLogRolling");
                x.SetDisplayName("DummyServiceWithLogRolling");
                x.SetServiceName("DummyServiceWithLogRolling");

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                x.UseLog4Net();
            });
        }
    }

    public class DummyService
    {
        private System.Timers.Timer _timer;
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DummyService));
        public void Start()
        {
            _log.Info("starting");
            _timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(100).TotalMilliseconds);
            _timer.Elapsed += new ElapsedEventHandler(doWork);
            _timer.Enabled = true;
        }

        private static void doWork(object source, ElapsedEventArgs e)
        {
            _log.Info("Doing work... (filling log file with *): " + new String('*', 500));
        }

        public void Stop()
        {
            _log.Info("stopping");
            _timer.Enabled = false;
        }
    }
}
