using System;
using System.Diagnostics;
using System.IO;

namespace LogstashForwarder.Core
{
	public class LogstashForwarderProcessManager
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LogstashForwarderProcessManager));

		static Process _process;
        private string _server;
        private string _log_type;
        public string ConfigFile { get; private set; }
        public string GoLogstashForwarderFile { get; private set; }

	    public LogstashForwarderProcessManager()
	    {
	    }

	    public void Start()
	    {
	        ExtractGoLogstashForwarder();
            SetupConfigFile("TODO", "TODO", "TODO", "TODO");
            StartProcess();
		}

	    private void ExtractGoLogstashForwarder()
	    {
            if (!Environment.OSVersion.VersionString.Contains("Windows"))
                throw new NotSupportedException("LogstashForwarderProcessManager only supports Windows");

	        var tempFile = Path.GetTempFileName();
	        var exeFile = tempFile + ".exe";
            File.Move(tempFile, exeFile);

            using (var fStream = new FileStream(exeFile, FileMode.Create))
            {
                fStream.Write(Resources.Resource.go_logstash_forwarder_exe,
                    0, Resources.Resource.go_logstash_forwarder_exe.Length);
            }

            _log.Debug(string.Format("go-logstash-forwarder.exe => {0}", exeFile));

            GoLogstashForwarderFile = exeFile;
	    }

        private void SetupConfigFile(string serverUrl, string certificatePath, string logPath, string logType)
        {
            ConfigFile = Path.GetTempFileName();
            var config = Resources.Resource.go_logstash_forwarder_config;
            config = config
                .Replace("{0}", serverUrl)
                .Replace("{1}", certificatePath)
                .Replace("{2}", logPath)
                .Replace("{3}", logType);
            File.WriteAllText(ConfigFile, config);
        }

	    public void Stop()
		{
			if (_process == null)
				return;
			_process.StandardInput.Close (); // send the close process signal
			_process.WaitForExit (5 * 1000);
            if (!_process.HasExited)
            {
                _process.Kill();
            }

            //Cleanup
            File.Delete(GoLogstashForwarderFile);
		}

	    private void StartProcess()
		{
			var startInfo = new ProcessStartInfo(GoLogstashForwarderFile)
			{
                Arguments = "-config " + ConfigFile,
				WorkingDirectory = Path.GetTempPath(),
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
            _log.DebugFormat("go-logstash-forwarder.exe: running {0} -config {1}", GoLogstashForwarderFile, ConfigFile);
			_process = Process.Start(startInfo);

			_process.OutputDataReceived += (s, e) => _log.Info("go-logstash-forwarder.exe: " + e.Data);
			_process.BeginOutputReadLine();

            _process.ErrorDataReceived += (s, e) => _log.Info("go-logstash-forwarder.exe: " + e.Data);
			_process.BeginErrorReadLine();
		}
	}
}

