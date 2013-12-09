using System;
using LogstashForwarder.Core;
using Topshelf;

namespace LogstashForwarder.Service
{
	class MainClass
	{
		public static void Main (string[] args)
		{
		    log4net.Config.XmlConfigurator.Configure();

            HostFactory.Run(x =>
            {
                x.Service<LogstashForwarderService>(s =>
                {
                    s.ConstructUsing(name => new LogstashForwarderService());
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

    public class LogstashForwarderService
    {
        private LogstashForwarderProcessManager _logstashForwarderProcessManager;
        public void Start()
        {
            _logstashForwarderProcessManager = new LogstashForwarderProcessManager();
            _logstashForwarderProcessManager.Start();
        }

        public void Stop()
        {
            _logstashForwarderProcessManager.Stop();
        }
    }
}
