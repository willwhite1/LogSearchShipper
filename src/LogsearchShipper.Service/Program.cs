using System;
using LogsearchShipper.Core;
using Topshelf;

namespace LogsearchShipper.Service
{
	class MainClass
	{
		public static void Main (string[] args)
		{
		    log4net.Config.XmlConfigurator.Configure();

            HostFactory.Run(x =>
            {
                x.Service<LogsearchShipperService>(s =>
                {
                    s.ConstructUsing(name => new LogsearchShipperService());
                    s.WhenStarted(tc =>
                    {
                        tc.Start();
                    });
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsNetworkService();
                x.StartAutomatically();

				x.SetDescription("Logsearch Shipper.NET - forwards (Windows) log files to Logsearch cluster");
				x.SetDisplayName("Logsearch Shipper.NET");
				x.SetServiceName("logsearch_shipper_net");

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                x.UseLog4Net();
				x.UseLinuxIfAvailable();
            });
		}
	}

    public class LogsearchShipperService
    {
        private LogsearchShipperProcessManager _LogsearchShipperProcessManager;
        public void Start()
        {
            _LogsearchShipperProcessManager = new LogsearchShipperProcessManager();
            _LogsearchShipperProcessManager.Start();
        }

        public void Stop()
        {
            _LogsearchShipperProcessManager.Stop();
        }
    }
}
