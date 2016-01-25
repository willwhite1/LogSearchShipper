using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using GainCapital.AutoUpdate;
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

		private readonly ManualResetEvent _terminationEvent = new ManualResetEvent(false);

		public bool Start(HostControl hostControl)
		{
			var curAssembly = typeof(MainClass).Assembly;
			Log.Info(new
				{
					Message = "Process info",
					MainProcessVersion = curAssembly.GetName().Version.ToString(),
					Mode = UpdateChecker.GetAppMode(hostControl).ToString(),
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

			_updateChecker = new UpdateChecker(hostControl,
				new UpdatingInfo
				{
					NugetAppName = Const.AppName,
					ServiceName = ServiceName,
					Update = (packagePath, updateDeploymentPath) =>
					{
						var configPath = FindConfigPath(Dns.GetHostName(), Path.Combine(packagePath, @"content\net45\Config"));
						if (UpdateChecker.Copy(configPath, updateDeploymentPath, new[] { "*.config" }) < 1)
							throw new ApplicationException("Update package - no config files");
					},
				});
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
				if (parts.Length != 4 || !string.Equals(parts[2], "LogSearchShipper"))
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

		private UpdateChecker _updateChecker;
	}
}
