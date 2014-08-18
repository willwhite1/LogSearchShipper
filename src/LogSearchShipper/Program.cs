using System;
using System.Threading;
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

		private volatile bool _terminate;

		public bool Start(HostControl hostControl)
		{
			var thread = new Thread(args => WatchForExitKey(hostControl));
			thread.Start();

			_LogSearchShipperProcessManager = new LogSearchShipperProcessManager();
			_LogSearchShipperProcessManager.Start();

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_terminate = true;

			_log.Debug("Stop: Calling LogSearchShipperProcessManager.Stop()");
			_LogSearchShipperProcessManager.Stop();
			_log.Debug("Stop: LogSearchShipperProcessManager.Stop() completed");

			return true;
		}

		void WatchForExitKey(HostControl hostControl)
		{
			try
			{
				while (!_terminate)
				{
					if (Console.KeyAvailable)
					{
						var ch = Console.ReadKey();
						if (ch.KeyChar == 'q')
						{
							Stop(hostControl);
							Environment.Exit(0);
						}
					}
					Thread.Yield();
				}
			}
			catch (InvalidOperationException)
			{
				// no console is attached
			}
		}
	}
}