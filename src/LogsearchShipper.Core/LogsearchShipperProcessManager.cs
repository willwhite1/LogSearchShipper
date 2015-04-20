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
		private static readonly ILog _log = LogManager.GetLogger(typeof(LogSearchShipperProcessManager));

		private readonly Dictionary<string, Timer> _environmentDiagramLoggingTimers = new Dictionary<string, Timer>();
		private readonly List<ConfigWatcher> _watchedConfigFiles = new List<ConfigWatcher>();

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
			_log.Info("LogSearchShipperProcessManager.Start");

			if (!Directory.Exists(LogSearchShipperConfig.DataFolder))
			{
				Directory.CreateDirectory(LogSearchShipperConfig.DataFolder);
			}
			NxLogProcessManager = new NxLogProcessManager(LogSearchShipperConfig.DataFolder,
				LogSearchShipperConfig.ShipperServiceUsername, LogSearchShipperConfig.ShipperServicePassword)
				{
					SessionId = LogSearchShipperConfig.SessionId,
					ProcessorUsageReportingIntervalSeconds = LogSearchShipperConfig.ProcessorUsageReportingIntervalSeconds,
					FilePollIntervalSeconds = LogSearchShipperConfig.FilePollIntervalSeconds,
					OutputFile = LogSearchShipperConfig.OutputFile,
				};

			SetupInputFiles();
			NxLogProcessManager.OutputSyslog = new SyslogEndpoint(LogSearchShipperConfig.IngestorHost,
				LogSearchShipperConfig.IngestorPort);

			var processId = NxLogProcessManager.Start();

			foreach (var watcher in _watchedConfigFiles)
				watcher.SubscribeConfigFileChanges(OnEdbConfigChange);

			return processId;
		}

		private void OnEdbConfigChange()
		{
			_log.Info("LogSearchShipperProcessManager - configs have changed");

			lock (_configChangingSync)
			{
				if (_configChanging)
				{
					_log.Info("Already in the process of updating config; ignoring trigger");
					return;
				}

				_configChanging = true;

				try
				{
					_log.Info("Updating config and restarting shipping...");
					NxLogProcessManager.Stop();
					SetupInputFiles();
					NxLogProcessManager.Start();
				}
				finally
				{
					_configChanging = false;
				}
			}
		}

		readonly object _configChangingSync = new object();
		private bool _configChanging;

		public void Stop()
		{
			_log.Info("LogSearchShipperProcessManager.Stop");

			_log.Info("Stopping Environment Diagram logger...");
			foreach (Timer environmentDiagramLoggingTimer in _environmentDiagramLoggingTimers.Values)
			{
				environmentDiagramLoggingTimer.Dispose();
			}
			_log.Info("Stopping EDB file watchers...");
			foreach (var watchedConfigFile in _watchedConfigFiles)
			{
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

		private void AddWatchedConfigFile(string filePath)
		{
			string fullPath = Path.GetFullPath(filePath);
			if (WatcherAlreadyExists(fullPath))
				return;
			var watcher = new ConfigWatcher(fullPath, _log);
			_watchedConfigFiles.Add(watcher);
		}

		private bool WatcherAlreadyExists(string fullPath)
		{
			foreach (var watcher in _watchedConfigFiles)
			{
				if (Path.Combine(watcher.Watcher.Path, watcher.Watcher.Filter) == fullPath)
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

			var period = Convert.ToInt64(TimeSpan.FromMinutes(envWatchElement.LogEnvironmentDiagramDataEveryMinutes).TotalMilliseconds);
			var timer = new Timer(
				LogEnvironmentData,
				envWatchElement, 0, //Run once immediately
				period);

			_environmentDiagramLoggingTimers[key] = timer;
		}

		public static void LogEnvironmentData(object state)
		{
			try
			{
				var parser = new EDBFileWatchParser((EnvironmentWatchElement)state);
				IEnumerable<EDBEnvironment> environments = parser.GenerateLogsearchEnvironmentDiagram();

				_log.Info(string.Format("Logged environment diagram data for {0}",
					string.Join(",", environments.Select(e => e.Name))));

				LogManager.GetLogger("EnvironmentDiagramLogger").Info(new { Environments = environments });

				EdbDataFormatter.ReportData(environments);
			}
			catch (Exception exc)
			{
				_log.Error(exc);
			}
		}
	}
}