using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using log4net;
using log4net.Config;
using NuGet;
using Topshelf;
using Topshelf.Hosts;

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

		private readonly ManualResetEvent _terminationEvent = new ManualResetEvent(false);

		private Thread _updateThread;

		public bool Start(HostControl hostControl)
		{
			var curAssembly = typeof(MainClass).Assembly;
			Log.Info(new
				{
					Message = "Process info",
					MainProcessVersion = curAssembly.GetName().Version.ToString(),
					Mode = GetAppMode(hostControl).ToString(),
					User = Environment.UserName,
				});

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

			_core = new LogSearchShipperProcessManager
			{
				ServiceName = ServiceName
			};

			_core.RegisterService();
			_core.Start();

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_terminationEvent.Set();

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

		public static AppMode GetAppMode(HostControl hostControl)
		{
			return (hostControl is ConsoleRunHost) ? AppMode.Console : AppMode.Service;
		}

		void CheckForUpdates(HostControl hostControl)
		{
			while (true)
			{
				try
				{
					var packageId = Const.AppName;
					var curAssemblyPath = Assembly.GetExecutingAssembly().Location;
					var appPath = Path.GetDirectoryName(curAssemblyPath);
					var updateAreaPath = Path.Combine(appPath, "UpdateData");

					if (Directory.Exists(updateAreaPath))
						FileUtil.Cleanup(updateAreaPath, "*.*", false, true);

					var repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
					var lastPackage = GetLastPackage(repo, packageId);
					var updateVersion = lastPackage.Version.Version;

					var curVersion = new Version(FileVersionInfo.GetVersionInfo(curAssemblyPath).ProductVersion);

					if (updateVersion > curVersion)
					{
						Log.Info(new
						{
							Category = Const.LogCategory.InternalDiagnostic,
							Message = "Updating LogSearchShipper",
							OldVersion = curVersion.ToString(),
							NewVersion = updateVersion.ToString(),
						});

						var packageManager = new PackageManager(repo, updateAreaPath);
						packageManager.InstallPackage(packageId, lastPackage.Version);
						var packagePath = Path.Combine(updateAreaPath, packageId + "." + lastPackage.Version);
						var updaterPath = Path.Combine(packagePath, "lib", "net45", "Updater.exe");

						var appMode = GetAppMode(hostControl);
						var startingName = (appMode == AppMode.Service) ? ServiceName : "LogSearchShipper.exe";
						var args = string.Format("{0} {1} \"{2}\" \"{3}\" \"{4}\"", Process.GetCurrentProcess().Id, appMode,
							EscapeCommandLineArg(startingName), EscapeCommandLineArg(updateAreaPath), EscapeCommandLineArg(appPath));

						Process.Start(new ProcessStartInfo
							{
								WorkingDirectory = appPath,
								FileName = updaterPath,
								Arguments = args,
							});
						hostControl.Stop();
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
					Log.Error(new
						{
							Category = Const.LogCategory.InternalDiagnostic,
							Message = exc.Message,
						});
				}
				catch (Exception exc)
				{
					Log.Error(new
						{
							Category = Const.LogCategory.InternalDiagnostic,
							Message = exc.ToString(),
						});
				}

				if (_terminationEvent.WaitOne(TimeSpan.FromMinutes(1)))
					break;
			}
		}

		private static IPackage GetLastPackage(IPackageRepository repo, string packageId)
		{
			var packages = repo.FindPackagesById(packageId).ToList();
			packages.RemoveAll(val => !val.IsListed());
			packages.Sort((x, y) => x.Version.CompareTo(y.Version));
			var lastPackage = packages.Last();
			return lastPackage;
		}

		static string EscapeCommandLineArg(string val)
		{
			if (val.EndsWith("\\"))
				return val + "\\";
			return val;
		}
	}
}
