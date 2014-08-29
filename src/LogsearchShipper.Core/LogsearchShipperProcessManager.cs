using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;

namespace LogSearchShipper.Core
{
	public class LogSearchShipperProcessManager
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (LogSearchShipperProcessManager));

		private readonly Dictionary<string, Timer> _environmentDiagramLoggingTimers = new Dictionary<string, Timer>();
		private readonly List<FileSystemWatcher> _watchedConfigFiles = new List<FileSystemWatcher>();

		public NxLogProcessManager NxLogProcessManager { get; private set; }

		public LogSearchShipperSection LogSearchShipperConfig
		{
			get
			{
				var LogSearchShipperConfig =
					ConfigurationManager.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;
				Debug.Assert(LogSearchShipperConfig != null, "LogSearchShipperConfig != null");
				return LogSearchShipperConfig;
			}
		}

		public int Start()
		{
			if (!Directory.Exists(LogSearchShipperConfig.DataFolder))
			{
				Directory.CreateDirectory(LogSearchShipperConfig.DataFolder);
			}
			NxLogProcessManager = new NxLogProcessManager(LogSearchShipperConfig.DataFolder,
				LogSearchShipperConfig.ShipperServiceUsername, LogSearchShipperConfig.ShipperServicePassword);

			SetupInputFiles();
			NxLogProcessManager.OutputSyslog = new SyslogEndpoint(LogSearchShipperConfig.IngestorHost,
				LogSearchShipperConfig.IngestorPort);

			var processId = NxLogProcessManager.Start();

			var configChanging = new CodeBlockLocker {isBusy = false};

			WhenConfigFileChanges(() =>
			{
				if (configChanging.isBusy)
				{
					_log.Info("Already in the process of updating config; ignoring trigger");
					return;
				}

				lock (configChanging)
				{
					configChanging.isBusy = true;

					_log.Info("Updating config and restarting shipping...");
					NxLogProcessManager.Stop();
					SetupInputFiles();
					NxLogProcessManager.Start();

					configChanging.isBusy = false;
				}
			});

			return processId;
		}

		public void Stop()
		{
			_log.Info("Stopping Environment Diagram logger...");
			foreach (Timer environmentDiagramLoggingTimer in _environmentDiagramLoggingTimers.Values)
			{
				environmentDiagramLoggingTimer.Dispose();
			}
			_log.Info("Stopping EDB file watchers...");
			foreach (FileSystemWatcher watchedConfigFile in _watchedConfigFiles)
			{
				watchedConfigFile.EnableRaisingEvents = false;
				watchedConfigFile.Dispose();
			}

			_log.Info("Stopping nxlog.exe...");
			NxLogProcessManager.Stop();
		}

		private void SetupInputFiles()
		{
			var watches = new List<FileWatchElement>();

			ExtractFileWatchers(LogSearchShipperConfig, watches);
			ExtractEDBFileWatchers(LogSearchShipperConfig, watches);

			NxLogProcessManager.InputFiles = watches;
		}

		private void WhenConfigFileChanges(Action actionsToRun)
		{
			foreach (FileSystemWatcher watcher in _watchedConfigFiles)
			{
				watcher.Changed += (s, e) =>
				{
					_log.InfoFormat("Detected change in file: {0}", e.FullPath);
					actionsToRun();
				};
				watcher.EnableRaisingEvents = true;
			}
		}

		private void AddWatchedConfigFile(string filePath)
		{
			string fullPath = Path.GetFullPath(filePath);
			if (WatcherAlreadyExists(fullPath)) return;
			var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
			_watchedConfigFiles.Add(watcher);
		}

		private bool WatcherAlreadyExists(string fullPath)
		{
			foreach (FileSystemWatcher watcher in _watchedConfigFiles)
			{
				if (Path.Combine(watcher.Path, watcher.Filter) == fullPath)
					return true;
			}
			return false;
		}


		private static void ExtractFileWatchers(LogSearchShipperSection LogSearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogSearchShipperConfig.FileWatchers.Count; i++)
			{
				watches.Add(LogSearchShipperConfig.FileWatchers[i]);
			}
		}

		private void ExtractEDBFileWatchers(LogSearchShipperSection LogSearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogSearchShipperConfig.EDBFileWatchers.Count; i++)
			{
				EnvironmentWatchElement envWatchElement = LogSearchShipperConfig.EDBFileWatchers[i];

				StartLoggingEnvironmentData(envWatchElement);
				AddWatchedConfigFile(envWatchElement.DataFile);

				var parser = new EDBFileWatchParser(envWatchElement);
				watches.AddRange(parser.ToFileWatchCollection());
			}
		}

		/// <summary>
		///  Logs the environment diagram data as extracted from the EDB file that is being used to determine what log files to
		///  ship
		/// </summary>
		/// <param name="edbDataFilePath"></param>
		private void StartLoggingEnvironmentData(EnvironmentWatchElement envWatchElement)
		{
			string key = envWatchElement.DataFile;
			if (_environmentDiagramLoggingTimers.ContainsKey(key))
			{
				_environmentDiagramLoggingTimers[key].Dispose();
			}

			var timer = new Timer(
				LogEnvironmentData, envWatchElement, 0, //Run once immediately
				Convert.ToInt64(TimeSpan.FromMinutes(envWatchElement.LogEnvironmentDiagramDataEveryMinutes).TotalMilliseconds)
				);

			_environmentDiagramLoggingTimers[key] = timer;
		}

		public static void LogEnvironmentData(object state)
		{
			var parser = new EDBFileWatchParser((EnvironmentWatchElement) state);
			IEnumerable<EDBEnvironment> environments = parser.GenerateLogsearchEnvironmentDiagram();

			_log.Info(string.Format("Logged environment diagram data for {0}", string.Join(",", environments.Select(e => e.Name))));

			LogManager.GetLogger("EnvironmentDiagramLogger").Info(new {Environments = environments});
		}

		public class CodeBlockLocker
		{
			public bool isBusy { get; set; }
		}
	}
}