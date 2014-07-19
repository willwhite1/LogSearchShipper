using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.Resources;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogProcessManager
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (NxLogProcessManager));
		private readonly string _dataFolder;
		private readonly TimeSpan _waitForNxLogProcessToExitBeforeKilling = TimeSpan.FromSeconds(1);
		private string _nxLogFolder;
		private string _nxLogFile;
		private Process _process;

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

		public int Start()
		{
			//should wait until started
			ExtractNXLog();
			SetupConfigFile();

			string executablePath = Path.Combine(BinFolder, "nxlog.exe");
			string arguments = "-f -c " + ConfigFile;
			_log.InfoFormat("Running {0} {1}", executablePath, arguments);

			_process = new Process
			{
				StartInfo =
				{
					FileName = executablePath,
					Arguments = arguments,
					RedirectStandardInput = true,
					CreateNoWindow = true,
					UseShellExecute = false
				},
			};

			_process.Start();

			_log.InfoFormat("nxlog.exe running with PID: {0}", _process.Id);

			WatchAndLogNxLogFileOutput();

			return _process.Id;
		}

		/// <summary>
		/// Start a background task to log nxlog process output every 250ms
		/// </summary>
		private void WatchAndLogNxLogFileOutput()
		{
			Task.Run(() =>
			{
			 var _nxLogOutputParser = new NxLogOutputParser();
				using (FileStream fs = new FileStream(NxLogFile,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite))
				{
					using (StreamReader sr = new StreamReader(fs))
					{
						while (!_process.HasExited) // reading the old data
						{
							var lines = sr.ReadToEnd();
							foreach (var line in lines.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries))
							{
							 var logEvent = _nxLogOutputParser.Parse(line.Trim());
							 _log.Logger.Log(_nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
							}
							Thread.Sleep(TimeSpan.FromMilliseconds(250));
						}
					}
				}
			});
		}

	  public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

		private bool _disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
            }

            _disposed = true;
        }
    }

    ~NxLogProcessManager()
    {
        Dispose(false);
    }

		private void StopNxLogProcess()
		{
		 if (_process == null || _process.HasExited)
			return;

		 _process.StandardInput.Close();

		 if (_process == null || _process.HasExited)
			return;

		 _log.Info("Trying to close nxlog.exe gracefully by sending Ctrl-C");
		 Win32.AttachConsole((uint)_process.Id);
		 Win32.SetConsoleCtrlHandler(delegate { return true; }, true); //Set CtrlHandler to ignore
		 Win32.GenerateConsoleCtrlEvent(Win32.CtrlType.CtrlCEvent, 0);

		 if (_process == null || _process.HasExited)
			return;

		 // close console forcefully if not finished within allowed timeout
		 _log.InfoFormat("Waiting for voluntary nxlog.exe exit: Timeout={0}", _waitForNxLogProcessToExitBeforeKilling);
		 var exited = _process.WaitForExit(Convert.ToInt32(_waitForNxLogProcessToExitBeforeKilling.TotalMilliseconds));
			if (exited) return;

			_log.InfoFormat("Closing the nxlog.exe forcefully");
			_process.Kill();
		}

		private void CleanupNxLogProcessResources()
		{

			Thread.Sleep(TimeSpan.FromMilliseconds(100));

			//Cleanup
			try
			{
			 _log.InfoFormat("Deleting folder {0}", BinFolder);
			 Directory.Delete(BinFolder, true);
			 
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

			try
			{
			 _log.InfoFormat("Deleting NXLog file {0}", NxLogFile);
			 File.Delete(NxLogFile);
			}
			catch (Exception)
			{
			 //Wait a bit more, then try again
			 try
			 {
				_log.InfoFormat("Failed to delete {0}.  Waiting 1 sec, then trying again", NxLogFile);
				Thread.Sleep(TimeSpan.FromMilliseconds(1000));
				File.Delete(NxLogFile);
			 }
			 catch (Exception e)
			 {
				_log.Warn(string.Format("Unable to delete {0}.  Giving up.", NxLogFile), e);
			 }
			}

		}

		public void Stop()
		{
			_log.Info("Stopping and cleaning up nxlog.exe process.");

			StopNxLogProcess();
			CleanupNxLogProcessResources();

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
LogLevel	{0}
LogFile		{1}
	
ModuleDir	{2}\modules
CacheDir	{3}
PidFile		{3}\nxlog.pid
SpoolDir	{4}

<Extension syslog>
		Module	xm_syslog
</Extension>

{5}
{6}
{7}
{8}
{9}
",
				_log.IsDebugEnabled ? "DEBUG" : "INFO",
				NxLogFile,
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

		public string NxLogFile
		{
			get
			{
				if (string.IsNullOrEmpty(_nxLogFile))
				{
					_nxLogFile = Path.GetTempFileName();
				}
			 return _nxLogFile;
			}
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