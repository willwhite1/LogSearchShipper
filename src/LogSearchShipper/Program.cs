using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using log4net;
using log4net.Config;
using Topshelf;

using LogSearchShipper.Core;
using Const = LogSearchShipper.Core.Const;

namespace LogSearchShipper
{
    internal class MainClass
	{
		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			HostFactory.Run(x =>
			{
				x.Service(
					settings => new LogSearchShipperService
					{
						ServiceName = settings.ServiceName
					});
				x.RunAsLocalSystem();
				x.StartAutomatically();

				x.SetDescription("LogSearchShipper - forwards (Windows) log files to Logsearch cluster");
				x.SetDisplayName("LogSearchShipper");
				x.SetServiceName("LogSearchShipper");

				x.EnableServiceRecovery(rc =>
				{
					rc.RestartService(1); // restart the service after 1 minute
				});

				x.UseLog4Net();
			});
		}
	}

	public class LogSearchShipperService : ServiceControl
	{
		public string ServiceName;

		private static readonly ILog Log = LogManager.GetLogger(typeof(LogSearchShipperService));
		private LogSearchShipperProcessManager _core;

        private readonly UpdateChecker _updateChecker = new UpdateChecker();
		private readonly ManualResetEvent _terminationEvent = new ManualResetEvent(false);

		public bool Start(HostControl hostControl)
		{
			var curAssembly = typeof(MainClass).Assembly;
			Log.Info(new
				{
					Message = "Process info",
					MainProcessVersion = curAssembly.GetName().Version.ToString(),					
					User = Environment.UserName,
				});

			var thread = new Thread(args => WatchForExitKey(hostControl))
			{
				IsBackground = true,
			};
			thread.Start();

			_core = new LogSearchShipperProcessManager
			{
				ServiceName = ServiceName
			};

			_core.RegisterService();
			_core.Start();
		    _updateChecker.Start();

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_terminationEvent.Set();
			_updateChecker.Stop();

			Log.Debug("Stop: Calling LogSearchShipperProcessManager.Stop()");

			if (_core != null)
			{
				_core.Stop();
				Log.Debug("Stop: LogSearchShipperProcessManager.Stop() completed");

				_core.Dispose();
				_core = null;
			}

			return true;
		}

		// NOTE check keyboard exit commands, when running as a console application
		// 'q' is used when running with a redirected input, as it's impossible to send Ctrl+C in this case
		void WatchForExitKey(HostControl hostControl)
		{
			while (true)
			{
				if (_terminationEvent.WaitOne(TimeSpan.FromMilliseconds(1)))
					break;

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

				if (Char.ToLower(ch) == 'q')
					hostControl.Stop();
			}
		}

	}
}
