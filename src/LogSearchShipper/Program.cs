using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using log4net;
using log4net.Config;
using NuGet;
using Topshelf;

using LogSearchShipper.Core;
using LogSearchShipper.Updater;


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
				x.RunAsNetworkService();
				x.StartAutomatically();

				x.SetDescription("LogSearchShipper - forwards (Windows) log files to Logsearch cluster");
				x.SetDisplayName("LogSearchShipper");
				x.SetServiceName("LogSearchShipper");

				x.EnableServiceRecovery(rc =>
				{
					rc.RestartService(1); // restart the service after 1 minute
				});

				x.UseLog4Net();
				x.UseLinuxIfAvailable();
			});
		}
	}

	public class LogSearchShipperService : ServiceControl
	{
		public string ServiceName;

		private static readonly ILog Log = LogManager.GetLogger(typeof(LogSearchShipperService));
		private LogSearchShipperProcessManager _logSearchShipperProcessManager;

		private readonly ManualResetEvent _terminationEvent = new ManualResetEvent(false);

		private Thread _updateThread;

		public bool Start(HostControl hostControl)
		{
			var curAssembly = typeof(MainClass).Assembly;
			Log.Info(new { MainProcessVersion = curAssembly.GetName().Version.ToString() });

			var thread = new Thread(args => WatchForExitKey(hostControl))
			{
				IsBackground = true,
			};
			thread.Start();

			_updateThread = new Thread(args => CheckForUpdates(hostControl))
			{
				IsBackground = true,
			};
			_updateThread.Start();

			_logSearchShipperProcessManager = new LogSearchShipperProcessManager
			{
				ServiceName = ServiceName
			};

			_logSearchShipperProcessManager.RegisterService();
			_logSearchShipperProcessManager.Start();

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_terminationEvent.Set();

			Log.Debug("Stop: Calling LogSearchShipperProcessManager.Stop()");

			if (_logSearchShipperProcessManager != null)
			{
				_logSearchShipperProcessManager.Stop();
				Log.Debug("Stop: LogSearchShipperProcessManager.Stop() completed");

				_logSearchShipperProcessManager.Dispose();
				_logSearchShipperProcessManager = null;
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
					StopApplication(hostControl);
			}
		}

		private void StopApplication(HostControl hostControl)
		{
			Stop(hostControl);
			Environment.Exit(0);
		}

		void CheckForUpdates(HostControl hostControl)
		{
			while (true)
			{
				try
				{
					var packageId = Const.AppName;

					var repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
					var packages = repo.FindPackagesById(packageId).ToList();
					var lastPackage = packages.Max(val => val.Version);
					var updateVersion = lastPackage.Version;

					var curAssemblyPath = Assembly.GetExecutingAssembly().Location;
					var curVersion = new Version(FileVersionInfo.GetVersionInfo(curAssemblyPath).ProductVersion);

					if (updateVersion > curVersion)
					{
					}
				}
				catch (ThreadInterruptedException)
				{
					break;
				}
				catch (ThreadAbortException)
				{
					break;
				}
				catch (ApplicationException exc)
				{
					Log.Error(exc.Message);
				}
				catch (Exception exc)
				{
					Log.Error(exc.ToString());
				}

				if (_terminationEvent.WaitOne(TimeSpan.FromMinutes(1)))
					break;
			}
		}
	}
}
