using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.Resources;
using RunProcess;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogProcessManager
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (NxLogProcessManager));
		private static readonly NxLogOutputParser _nxLogOutputParser = new NxLogOutputParser();
		private readonly string _dataFolder;
		private readonly TimeSpan _waitForNxLogProcessToExitBeforeKilling = TimeSpan.FromSeconds(1);
		private string _nxLogFolder;
		private ProcessHost _nxLogProcessHost;

		public NxLogProcessManager(string dataFolder)
		{
			_dataFolder = Path.GetFullPath(dataFolder);
			InputFiles = new List<FileWatchElement>();
		}

		public NxLogProcessManager() : this(Path.Combine(Path.GetTempPath(), "nxlog-data-" + Guid.NewGuid().ToString("N")))
		{
		}

		public SyslogEndpoint InputSyslog { get; set; }
		public List<FileWatchElement> InputFiles { get; set; }

		public SyslogEndpoint OutputSyslog { get; set; }
		public string OutputFile { get; set; }

		public string ConfigFile { get; private set; }

		public string BinFolder
		{
			get
			{
				if (!string.IsNullOrEmpty(_nxLogFolder)) return _nxLogFolder;

				_nxLogFolder = Path.Combine(Path.GetTempPath(), "nxlog-" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(_nxLogFolder);
				return _nxLogFolder;
			}
		}

		public string DataFolder
		{
			get
			{
				if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
				return _dataFolder;
			}
		}

		public string Config { get; private set; }

		public ProcessHost Start()
		{
			//should wait until started
			ExtractNXLog();
			SetupConfigFile();

			string executablePath = Path.Combine(BinFolder, "nxlog.exe");
			string arguments = "-f -c " + ConfigFile;
			_log.InfoFormat("Running {0} {1}", executablePath, arguments);

			_nxLogProcessHost = new ProcessHost(executablePath, DataFolder);

			_nxLogProcessHost.Start(arguments);

			//Wait up to 1 second for the process to start
			for (int i = 0; i < 10; i++)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
				if (_nxLogProcessHost.IsAlive())
					break;
			}

			//Start a background task to log nxlog process output every 250ms
			Task.Run(() =>
			{
				string leftOverLine = string.Empty;
				while (_nxLogProcessHost.IsAlive())
				{
					string stdOut = leftOverLine + _nxLogProcessHost.StdOut.ReadAllText(Encoding.Default);
					foreach (string line in stdOut.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries))
					{
						//Log complete lines, or store incomplete lines to be logged next time round
						if (line.EndsWith("\r"))
						{
							LogNxLogOutput(line.Trim());
						}
						else
						{
							leftOverLine = line;
						}
					}

					Thread.Sleep(TimeSpan.FromMilliseconds(250));
				}
			});

			if (!_nxLogProcessHost.IsAlive())
			{
				throw new NxLogStartException("nxlog.exe failed to start");
			}

			_log.InfoFormat("nxlog.exe running with PID: {0}", _nxLogProcessHost.ProcessId());
			return _nxLogProcessHost;
		}

		private static void LogNxLogOutput(string data)
		{
			if (string.IsNullOrEmpty(data)) return;

			NxLogOutputParser.NxLogEvent logEvent = _nxLogOutputParser.Parse(data);
			_log.Logger.Log(_nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
		}

		public void Stop()
		{
			_log.Info("Stopping and cleaning up nxlog.exe process.");

			if (_nxLogProcessHost == null || !_nxLogProcessHost.IsAlive())
			{
				_log.Info("nxlog.exe process doesn't exist - nothing to Stop.");
				return;
			}

			_log.Info("sending Ctrl-C to nxlog.exe process so it can clean up");
			_nxLogProcessHost.StdIn.WriteLine(Encoding.Default, char.ConvertFromUtf32(3));

			_log.InfoFormat("Waiting for {0}sec for nxlog.exe process to shut down gracefully",
				_waitForNxLogProcessToExitBeforeKilling.TotalSeconds);
			if (!_nxLogProcessHost.WaitForExit(_waitForNxLogProcessToExitBeforeKilling))
			{
				_log.WarnFormat("Killing nxlog.exe process since it didn't exit within {0}sec",
					_waitForNxLogProcessToExitBeforeKilling.TotalSeconds);
				_nxLogProcessHost.Kill();
			}

			//Cleanup
			try
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
				Directory.Delete(BinFolder, true);
				_log.InfoFormat("Deleting folder {0}", BinFolder);
			}
			catch (Exception)
			{
				//Wait a bit more, then try again
				try
				{
					_log.InfoFormat("Failed to delete {0}.  Waiting 1 sec, then trying again", BinFolder);
					Thread.Sleep(TimeSpan.FromMilliseconds(1000));
					Directory.Delete(BinFolder, true);
				}
				catch (Exception e)
				{
					_log.Warn(string.Format("Unable to delete {0}.  Giving up.", BinFolder), e);
				}
			}

			_log.Info("Successfully stopped and cleaned up nxlog.exe process");
		}

		/// <summary>
		///  We're expecting a config that looks something like this:
		///  define BIN_FOLDER C:\Users\Andrei\AppData\Local\Temp\nxlog-81f27590cf4a4095915358b867c030af
		///  define DATA_FOLDER C:\Dev\logsearch-shipper.NET\data
		///  Moduledir %BIN_FOLDER%\modules
		///  CacheDir %DATA_FOLDER%
		///  Pidfile %DATA_FOLDER%\nxlog.pid
		///  SpoolDir %DATA_FOLDER%
		///  LogLevel INFO
		///  <Extension syslog>
		///   Module	xm_syslog
		///  </Extension>
		///  <Output out>
		///   Module	om_tcp
		///   Host	endpoint.example.com
		///   Port	5514
		///   Exec	to_syslog_ietf();
		///   Exec    log_debug("Sending syslog data: " + $raw_event);
		///   #OutputType	Syslog_TLS
		///  </Output>
		///  <Route 1>
		///   Path        file0, file1, file2, file3, file4 => out
		///  </Route>
		///  <Input file0>
		///   Module	im_file
		///   File	"myfile.log"
		///   ReadFromLast TRUE
		///   SavePos	TRUE
		///   CloseWhenIdle TRUE
		///   Exec	$path = file_name(); $type = "myfile_type"; $field1="field1 value"; $field2="field2 value" $Message =
		///   $raw_event;
		///  </Input>
		///  <Input file1>
		///   Module	im_file
		///   File	"C:\\Logs\\myfile.log"
		///   ReadFromLast TRUE
		///   SavePos	TRUE
		///   CloseWhenIdle TRUE
		///   Exec	$path = file_name(); $type = "type/subtype"; $field1="field1 value"; $Message = $raw_event;
		///  </Input>
		///  <Input file2>
		///   Module	im_file
		///   File	"\\\\PKH-PPE-APP10\\logs\\Apps\\PriceHistoryService\\log.log"
		///   ReadFromLast TRUE
		///   SavePos	TRUE
		///   CloseWhenIdle TRUE
		///   Exec	$path = file_name(); $type = "log4net"; $host="PKH-PPE-APP10"; $service="PriceHistoryService"; $Message =
		///   $raw_event;
		///  </Input>
		/// </summary>
		internal void SetupConfigFile()
		{
			string config = string.Format(@"
LogLevel DEBUG

ModuleDir	{0}\modules
CacheDir	{1}
PidFile		{1}\nxlog.pid
SpoolDir	{2}

<Extension syslog>
		Module	xm_syslog
</Extension>

{3}
{4}
{5}
{6}
{7}
",
				Path.GetFullPath(BinFolder),
				Path.GetFullPath(DataFolder),
				Path.GetDirectoryName(Assembly.GetAssembly(typeof (NxLogProcessManager)).Location),
				GenerateOutputSyslogConfig(),
				GenerateOutputFileConfig(),
				GenerateInputSyslogConfig(),
				GenerateInputFilesConfig(),
				GenerateRoutes()
				);

			Config = config;
			ConfigFile = Path.Combine(DataFolder, "nxlog.conf");
			File.WriteAllText(ConfigFile, config);
			_log.InfoFormat("NXLog config file: {0}", ConfigFile);
		}

		/// <summary>
		///  Generates the route config eg:
		///  <Route to_syslog>
		///   Path        in_syslog, in_file0, in_file1, in_file2, in_file3, in_file4 => out_syslog
		///  </Route>
		///  <Route to_file>
		///   Path        in_syslog, in_file0, in_file1, in_file2, in_file3, in_file4 => out_file
		///  </Route>
		/// </summary>
		private string GenerateRoutes()
		{
			string routeSection = string.Empty;
			string allInputs = string.Empty;

			if (InputSyslog != null)
			{
				allInputs += "in_syslog,";
			}
			for (int i = 0; i < InputFiles.Count; i++)
			{
				allInputs += "in_file" + i + ",";
			}
			allInputs = allInputs.TrimEnd(new[] {','});

			if (OutputSyslog != null)
			{
				routeSection += string.Format(@"
# The buffer needed to NOT loose events when Logstash restarts
<Processor buffer_out_syslog>
    Module      pm_buffer
    # 100Mb buffer
    MaxSize 100000
    Type Mem
    # warn at 50Mb
    WarnLimit 50000
</Processor>
<Route route_to_syslog>
	Path {0} => buffer_out_syslog => out_syslog
</Route>
", allInputs);
			}

			if (OutputFile != null)
			{
				routeSection += string.Format(@"
<Route route_to_file>
	Path {0} => out_file
</Route>
", allInputs);
			}

			return routeSection;
		}

		private string GenerateInputSyslogConfig()
		{
			if (InputSyslog == null) return String.Empty;

			_log.InfoFormat("Recieving data from: syslog-tls://{0}:{1}", InputSyslog.Host, InputSyslog.Port);
			return string.Format(@"
<Input in_syslog>
		Module	im_ssl
		Host	{0}
		Port	{1}
		RequireCert FALSE
		AllowUntrusted TRUE
		Exec	parse_syslog_ietf();
</Input>",
				InputSyslog.Host, InputSyslog.Port);
		}

		private string GenerateOutputSyslogConfig()
		{
			if (OutputSyslog == null) return String.Empty;

			_log.InfoFormat("Sending data to: syslog-tls://{0}:{1}", OutputSyslog.Host, OutputSyslog.Port);
			return string.Format(@"
<Output out_syslog>
		Module	om_ssl
		Host	{0}
		Port	{1}
		AllowUntrusted TRUE
		Exec	to_syslog_ietf();
</Output>",
				OutputSyslog.Host, OutputSyslog.Port);
		}

		private string GenerateOutputFileConfig()
		{
			if (string.IsNullOrEmpty(OutputFile)) return String.Empty;

			if (!File.Exists(OutputFile)) File.WriteAllText(string.Empty, OutputFile);
			_log.InfoFormat("Sending data to file: {0}", OutputFile);

			return string.Format(@"
<Output out_file>
	Module	im_file
	File	""{0}""
</Output>",
				OutputFile);
		}

		private void ExtractNXLog()
		{
			if (!Environment.OSVersion.VersionString.Contains("Windows"))
				throw new NotSupportedException("NxLogProcessManager only supports Windows");

			string zipFile = Path.Combine(BinFolder, "nxlog.zip");
			using (var fStream = new FileStream(zipFile, FileMode.Create))
			{
				fStream.Write(Resource.nxlog_ce_2_7_1191_zip,
					0, Resource.nxlog_ce_2_7_1191_zip.Length);
			}

			using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Update))
			{
				archive.ExtractToDirectory(BinFolder);
			}

			_log.Info(string.Format("BinFolder => {0}", BinFolder));
		}

		/// <summary>
		///  Generates a config block for InputFile eg:
		///  <Input file0>
		///   Module	im_file
		///   File	"myfile.log"
		///   ReadFromLast TRUE
		///   SavePos	TRUE
		///   CloseWhenIdle TRUE
		///   PollInterval 5
		///   DirCheckInterval 30
		///   Exec	$path = file_name(); $type = "myfile_type"; $field1="field1 value"; $field2="field2 value" $Message =
		///   $raw_event;
		///  </Input>
		///  <Input file1>
		///   ...
		/// </summary>
		/// <returns></returns>
		private string GenerateInputFilesConfig()
		{
			string filesSection = "";

			for (int i = 0; i < InputFiles.Count; i++)
			{
				FileWatchElement inputFile = InputFiles[i];

				_log.InfoFormat("Recieving data from file: {0}", inputFile.Files);
				filesSection += string.Format(@"
<Input in_file{0}>
	Module	im_file
	File	""{1}""
	ReadFromLast TRUE
	SavePos	TRUE
	CloseWhenIdle TRUE
	PollInterval 5
	DirCheckInterval 10
	Exec	$path = file_name(); $name = ""logsearch-shipper.NET""; $module = ""nxlog""; $type = ""{2}""; ",
					i,
					inputFile.Files.Replace(@"\", @"\\"),
					inputFile.Type);

				foreach (FieldElement field in inputFile.Fields)
				{
					filesSection += string.Format(@"${0} = ""{1}""; ", field.Key, field.Value);
				}
				// Limit maximum message size to just less than 1MB; or NXLog dies with: ERROR string limit (1048576 bytes) reached
				filesSection += @"$Message = substr($raw_event, 0, 1040000);
</Input>
";
			}

			return filesSection;
		}
	}
}