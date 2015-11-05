using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
					CheckUpdatesOnce(hostControl);
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
					LogError(exc.Message);
				}
				catch (InvalidOperationException exc)
				{
					if (!exc.Message.StartsWith("Unable to find version"))
						LogError(exc.ToString());
				}
				catch (Exception exc)
				{
					LogError(exc.ToString());
				}

				if (_terminationEvent.WaitOne(_core.LogSearchShipperConfig.UpdateCheckingPeriod))
					break;
			}
		}

		private void CheckUpdatesOnce(HostControl hostControl)
		{
			var packageId = Const.AppName;
			var curAssemblyPath = Assembly.GetExecutingAssembly().Location;
			var appPath = Path.GetDirectoryName(curAssemblyPath);
			var updateUrl = _core.LogSearchShipperConfig.NugetServerUrl;

			if (!JunctionPoint.Exists(appPath))
			{
				LogError(string.Format("Invalid app folder structure: \"{0}\". Turned off auto updates.", appPath));
				throw new ThreadInterruptedException();
			}

			var appParentPath = Path.GetDirectoryName(appPath);
			var updateDataPath = Path.Combine(appParentPath, "UpdateData");

			if (Directory.Exists(updateDataPath))
				FileUtil.Cleanup(updateDataPath, "*.*", false, true);

			Log.Info(new
			{
				Category = Const.LogCategory.InternalDiagnostic,
				Message = string.Format("Auto update URL: {0}", updateUrl),
				IsPreProductionEnvironment = _core.LogSearchShipperConfig.IsPreProductionEnvironment,
			});

			if (string.IsNullOrEmpty(updateUrl))
				return;

			var repo = PackageRepositoryFactory.Default.CreateRepository(updateUrl);
			var lastPackage = GetLastPackage(repo, packageId);
			var updateVersion = lastPackage.Version.Version;

			var curVersion = new Version(FileVersionInfo.GetVersionInfo(curAssemblyPath).ProductVersion);

			if (updateVersion <= curVersion)
				return;

			var packageManager = new PackageManager(repo, updateDataPath);
			packageManager.InstallPackage(packageId, lastPackage.Version, true, false);

			Log.Info(new
			{
				Category = Const.LogCategory.InternalDiagnostic,
				Message = "Updating LogSearchShipper",
				OldVersion = curVersion.ToString(),
				NewVersion = updateVersion.ToString(),
			});

			var packagePath = Path.Combine(updateDataPath, packageId + "." + lastPackage.Version);
			var updateDeploymentPath = Path.Combine(appParentPath, "v" + lastPackage.Version);
			var updatedCurrentPath = Path.Combine(appParentPath, "current");
			var configPath = FindConfigPath(Dns.GetHostName(), Path.Combine(packagePath, @"content\net45\Config"));
			var packageBinPath = Path.Combine(packagePath, "lib");

			Copy(packageBinPath, updateDeploymentPath, UpdateFileTypes);
			Copy(appPath, updateDeploymentPath, new[] { "*.log" });
			if (Copy(configPath, updateDeploymentPath, new[] { "*.config" }) < 1)
				throw new ApplicationException("Update package - no config files");

			var updaterPath = Path.Combine(updateDeploymentPath, "Updater.exe");

			var appMode = GetAppMode(hostControl);
			var startingName = (appMode == AppMode.Service) ? ServiceName : "LogSearchShipper.exe";
			var args = string.Format("{0} {1} \"{2}\" \"{3}\" \"{4}\"", Process.GetCurrentProcess().Id, appMode,
				EscapeCommandLineArg(startingName), EscapeCommandLineArg(updateDeploymentPath),
				EscapeCommandLineArg(updatedCurrentPath));

			Process.Start(new ProcessStartInfo
			{
				WorkingDirectory = updateDeploymentPath,
				FileName = updaterPath,
				Arguments = args,
			});
			hostControl.Stop();
		}

		private IPackage GetLastPackage(IPackageRepository repo, string packageId)
		{
			var packages = repo.FindPackagesById(packageId).ToList();
			packages.RemoveAll(val => !val.IsListed());
			if (_core.LogSearchShipperConfig.IsPreProductionEnvironment != true)
				packages.RemoveAll(val => !val.IsReleaseVersion());
			if (packages.Count == 0)
				throw new ApplicationException("No update package is found");
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

		static void LogInfo(string message)
		{
			Log.Info(new
			{
				Category = Const.LogCategory.InternalDiagnostic,
				Message = message,
			});
		}

		static void LogError(string message)
		{
			Log.Error(new
			{
				Category = Const.LogCategory.InternalDiagnostic,
				Message = message,
			});
		}

		static int Copy(string sourcePath, string targetPath, string[] fileTypes)
		{
			var res = 0;

			if (!Directory.Exists(targetPath))
				Directory.CreateDirectory(targetPath);

			foreach (var wildcard in fileTypes)
			{
				foreach (var file in Directory.GetFiles(sourcePath, wildcard, SearchOption.AllDirectories))
				{
					var targetFilePath = Path.Combine(targetPath, Path.GetFileName(file));
					File.Copy(file, targetFilePath, true);
					res++;
				}
			}

			return res;
		}

		static string FindConfigPath(string hostName, string configsBasePath)
		{
			var dirs = Directory.GetDirectories(configsBasePath, "*", SearchOption.AllDirectories);
			dirs = dirs.Select(dir => ToRelativePath(dir, configsBasePath)).ToArray();

			var hosts = ExtractHostConfigs(dirs);

			string relativePath;
			if (!hosts.TryGetValue(hostName, out relativePath))
				throw new ApplicationException(string.Format("Config for host \"{0}\" is not found", hostName));
			var res = Path.Combine(configsBasePath, relativePath);

			LogInfo(string.Format("Use config file: \"{0}\"", res));
			return res;
		}

		private static Dictionary<string, string> ExtractHostConfigs(string[] dirs)
		{
			var hosts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var dir in dirs)
			{
				var parts = dir.Split('\\');
				if (parts.Length != 4)
					continue;

				var curHostName = parts.Last();
				string fullPath;
				if (hosts.TryGetValue(curHostName, out fullPath))
				{
					LogError(string.Format("Duplicate configs in the update package: \"{0}\" \"{1}\"", fullPath, dir));
					continue;
				}

				hosts.Add(curHostName, dir);
			}
			return hosts;
		}

		static string ToRelativePath(string filePath, string refPath)
		{
			var pathNormalized = Path.GetFullPath(filePath);

			var refNormalized = Path.GetFullPath(refPath);
			refNormalized = refNormalized.TrimEnd('\\', '/');

			if (!pathNormalized.StartsWith(refNormalized))
				throw new ApplicationException(string.Format("Invalid reference path: {0}", refPath));
			var res = pathNormalized.Substring(refNormalized.Length + 1);
			return res;
		}

		private static readonly string[] UpdateFileTypes = { "*.exe", "*.dll", "*.pdb", "*.xml" };
	}
}
