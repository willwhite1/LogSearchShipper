using System;
using System.Linq;
using System.Threading;

using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using LogSearchShipper.Appenders;
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
			ConfigureDefaultAppenders();

			HostFactory.Run(x =>
			{
				x.Service<LogSearchShipperService>();
				x.RunAsNetworkService();
				x.StartAutomatically();

				x.SetDescription("LogSearchShipper - forwards (Windows) log files to Logsearch cluster");
				x.SetDisplayName("LogSearchShipper");
				x.SetServiceName("LogSearchShipper");

				x.EnableServiceRecovery(rc => { rc.RestartService(1); // restart the service after 1 minute
				});

				x.UseLog4Net();
				x.UseLinuxIfAvailable();
			});
		}

		static void ConfigureDefaultAppenders()
		{
			var repository = LogManager.GetRepository() as Hierarchy;
			if (repository != null)
			{
				var appenders = repository.GetAppenders();
				if (appenders != null)
				{
					var debugAppender = appenders.FirstOrDefault(val => val.Name == "DebugLogAppender");
					if (debugAppender == null)
					{
						var newAppender = new DebugLogAppender();
						newAppender.ActivateOptions();
						BasicConfigurator.Configure(newAppender);
					}
				}
			}
		}
	}

	public class LogSearchShipperService : ServiceControl
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (LogSearchShipperService));
		private LogSearchShipperProcessManager _LogSearchShipperProcessManager;

		private volatile bool _terminate;

		public bool Start(HostControl hostControl)
		{
			var curAssembly = typeof(MainClass).Assembly;
			_log.Info(new { MainProcessVersion = curAssembly.GetName().Version.ToString() });

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
			while (!_terminate)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(1));

				char ch;

				try
				{
					if (!Console.KeyAvailable)
						continue;

					var tmp = Console.ReadKey();
					ch = tmp.KeyChar;
				}
				catch (InvalidOperationException)
				{
					// console input is redirected

					var tmp = Console.In.Read();
					if (tmp == -1)
						continue;

					ch = (char)tmp;
				}

				if (ch == 'q')
				{
					Stop(hostControl);
					Environment.Exit(0);
				}
			}
		}
	}
}