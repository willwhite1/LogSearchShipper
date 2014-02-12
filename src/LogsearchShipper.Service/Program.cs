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

                x.SetDescription("LogStash Forwarder.NET - forwards (Windows) log files to Logsearch cluster");
                x.SetDisplayName("LogStash Forwarder.NET");
                x.SetServiceName("logstash_forwarder_net");

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                x.UseLog4Net();
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
