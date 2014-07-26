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
		private string _nxBinFolder;
		private string _nxLogFile;
		private string _maxNxLogFileSize = "1M";
		private string _rotateNxLogFileEvery = "1 min";
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
				if (!string.IsNullOrEmpty(_nxBinFolder)) return _nxBinFolder;

				_nxBinFolder = Path.Combine(DataFolder,"nxlog" );
				Directory.CreateDirectory(_nxBinFolder);
				return _nxBinFolder;
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
			ExtractNXLog();
			SetupConfigFile();
			StartNxLogProcess();

			return _process.Id;
		}

		internal void StartNxLogProcess()
		{
			string executablePath = Path.Combine(BinFolder, "nxlog.exe");
			string arguments = string.Format("-f -c \"{0}\"", ConfigFile);
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

			// Start a background task to log nxlog process output every 250ms
			Task.Run(() => new NxLogFileWatcher(this).WatchAndLog());
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

		public void Stop()
		{
			if (_process == null || _process.HasExited)
			 return;

			try
			{
			 _log.Info("Trying to close nxlog.exe gracefully");
			 _process.StandardInput.Close();

			 if (_process == null || _process.HasExited)
				return;

				_log.InfoFormat("Waiting for voluntary nxlog.exe exit: Timeout={0}", _waitForNxLogProcessToExitBeforeKilling);
				_process.WaitForExit(Convert.ToInt32(_waitForNxLogProcessToExitBeforeKilling.TotalMilliseconds));
			}
			finally
			{
			 // close console forcefully if not finished within allowed timeout
				if (_process != null || !_process.HasExited)
				{
				 _log.InfoFormat("Closing the nxlog.exe forcefully");
				 _process.Kill();
				}
				
			}
		}

		/// <summary>
		///  We're expecting a config that looks something like this:
		///  define BIN_FOLDER C:\Users\Andrei\AppData\Local\Temp\nxlog-81f27590cf4a4095915358b867c030af
		///  define DATA_FOLDER C:\Dev\LogSearchShipper\data
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

<Extension fileop>
    Module      xm_fileop

    # Check the size of our log file every {2}, rotate if larger than {3}, keeping a maximum of 1 files
    <Schedule>
        Every   {2}
        Exec    if (file_size('{1}') >= {3}) file_cycle('{1}', 1);
    </Schedule>
</Extension>
	
ModuleDir	{4}\modules
CacheDir	{5}
PidFile		{5}\nxlog.pid
SpoolDir	{6}

<Extension syslog>
		Module	xm_syslog
</Extension>

{7}
{8}
{9}
{10}
{11}
",
				_log.IsDebugEnabled ? "DEBUG" : "INFO",
				NxLogFile,
				RotateNxLogFileEvery,
				MaxNxLogFileSize,
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

		public string MaxNxLogFileSize
		{
			get { return _maxNxLogFileSize; }
			set { _maxNxLogFileSize = value; }
		}

		public string NxLogFile
		{
			get
			{
				if (string.IsNullOrEmpty(_nxLogFile))
				{
				 _nxLogFile = Path.Combine(DataFolder, "nxlog.log");
				}
			 return _nxLogFile;
			}
		}

		public string RotateNxLogFileEvery
		{
			get { return _rotateNxLogFileEvery; }
			set { _rotateNxLogFileEvery = value; }
		}

		public Process NxLogProcess
		{
			get { return _process; }
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
			allInputs = allInputs.TrimEnd(new[] { ',' });

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

			_log.Info(string.Format("BinFolder => {0}", BinFolder));

			string zipFile = Path.Combine(BinFolder, "nxlog.zip");
			if (File.Exists(zipFile)) return;

			_log.Info(string.Format("Extracting nxlog.zip => {0}", BinFolder));
			using (var fStream = new FileStream(zipFile, FileMode.Create))
			{
				fStream.Write(Resource.nxlog_ce_2_7_1191_zip,
					0, Resource.nxlog_ce_2_7_1191_zip.Length);
			}

			using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read))
			{
				archive.ExtractToDirectory(BinFolder);
			}
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
	Exec	$path = file_name(); $type = ""{2}""; ",
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