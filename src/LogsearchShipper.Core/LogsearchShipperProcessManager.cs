using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using log4net;
using LogsearchShipper.Core.ConfigurationSections;
using LogsearchShipper.Core.Resources;

namespace LogsearchShipper.Core
{
	public class LogsearchShipperProcessManager
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (LogsearchShipperProcessManager));
		private static Process _process;

		private readonly Dictionary<string, Timer> _environmentDiagramLoggingTimers = new Dictionary<string, Timer>();
		private readonly List<FileSystemWatcher> _watchedConfigFiles = new List<FileSystemWatcher>();
		private string _nxLogFolder;
		private string _nxLogDataFolder;

		public string ConfigFile { get; private set; }

		public LogsearchShipperSection LogsearchShipperConfig
		{
			get
			{
				var LogsearchShipperConfig =
					ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
				Debug.Assert(LogsearchShipperConfig != null, "LogsearchShipperConfig != null");
				return LogsearchShipperConfig;
			}
		}

		public string NXLogBinFolder
		{
			get
			{
				if (string.IsNullOrEmpty(_nxLogFolder))
				{
					_nxLogFolder = Path.Combine(Path.GetTempPath(), "nxlog-" + Guid.NewGuid().ToString("N"));
					Directory.CreateDirectory(_nxLogFolder);
				}
				return _nxLogFolder;
			}
		}

		public string NXLogDataFolder
		{
				get
				{
					if (!string.IsNullOrEmpty(_nxLogDataFolder)) return _nxLogDataFolder;

					if (!Directory.Exists(LogsearchShipperConfig.DataFolder))
					{
							Directory.CreateDirectory(LogsearchShipperConfig.DataFolder);
					}

					_nxLogDataFolder = LogsearchShipperConfig.DataFolder;
					
					return _nxLogDataFolder;
				}
		}

		/// <summary>
		///     We're expecting a config that looks something like this:
		///     define BIN_FOLDER C:\Users\Andrei\AppData\Local\Temp\nxlog-81f27590cf4a4095915358b867c030af
		///     define DATA_FOLDER C:\Dev\logsearch-shipper.NET\data
		///     Moduledir %BIN_FOLDER%\modules
		///     CacheDir %DATA_FOLDER%
		///     Pidfile %DATA_FOLDER%\nxlog.pid
		///     SpoolDir %DATA_FOLDER%
		///     LogLevel INFO
		///     <Extension syslog>
		///         Module	xm_syslog
		///     </Extension>
		///     <Output out>
		///         Module	om_tcp
		///         Host	endpoint.example.com
		///         Port	5514
		///         Exec	to_syslog_ietf();
		///         Exec    log_debug("Sending syslog data: " + $raw_event);
		///         #OutputType	Syslog_TLS
		///     </Output>
		///     <Route 1>
		///         Path        file0, file1, file2, file3, file4 => out
		///     </Route>
		///     <Input file0>
		///         Module	im_file
		///         File	"myfile.log"
		///         ReadFromLast TRUE
		///         SavePos	TRUE
		///         CloseWhenIdle TRUE
		///         Exec	$path = file_name(); $type = "myfile_type"; $field1="field1 value"; $field2="field2 value" $Message =
		///         $raw_event;
		///     </Input>
		///     <Input file1>
		///         Module	im_file
		///         File	"C:\\Logs\\myfile.log"
		///         ReadFromLast TRUE
		///         SavePos	TRUE
		///         CloseWhenIdle TRUE
		///         Exec	$path = file_name(); $type = "type/subtype"; $field1="field1 value"; $Message = $raw_event;
		///     </Input>
		///     <Input file2>
		///         Module	im_file
		///         File	"\\\\PKH-PPE-APP10\\logs\\Apps\\PriceHistoryService\\log.log"
		///         ReadFromLast TRUE
		///         SavePos	TRUE
		///         CloseWhenIdle TRUE
		///         Exec	$path = file_name(); $type = "log4net"; $host="PKH-PPE-APP10"; $service="PriceHistoryService"; $Message =
		///         $raw_event;
		///     </Input>
		/// </summary>
		internal void SetupConfigFile()
		{
			var watches = new List<FileWatchElement>();

			ExtractFileWatchers(LogsearchShipperConfig, watches);
			ExtractEDBFileWatchers(LogsearchShipperConfig, watches);

			string config = string.Format(@"
LogLevel {0}

define BIN_FOLDER {1}
ModuleDir %BIN_FOLDER%\modules

define DATA_FOLDER {2}
CacheDir %DATA_FOLDER%
PidFile %DATA_FOLDER%\nxlog.pid
SpoolDir %DATA_FOLDER%

<Extension syslog>
		Module	xm_syslog
</Extension>

<Output out>
		Module	om_ssl
		Host	{3}
		Port	{4}
		AllowUntrusted TRUE
		Exec	to_syslog_ietf();
</Output>

{5}
", 
				_log.IsDebugEnabled  ? "DEBUG" : "INFO", 
				NXLogBinFolder,
				NXLogDataFolder,
				LogsearchShipperConfig.IngestorHost, LogsearchShipperConfig.IngestorPort,
				GenerateFilesSection(watches)
				);

			ConfigFile = Path.Combine(NXLogDataFolder, "nxlog.conf");
			File.WriteAllText(ConfigFile, config);
			_log.DebugFormat("NXLog config file: {0}", ConfigFile);
			_log.Debug(config);
		}

		public void Start()
		{
			ExtractNXLog();
			SetupConfigFile();
			StartProcess();

			var configChanging = new CodeBlockLocker {isBusy = false};

			WhenConfigFileChanges(() =>
			{
				if (configChanging.isBusy)
				{
					_log.Debug("Already in the process of updating config; ignoring trigger");
					return;
				}

				lock (configChanging)
				{
					configChanging.isBusy = true;

					_log.Debug("Updating config and restarting shipping...");
					Stop();
					SetupConfigFile();
					StartProcess();

					configChanging.isBusy = false;
				}
			});
		}

		private void WhenConfigFileChanges(Action actionsToRun)
		{
			foreach (FileSystemWatcher watcher in _watchedConfigFiles)
			{
				watcher.Changed += (s, e) =>
				{
					_log.DebugFormat("Detected change in file: {0}", e.FullPath);
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

		private void ExtractNXLog()
		{
			if (!Environment.OSVersion.VersionString.Contains("Windows"))
				throw new NotSupportedException("LogsearchShipperProcessManager only supports Windows");

			string zipFile = Path.Combine(NXLogBinFolder, "nxlog.zip");
			using (var fStream = new FileStream(zipFile, FileMode.Create))
			{
				fStream.Write(Resource.nxlog_ce_2_7_1191_zip,
					0, Resource.nxlog_ce_2_7_1191_zip.Length);
			}

			using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Update))
			{
				archive.ExtractToDirectory(NXLogBinFolder);
			}

			_log.Info(string.Format("NXLogBinFolder => {0}", NXLogBinFolder));
		}

		/// <summary>
		///     Generates a config block similar to the below:
		///     <Route 1>
		///         Path        file0, file1, file2, file3, file4 => out
		///     </Route>
		///     <Input file0>
		///         Module	im_file
		///         File	"myfile.log"
		///         ReadFromLast TRUE
		///         SavePos	TRUE
		///         CloseWhenIdle TRUE
		///         PollInterval 5
		///         DirCheckInterval 30
		///         Exec	$path = file_name(); $type = "myfile_type"; $field1="field1 value"; $field2="field2 value" $Message =
		///         $raw_event;
		///     </Input>
		///     <Input file1>
		///         ...
		/// </summary>
		/// <param name="watches"></param>
		/// <returns></returns>
		private static string GenerateFilesSection(List<FileWatchElement> watches)
		{
			string routeSection = @"
<Route 1>
   Path     ";
			string filesSection = "";

			for (int i = 0; i < watches.Count; i++)
			{
				routeSection += "file" + i + ",";
				FileWatchElement watch = watches[i];
				filesSection += string.Format(@"
<Input file{0}>
	Module	im_file
	File	""{1}""
	ReadFromLast TRUE
	SavePos	TRUE
	CloseWhenIdle TRUE
	PollInterval 5
	DirCheckInterval 10
	Exec	$path = file_name(); $name = ""logsearch-shipper.NET""; $module = ""nxlog""; $type = ""{2}""; ",
					i,
					watch.Files.Replace(@"\",@"\\"),
					watch.Type);

				foreach (FieldElement field in watch.Fields)
				{
					filesSection += string.Format(@"${0} = ""{1}""; ", field.Key, field.Value);
				}
				// Limit maximum message size to just less than 1MB; or NXLog dies with: ERROR string limit (1048576 bytes) reached
				filesSection += @"$Message = substr($raw_event, 0, 1040000);
</Input>
";
			}
			routeSection = routeSection.TrimEnd(new[] {','}) + " => out";
			routeSection += "\n</Route>";

			return routeSection + "\n" + filesSection;
		}

		private static void ExtractFileWatchers(LogsearchShipperSection LogsearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogsearchShipperConfig.FileWatchers.Count; i++)
			{
				watches.Add(LogsearchShipperConfig.FileWatchers[i]);
			}
		}

		private void ExtractEDBFileWatchers(LogsearchShipperSection LogsearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogsearchShipperConfig.EDBFileWatchers.Count; i++)
			{
				EnvironmentWatchElement envWatchElement = LogsearchShipperConfig.EDBFileWatchers[i];

				StartLoggingEnvironmentData(envWatchElement);
				AddWatchedConfigFile(envWatchElement.DataFile);

				var parser = new EDBFileWatchParser(envWatchElement);
				watches.AddRange(parser.ToFileWatchCollection());
			}
		}

		/// <summary>
		///     Logs the environment diagram data as extracted from the EDB file that is being used to determine what log files to
		///     ship
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


		public void Stop()
		{
			const int waitForGoLogstashForwarderToExitSeconds = 5;

			_log.Info("Stopping and cleaning up nxlog.exe process.");

			if (_process == null)
			{
				_log.Info("nxlog.exe process doesn't exist - nothing to Stop.");
				return;
			}

			_log.Info("sending Ctrl-C to nxlog.exe process so it can clean up");
			_process.StandardInput.WriteLine(char.ConvertFromUtf32(3));

			_log.InfoFormat("Waiting for {0}sec for nxlog.exe process to shut down gracefully", waitForGoLogstashForwarderToExitSeconds);
			_process.WaitForExit(waitForGoLogstashForwarderToExitSeconds*1000);
			if (!_process.HasExited)
			{
				_log.WarnFormat("Killing nxlog.exe process since it didn't exit within {0}sec",
					waitForGoLogstashForwarderToExitSeconds);
				_process.Kill();
			}

			//Cleanup
			try
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
				Directory.Delete(NXLogBinFolder, true);
				_log.InfoFormat("Deleting folder {0}", NXLogBinFolder);
			}
			catch (Exception)
			{
				//Wait a bit more, then try again
				try
				{
					_log.InfoFormat("Failed to delete {0}.  Waiting 1 sec, then trying again", NXLogBinFolder);
					Thread.Sleep(TimeSpan.FromMilliseconds(1000));
					Directory.Delete(NXLogBinFolder, true);
				}
				catch (Exception e)
				{
					_log.Warn(string.Format("Unable to delete {0}.  Giving up.", NXLogBinFolder), e);
				}
			}

			_log.Info("Successfully stopped and cleaned up nxlog.exe process");
		}

		private void StartProcess()
		{
			string nxlogExe = Path.Combine(NXLogBinFolder, "nxlog.exe");
			var startInfo = new ProcessStartInfo(nxlogExe)
			{
				Arguments = "-f -c " + ConfigFile,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			_log.InfoFormat("Running {0} {1}", nxlogExe, startInfo.Arguments);
			_process = Process.Start(startInfo);

			_process.OutputDataReceived += LogNxLogOutput;
			_process.BeginOutputReadLine();

			_process.ErrorDataReceived += LogNxLogOutput;
			_process.BeginErrorReadLine();
		}

		private void LogNxLogOutput(object s, DataReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Data)) return;
			
			var nxLogOutputParser = new NXLogOutputParser();
			var logEvent = nxLogOutputParser.Parse(e.Data);
			_log.Logger.Log(nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
		}

		public class CodeBlockLocker
		{
			public bool isBusy { get; set; }
		}
	}
}