using log4net;
using log4net.Config;
using LogSearchShipper.Core;
using Topshelf;

namespace LogSearchShipper
{
	internal class MainClass
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (MainClass));

		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			HostFactory.Run(x =>
			{
				x.Service<LogSearchShipperService>();
				x.RunAsNetworkService();
				x.StartAutomatically();

				x.SetDescription("Logsearch Shipper.NET - forwards (Windows) log files to Logsearch cluster");
				x.SetDisplayName("Logsearch Shipper.NET");
				x.SetServiceName("logsearch_shipper_net");

				x.EnableServiceRecovery(rc => { rc.RestartService(1); // restart the service after 1 minute
				});

				x.UseLog4Net();
				x.UseLinuxIfAvailable();
			});
		}
	}

	public class LogSearchShipperService : ServiceControl
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (LogSearchShipperService));
		private LogSearchShipperProcessManager _LogSearchShipperProcessManager;

		public bool Start(HostControl hostControl)
		{
			_LogSearchShipperProcessManager = new LogSearchShipperProcessManager();
			_LogSearchShipperProcessManager.Start();

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_log.Debug("Stop: Calling LogSearchShipperProcessManager.Stop()");
			_LogSearchShipperProcessManager.Stop();
			_log.Debug("Stop: LogSearchShipperProcessManager.Stop() completed");

			return true;
		}
	}
}