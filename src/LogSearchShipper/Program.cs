using System;
using System.Linq;
using System.Threading;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository.Hierarchy;
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

			var repository = LogManager.GetRepository() as Hierarchy;
			if (repository != null)
			{
				var appenders = repository.GetAppenders();
				if (appenders != null)
				{
					if (appenders.FirstOrDefault(val => val.Name == "RollingFileAppenderDebug") == null)
					{
						// Write an excessive amount of info to LogSearchShipperDebug.log; NOT recommended to ship this to LogSearch
						var layout = new PatternLayout("%utcdate{ISO8601} [%thread] %-5level %logger - %.255message%newline");
						layout.ActivateOptions();
						var newAppender = new RollingFileAppender()
						{
							Name = "RollingFileAppenderDebug",
							File = "LogSearchShipperDebug.log",
							RollingStyle = RollingFileAppender.RollingMode.Size,
							AppendToFile = true,
							MaximumFileSize = "250MB",
							MaxSizeRollBackups = 2,
							Layout = layout,
						};
						newAppender.ActivateOptions();
						BasicConfigurator.Configure(newAppender);
					}
				}
			}

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