using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using log4net;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.Resources;

namespace LogSearchShipper.Core.NxLog
{
    public interface INxLogProcessManager
    {
        SyslogEndpoint InputSyslog { get; set; }
        List<FileWatchElement> InputFiles { get; set; }
        SyslogEndpoint OutputSyslog { get; set; }
        string OutputFile { get; set; }
        string ConfigFile { get; }
        string BinFolder { get; }
        string DataFolder { get; }
        string Config { get; }
        string MaxNxLogFileSize { get; set; }
        string NxLogFile { get; }
        string RotateNxLogFileEvery { get; set; }
        Process NxLogProcess { get; }
        int Start();
        void Dispose();
        void Stop();
    }

    public class NxLogProcessManager : INxLogProcessManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(NxLogProcessManager));
        private readonly string _dataFolder;
        private string _nxBinFolder;
        private string _nxLogFile;
        private string _maxNxLogFileSize = "1M";
        private string _rotateNxLogFileEvery = "1 min";
        private readonly string _serviceName;

        private readonly string _userName;
        private readonly string _password;

        private readonly object _sync = new object();
        private double _lastProcessorSecondsUsed;
        private double _lastNxlogProcessorSecondsUsed;
        private DateTime _lastProcessorUsageSentTime;
        private System.Timers.Timer _processorUsageReportingTimer;

        public NxLogProcessManager(string dataFolder, string serviceNamePrefix, string userName = null, string password = null)
        {
            _dataFolder = Path.GetFullPath(dataFolder);
            InputFiles = new List<FileWatchElement>();
            WinEventLogs = new List<WinEventWatchElement>();

            var configId = Path.GetFullPath(dataFolder);
            var hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(configId));
            configId = BitConverter.ToString(hash).Replace("-", "");

            _serviceName = "nxlog_" + configId;
            if (!string.IsNullOrEmpty(serviceNamePrefix))
                _serviceName = serviceNamePrefix + "_" + _serviceName;
            // limit max service name length.
            // See https://social.msdn.microsoft.com/Forums/vstudio/en-US/2c42c776-5e8b-4534-b11e-afaecabb3427/size-limitation-on-service-name-in-servicecontroller?forum=netfxbcl
            if (_serviceName.Length > 80)
                _serviceName = _serviceName.Substring(0, 80);

            _userName = userName;
            _password = password;

            ConfigFile = Path.Combine(DataFolder, "nxlog.conf");

            InitTimeZoneOffset();
        }

        public SyslogEndpoint InputSyslog { get; set; }
        public List<FileWatchElement> InputFiles { get; set; }
        public List<WinEventWatchElement> WinEventLogs { get; set; }

        public SyslogEndpoint OutputSyslog { get; set; }
        public string OutputFile { get; set; }

        public bool ResolveUncPaths { get; set; }

        public string ConfigFile { get; private set; }

        public string SessionId { get; set; }

        public double FilePollIntervalSeconds { get; set; }

        public double ProcessorUsageReportingIntervalSeconds { get; set; }

        public string BinFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(_nxBinFolder)) return _nxBinFolder;

                _nxBinFolder = Path.Combine(DataFolder, "nxlog");
                Directory.CreateDirectory(_nxBinFolder);
                return _nxBinFolder;
            }
        }

        public string DataFolder
        {
            get
            {
                if (!Directory.Exists(_dataFolder))
                    Directory.CreateDirectory(_dataFolder);
                return _dataFolder;
            }
        }

        public string Config { get; private set; }

        public void RegisterNxlogService()
        {
            _log.Info("NxLogProcessManager.RegisterNxlogService");

            VerifyNotDisposed();

            _curSessionId = SessionId == "*"
                ? Guid.NewGuid().ToString()
                : SessionId;

            ServiceControllerEx.StopService(_serviceName);
            ServiceControllerEx.DeleteService(_serviceName);

            ExtractNXLog();

            var executablePath = Path.Combine(BinFolder, "nxlog.exe");
            var serviceArguments = string.Format("\"{0}\" -c \"{1}\"", executablePath, ConfigFile);
            _log.InfoFormat("Running {0} as a service", serviceArguments);

            _log.InfoFormat("Truncating {0}", NxLogFile);
            if (File.Exists(NxLogFile))
                File.WriteAllText(NxLogFile, string.Empty);

            ServiceControllerEx.CreateService(_serviceName, serviceArguments, _userName, _password);
        }

        public void UnregisterNxlogService()
        {
            try
            {
                ServiceControllerEx.DeleteService(_serviceName);
            }
            catch (Exception exc)
            {
                _log.Error(exc);
            }
        }

        public int Start()
        {
            VerifyNotDisposed();

            _stopped = false;

            SetupConfigFile();
            StartNxLogProcess();

            return NxLogProcess.Id;
        }

        void StartNxLogProcess()
        {
            _log.Info("NxLogProcessManager.StartNxLogProcess");

            ServiceControllerEx.StartService(_serviceName);

            lock (_sync)
            {
                _lastProcessorUsageSentTime = DateTime.UtcNow;
                _lastProcessorSecondsUsed = 0;
                _lastNxlogProcessorSecondsUsed = 0;

                _processorUsageReportingTimer = new System.Timers.Timer
                {
                    AutoReset = false,
                    Interval = ProcessorUsageReportingIntervalSeconds * 1000
                };
                _processorUsageReportingTimer.Elapsed += _processorUsageReportingTimer_Elapsed;
                _processorUsageReportingTimer.Start();
            }
        }

        private void _processorUsageReportingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                lock (_sync)
                {
                    ReportCpuUsage(Process.GetCurrentProcess(), "ProcessorUsage",
                        ref _lastProcessorSecondsUsed, _lastProcessorUsageSentTime);
                    ReportCpuUsage(NxLogProcess, "NxlogProcessorUsage",
                        ref _lastNxlogProcessorSecondsUsed, _lastProcessorUsageSentTime);

                    _lastProcessorUsageSentTime = DateTime.UtcNow;
                }
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
            _processorUsageReportingTimer.Start();
        }

        private static void ReportCpuUsage(Process process, string name, ref double lastProcessorSecondsUsed, DateTime lastSentTime)
        {
            var processorSecondsUsed = process.TotalProcessorTime.TotalSeconds;
            if (lastProcessorSecondsUsed > 0)
            {
                var secondsPassed = (DateTime.UtcNow - lastSentTime).TotalSeconds;
                var averageProcessorUsage = ((processorSecondsUsed - lastProcessorSecondsUsed) / secondsPassed) * 100;

                var message = new Dictionary<string, object> { { name, averageProcessorUsage } };
                _log.Info(message);

                var messageNormalized = new Dictionary<string, object> { { name + "Normalized", averageProcessorUsage / Environment.ProcessorCount } };
                _log.Info(messageNormalized);
            }
            lastProcessorSecondsUsed = processorSecondsUsed;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void VerifyNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private volatile bool _disposed;
        private volatile bool _stopped;

        protected virtual void Dispose(bool disposing)
        {
            _log.Info("NxLogProcessManager.Dispose()");
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    UnregisterNxlogService();
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
            _log.Info("NxLogProcessManager.Stop");

            _stopped = true;
            lock (_sync)
            {
                if (_processorUsageReportingTimer != null)
                {
                    _processorUsageReportingTimer.Elapsed -= _processorUsageReportingTimer_Elapsed;
                    _processorUsageReportingTimer.Stop();
                }
            }

            _log.Info("Trying to close nxlog service gracefully");
            try
            {
                ServiceControllerEx.StopService(_serviceName);
            }
            catch (Exception exc)
            {
                _log.Error(exc);
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
        public void SetupConfigFile()
        {
            string config = string.Format(@"
LogLevel	{0}
LogFile		{1}

<Extension fileop>
	Module xm_fileop

	# Check the size of our log file every {2}, rotate if larger than {3}, keeping a maximum of 1 files
	<Schedule>
		Every {2}
		Exec if (file_size('{1}') >= {3}) file_cycle('{1}', 1);
	</Schedule>
</Extension>

<Extension json>
	Module	xm_json
</Extension>

ModuleDir	{4}\modules
CacheDir	{5}
PidFile		{5}\nxlog.pid
SpoolDir	{6}

<Extension syslog>
		Module	xm_syslog
</Extension>

<Extension multiline_default>
	Module	xm_multiline
	#HeaderLine == Anything not starting with whitespace
	HeaderLine	/^([^ ]+).*/
</Extension>

<Extension multiline_ci_log4net>
	Module	xm_multiline
	#HeaderLine == Any line starting with text (WARN, ERROR) and YYYY-
	HeaderLine	/^[A-Z]+[ \t]+\d{{4}}-.*/
</Extension>

{7}
{8}
{9}
{10}
{11}
{12}
{13}
{14}
",
                _log.IsDebugEnabled ? "DEBUG" : "INFO",
                NxLogFile,
                RotateNxLogFileEvery,
                MaxNxLogFileSize,
                Path.GetFullPath(BinFolder),
                Path.GetFullPath(DataFolder),
                Path.GetDirectoryName(Assembly.GetAssembly(typeof(NxLogProcessManager)).Location),
                GenerateOutputSyslogConfig(),
                GenerateOutputLogzConfig(),
                GenerateOutputFileConfig(),
                GenerateInputSyslogConfig(),
                GenerateInputFilesConfig(),
                GenerateInternalLoggingConfig(),
                GenerateWinEventWatchersConfig(),
                GenerateRoutes()
                );

            Config = config;
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
            get { return Process.GetProcessById(ServiceControllerEx.GetProcessId(_serviceName)); }
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
            for (int i = 0; i < WinEventLogs.Count; i++)
            {
                allInputs += "in_eventlog" + i + ",";
            }
            allInputs = allInputs.TrimEnd(',');

            allInputs = "in_internal," + allInputs;

            if (OutputSyslog != null)
            {
                routeSection += string.Format(@"
# The buffer needed to NOT loose events when Logstash restarts
<Processor buffer_out_syslog>
	Module pm_buffer
	# 100Mb buffer
	MaxSize 100000
	Type Mem
	# warn at 50Mb
	WarnLimit 50000
</Processor>
<Route route_to_syslog>
	Path {0} => buffer_out_syslog => out_syslog => out_logz
</Route>
", allInputs);
            }

            if (!string.IsNullOrEmpty(OutputFile))
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

            _log.InfoFormat("Receiving data from: syslog-tls://{0}:{1}", InputSyslog.Host, InputSyslog.Port);
            var certFile = Path.Combine(DataFolder, @"InputSyslog.crt");
            var keyFile = Path.Combine(DataFolder, @"InputSyslog.key");
            File.WriteAllText(certFile, @"-----BEGIN CERTIFICATE-----
MIICsDCCAhmgAwIBAgIJAJZZlYOII804MA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV
BAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX
aWRnaXRzIFB0eSBMdGQwHhcNMTQwNDA4MTUxNzA3WhcNMjQwNDA1MTUxNzA3WjBF
MQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50
ZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB
gQC2xI0wD26YOIEukuyokWDkKsFEvZxnadOEGT/9isf/mdiMk10NRZTF5bZU9ek9
Vj9HsO7sk2ays31bkjQVAw9/l2eQSDNKtnnWk28AiTEOvZq5ZYnc9PT5uyHQL4Uj
XJe2H8Dg/gfJhy9Ru9gpSSnRkYOXnwp2v6eJiQtzC6EG0QIDAQABo4GnMIGkMB0G
A1UdDgQWBBSyMMgqdi6u092zAgm03c0/JhP0bDB1BgNVHSMEbjBsgBSyMMgqdi6u
092zAgm03c0/JhP0bKFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUt
U3RhdGUxITAfBgNVBAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJAJZZlYOI
I804MAwGA1UdEwQFMAMBAf8wDQYJKoZIhvcNAQEFBQADgYEAif7W/VbSZ9GHfNDP
Cf+dsFTBk/1MpPW0cHXCX2lza42kbZ29PmhW1DSD+LkDcodL5wdVvTKSvJKmi5Cz
Y4O5DFyRcLQVTrhlUWfnUxTmaeWWzWyZe4RI98tTc2QHli6S9aeqczpa8k1aTiDp
XDPsPhpJjIepHXFRDaXUoV/T984=
-----END CERTIFICATE-----");
            File.WriteAllText(keyFile, @"-----BEGIN RSA PRIVATE KEY-----
MIICWwIBAAKBgQC2xI0wD26YOIEukuyokWDkKsFEvZxnadOEGT/9isf/mdiMk10N
RZTF5bZU9ek9Vj9HsO7sk2ays31bkjQVAw9/l2eQSDNKtnnWk28AiTEOvZq5ZYnc
9PT5uyHQL4UjXJe2H8Dg/gfJhy9Ru9gpSSnRkYOXnwp2v6eJiQtzC6EG0QIDAQAB
AoGASnrQmnw/cnLcWfFv1cXguTqfJfcrDI14r8VmaVkr5YJ5V9gZvHXVicvxwK+x
y9gg04NL6karPDme5TkwVju4DXJxwcT70QhFOG5EHFxij1HA8hgOU+K4X4FeNVbz
JPi27ktnJTsYs2Hq/UMoWygTvlTtyCsCytcAuo5jZRoy/cECQQDxoeFJiIH6hn2M
G/89USPeJKfiXIP8pSZCZi/FagVHRYKhgJ2MY4Uw4bmIxyiMO9VGSXhDpbnx1AAp
/6/KOod5AkEAwaKjDcI4c87DRQfPdORNBoKPTY1CuLgYUTIKBDUYUG0d/Vwy+USA
0NJI74B6sLCdfxLtK1d95XVuLRPDDGksGQJAMm0zI/JuFcdtegj5umUtlBWYR8BA
9z/L/T1wKMXYdihGe8fomTzHtgzVeHr/tkxiVPnONGfop1Qz+I/Yst6GGQJANAJ+
L1zikuCPfIQrieckdUIuQZNWv4zbIzwAir7EKB4W9w2Dt4ZZ3z0MUCA/VCQsOYyY
3ZJjg3V2QW9UbYn2SQJAMwhKLGhbuv5ge5K5H436Rl0NR2nZVaIgxez0Y8tVeTBT
rM8ETzoKmuLdiTl3uUhgJMtdOP8w7geYl8o1YP+3YQ==
-----END RSA PRIVATE KEY-----");

            return string.Format(@"
<Input in_syslog>
		Module	im_ssl
		Host	{0}
		Port	{1}
		CertFile {2}
		CertKeyFile {3}
		RequireCert FALSE
		AllowUntrusted TRUE
		Exec	parse_syslog_ietf();
</Input>",
                InputSyslog.Host, InputSyslog.Port, certFile, keyFile);
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
	Exec	if $Message $Message=replace($Message,""\n"",""¬"");
	Exec	to_syslog_ietf();
</Output>",
                OutputSyslog.Host, OutputSyslog.Port);
        }

        private string GenerateOutputLogzConfig()
        {
            var certFile = Path.Combine(DataFolder, @"logz.crt");
            File.WriteAllText(certFile, @"-----BEGIN CERTIFICATE-----
MIICsDCCAhmgAwIBAgIJAJZZlYOII804MA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV
BAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX
aWRnaXRzIFB0eSBMdGQwHhcNMTQwNDA4MTUxNzA3WhcNMjQwNDA1MTUxNzA3WjBF
MQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50
ZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB
gQC2xI0wD26YOIEukuyokWDkKsFEvZxnadOEGT/9isf/mdiMk10NRZTF5bZU9ek9
Vj9HsO7sk2ays31bkjQVAw9/l2eQSDNKtnnWk28AiTEOvZq5ZYnc9PT5uyHQL4Uj
XJe2H8Dg/gfJhy9Ru9gpSSnRkYOXnwp2v6eJiQtzC6EG0QIDAQABo4GnMIGkMB0G
A1UdDgQWBBSyMMgqdi6u092zAgm03c0/JhP0bDB1BgNVHSMEbjBsgBSyMMgqdi6u
092zAgm03c0/JhP0bKFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUt
U3RhdGUxITAfBgNVBAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJAJZZlYOI
I804MAwGA1UdEwQFMAMBAf8wDQYJKoZIhvcNAQEFBQADgYEAif7W/VbSZ9GHfNDP
Cf+dsFTBk/1MpPW0cHXCX2lza42kbZ29PmhW1DSD+LkDcodL5wdVvTKSvJKmi5Cz
Y4O5DFyRcLQVTrhlUWfnUxTmaeWWzWyZe4RI98tTc2QHli6S9aeqczpa8k1aTiDp
XDPsPhpJjIepHXFRDaXUoV/T984=
-----END CERTIFICATE-----");

            return string.Format(@"
<Output out_logz>
	Module  om_ssl
    CAFile  {0}
    AllowUntrusted TRUE
    Host    listener.logz.io    
    Port    5052
    Exec    $token=""{1}""; $message=$raw_event; to_json();
</Output>", certFile, OutputSyslog.Token);
        }

        private string GenerateOutputFileConfig()
        {
            if (string.IsNullOrEmpty(OutputFile)) return String.Empty;

            if (!File.Exists(OutputFile)) File.WriteAllText(OutputFile, string.Empty);
            _log.InfoFormat("Sending data to file: {0}", OutputFile);

            return string.Format(@"
<Output out_file>
	Module	om_file
	File	""{0}""
	Exec if $type != 'json' {{ $Message = $raw_event; to_json(); $type = 'json'; }}
</Output>",
                OutputFile.Replace(@"\", @"\\"));
        }

        private void ExtractNXLog()
        {
            if (!Environment.OSVersion.VersionString.Contains("Windows"))
                throw new NotSupportedException("NxLogProcessManager only supports Windows");

            _log.Info(string.Format("BinFolder => {0}", BinFolder));

            var zipProperty = typeof(Resource).GetProperties(BindingFlags.Static | BindingFlags.NonPublic).
                First(property => property.Name.StartsWith("nxlog_ce_"));
            var newZipFile = (byte[])zipProperty.GetValue(null);
            var zipFileName = Path.Combine(BinFolder, zipProperty.Name + ".zip");

            if (File.Exists(zipFileName) && File.Exists(Path.Combine(BinFolder, "nxlog.exe")))
            {
                _log.InfoFormat("'{0}' already exists", zipFileName);
                return;
            }

            foreach (var file in Directory.GetFiles(BinFolder, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }

            _log.Info(string.Format("Extracting nxlog.zip => {0}", BinFolder));
            using (var fStream = new FileStream(zipFileName, FileMode.Create))
            {
                fStream.Write(newZipFile, 0, newZipFile.Length);
            }

            using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
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
                var inputFile = InputFiles[i];
                filesSection += (inputFile.SourceTailer == TailerType.MT)
                    ? GenerateMtFileWatchConfig(inputFile, i)
                    : GenerateNormalFileWatchConfig(inputFile, i);
            }

            return filesSection;
        }

        string GenerateNormalFileWatchConfig(FileWatchElement inputFile, int i)
        {
            var res = "";
            var inputFileEscaped = PrepareFilePaths(inputFile.Files);

            _log.InfoFormat("Receiving data from file: {0}", inputFile.Files);
            res += string.Format(@"
<Input in_file{0}>
	Module	im_file
	InputType	{8}
	File	""{1}""
	ReadFromLast {2}
	SavePos	TRUE
	CloseWhenIdle {7}
	PollInterval {5}
	DirCheckInterval {6}
	Exec	$path = ""{3}""; $type = ""{4}"";
",
                i,
                inputFileEscaped,
                inputFile.ReadFromLast.ToString().ToUpper(),
                inputFile.Files,
                inputFile.Type,
                FilePollIntervalSeconds,
                FilePollIntervalSeconds * 2,
                inputFile.CloseWhenIdle.ToString().ToUpper(),
                inputFile.MultilineRule);

            res += AppendCustomFields(inputFile);

            // Limit maximum message size to just less than 1MB; or NXLog dies with: ERROR string limit (1048576 bytes) reached
            res += @"	Exec if $Message $Message = substr($raw_event, 0, 1040000);" + Environment.NewLine;

            if (inputFile.CustomNxlogConfig != null)
            {
                var customNxlog = inputFile.CustomNxlogConfig.Value;
                if (!string.IsNullOrWhiteSpace(customNxlog))
                    res += "\t" + customNxlog + Environment.NewLine;
            }

            res += GetSessionId();
            res += @"</Input>" + Environment.NewLine;

            return res;
        }

        private static string AppendCustomFields(IWatchElement watchElement)
        {
            if (watchElement.Fields.Count == 0)
                return "";
            var buf = new StringBuilder();
            foreach (FieldElement field in watchElement.Fields)
            {
                if (!field.Key.All(ch => Char.IsLetterOrDigit(ch) || ch == '/' || ch == '_'))
                {
                    var message = string.Format("fileWatch: '{0}' contains invalid field name '{1}' (must contain letters, digits, slashes and underscores only)",
                        watchElement.Key, field.Key);
                    throw new ApplicationException(message);
                }

                buf.AppendFormat(@"${0} = ""{1}""; ", field.Key, field.Value);
            }
            return "	Exec " + buf + Environment.NewLine;
        }

        string GenerateMtFileWatchConfig(FileWatchElement inputFile, int i)
        {
            var inputFileEscaped = PrepareFilePaths(inputFile.Files);
            var mainModulePath = new Uri(Process.GetCurrentProcess().MainModule.FileName).LocalPath;
            var exePath = Path.Combine(Path.GetDirectoryName(mainModulePath), "MtLogTailer.exe");
            var exePathEscaped = exePath.Replace(@"\", @"\\");

            var res = string.Format(@"
<Input in_file{0}>
	Module im_exec
	Command ""{1}""
	Arg ""{2}""
	Arg -readFromLast:{5}
	Restart True
	Exec $Message = string($raw_event);
	Exec $path = ""{3}""; $type = ""{4}"";
	Exec if $Message =~ /^(([^\t]+)\t)/ {{ $fullPath = $2; $Message = substr($Message, size($1)); }}
	Exec if $Message $Message = substr($Message, 0, 1040000);
", i, exePathEscaped, inputFileEscaped, inputFile.Files, inputFile.Type, inputFile.ReadFromLast.ToString().ToLower());

            res += AppendCustomFields(inputFile);
            res += GetSessionId();
            res += @"</Input>" + Environment.NewLine;

            return res;
        }

        private string GenerateInternalLoggingConfig()
        {
            var res = string.Format(@"
<Input in_internal>
	Module im_internal
	Exec $logger = 'nxlog.exe';
	Exec delete($SourceModuleType); delete($SourceModuleName); delete($SeverityValue); delete($ProcessID);
	Exec delete($SourceName); delete($EventReceivedTime); delete($Hostname);
	Exec rename_field('Severity', 'level');
	Exec $timestamp = strftime($EventTime, '%Y-%m-%dT%H:%M:%S' + '{0}'); delete($EventTime);

	Exec if string($Message) =~ /^failed to open/ $Category = 'MISSING_FILE';
	Exec if string($Message) =~ /^input file does not exist:/ $Category = 'MISSING_FILE';
	Exec if string($Message) =~ /^apr_stat failed on file/ $Category = 'MISSING_FILE';
	Exec rename_field('Message', 'nxlog_message');

	Exec to_json(); $type = 'json';
{1}
</Input>", _timeZoneText, GetSessionId());
            return res;
        }

        private string GenerateWinEventWatchersConfig()
        {
            var res = new StringBuilder();

            var i = 0;
            foreach (var cur in WinEventLogs)
            {
                res.Append(GenerateWinEventWatcherConfig(cur, i));
                i++;
            }

            return res.ToString();
        }

        private string GenerateWinEventWatcherConfig(WinEventWatchElement watcher, int i)
        {
            var res = string.Format(@"
<Input in_eventlog{0}>
	Module im_msvistalog
	ReadFromLast {1}
	Query <QueryList> \
			<Query Id=""0"">\
				<Select Path=""{2}"">{3}</Select>\
			</Query>\
		</QueryList>
{4}
", i, watcher.ReadFromLast.ToString().ToUpper(), watcher.Path, watcher.Query, GetSessionId());

            res += AppendCustomFields(watcher);

            // Limit maximum message size to just less than 1MB; or NXLog dies with: ERROR string limit (1048576 bytes) reached
            res += @"	Exec if $Message $Message = substr($raw_event, 0, 1040000);" + Environment.NewLine;

            res += string.Format(@"
	Exec $logger = 'nxlog.exe';
	Exec $service = 'WindowsEvents';
	Exec delete($SeverityValue);
	Exec rename_field('Severity', 'level');
	Exec $timestamp = strftime($EventTime, '%Y-%m-%dT%H:%M:%S' + '{0}'); delete($EventTime);
	Exec rename_field('Hostname', 'host');
	Exec delete ($EventID); delete ($EventType); delete ($Keywords); delete ($Task); delete ($RecordNumber); delete ($ProcessID);
	Exec delete ($ThreadID); delete ($Channel); delete ($EventReceivedTime);
	Exec if $level == 'WARNING' $level = 'WARN';
	Exec rename_field('Message', 'event_description');
	Exec to_json(); $type = 'json';
</Input>" + Environment.NewLine, _timeZoneText);

            return res;
        }

        void InitTimeZoneOffset()
        {
            // nxlog doesn't handle time zone correctly, so we need to set the correct time zone variable to be used in the nxlog config file
            var timeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            _timeZoneText = timeZoneOffset.ToString("hh\\:mm");
            var sign = (timeZoneOffset >= TimeSpan.Zero) ? "+" : "-";
            _timeZoneText = sign + _timeZoneText;
        }

        string GetSessionId()
        {
            if (string.IsNullOrEmpty(_curSessionId))
                return "";
            var res = string.Format("	Exec $sessionId = '{0}';" + Environment.NewLine, _curSessionId);
            return res;
        }

        string PrepareFilePaths(string val)
        {
            var res = val;

            if (ResolveUncPaths)
            {
                var localHostUncPath = @"\\" + Environment.MachineName + @"\";
                if (res.StartsWith(localHostUncPath, StringComparison.OrdinalIgnoreCase))
                {
                    var sharePath = res.Substring(localHostUncPath.Length);
                    var shareName = sharePath.Split(new[] { @"\" }, StringSplitOptions.None).First();
                    var pathInsideShare = sharePath.Substring(shareName.Length);

                    var shares = ListFileShares();
                    var matchingShares = shares.Where(cur => cur.Key.Equals(shareName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matchingShares.Count == 1)
                    {
                        var shareInfo = matchingShares.First();
                        var localPath = shareInfo.Value + pathInsideShare;
                        if (HasReadAccess(localPath))
                            res = shareInfo.Value + pathInsideShare;
                    }
                }
            }

            res = res.Replace(@"\", @"\\");
            return res;
        }

        Dictionary<string, string> ListFileShares()
        {
            var scope = new ManagementScope(@"\\localhost\root\CIMV2");
            scope.Connect();
            var worker = new ManagementObjectSearcher(scope, new ObjectQuery("select * from win32_share"));
            var shares = worker.Get().Cast<ManagementObject>().ToDictionary(
                share => share["Name"].ToString(), share => share["Path"].ToString());
            return shares;
        }

        bool HasReadAccess(string fileSpec)
        {
            var folderPath = Path.GetDirectoryName(fileSpec);
            if (!Directory.Exists(folderPath))
                return false;

            try
            {
                var filePattern = Path.GetFileName(fileSpec);
                var files = Directory.GetFiles(folderPath, filePattern);

                foreach (var file in files)
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception exc)
            {
                _log.Warn(exc);
                return false;
            }
        }

        private string _curSessionId;
        private string _timeZoneText;
    }
}