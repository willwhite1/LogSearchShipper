using System;
using System.Diagnostics;
using System.IO;

namespace LogstashForwarder.Core
{
	public class LogstashForwarderProcessManager
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LogstashForwarderProcessManager));

		static Process _process;

		public LogstashForwarderProcessManager ()
		{

		}

		public void Start() {
			var path = Path.Combine(Path.GetTempPath(), "tmp.logsearch-ciapi_latency_monitor-bot");
			var exeFilePath = Path.Combine(path, "lumberjack.exe");
			var dataPath = Path.Combine(path, "data");
			var logFilePath = Path.Combine(dataPath, Process.GetCurrentProcess().Id + "log.txt");

			var processes = Process.GetProcessesByName("lumberjack");
			if (processes.Length == 0)
			{
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				else
					WipeDirectory(path);

				if (!Directory.Exists(dataPath))
					Directory.CreateDirectory(dataPath);
				else
					WipeDirectory(dataPath);

				TryUpdateLumberjack(path, exeFilePath);
				StartProcess(exeFilePath, path);
			}
		}

		public void Stop()
		{
			if (_process == null)
				return;
			_process.StandardInput.Close (); // send the close process signal
			_process.WaitForExit (5 * 1000);
			_process.Kill ();
		}

		private void StartProcess(string exeFilePath, string path)
		{
			var startInfo = new ProcessStartInfo(exeFilePath)
			{
				Arguments = "-config " + GetConfigPath(path),
				WorkingDirectory = path,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			_process = Process.Start(startInfo);

			_process.OutputDataReceived += (s, e) => _log.Info("Lumberjack: " + e.Data);
			_process.BeginOutputReadLine();

			_process.ErrorDataReceived += (s, e) => _log.Info("Lumberjack: " + e.Data);
			_process.BeginErrorReadLine();
		}

        private string _server;
        private string _log_type;
		private void TryUpdateLumberjack(string path, string exeFilePath)
		{
            if (!System.IO.File.Exists(exeFilePath))
            {
                var dataPath = Path.Combine(path, "data");

                var serverInfoString = _server;
                if (string.IsNullOrEmpty(serverInfoString))
                    throw new ApplicationException("LumberjackServer is not set");
                var serverInfo = serverInfoString.Split('|');
                var serverUrl = serverInfo[0];
                var certificatePath = NormalizePath(serverInfo[1]);

                string outputPath = Path.Combine(path, "lumberjack.exe");
                using (FileStream fStream = new FileStream(outputPath, FileMode.Create))
                {
                    fStream.Write(Resources.Resource.go_logstash_forwarder_exe, 
                        0, Resources.Resource.go_logstash_forwarder_exe.Length);
                }

                var configPath = GetConfigPath(path);
                var config = Resources.Resource.go_logstash_forwarder_config;
                config = config.Replace("{0}", serverUrl)
                    .Replace("{1}", certificatePath)
                    .Replace("{2}", NormalizePath(Path.Combine(dataPath, "*")))
                    .Replace("{3}", _log_type);
                File.WriteAllText(configPath, config);
            }
		}


		private static void WipeDirectory(string path)
		{
			foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
			{
				File.Delete(file);
			}
		}

		private static string NormalizePath(string val)
		{
			var res = val.Replace("\\", "\\\\");
			return res;
		}

		static string GetConfigPath(string path)
		{
			return Path.Combine(path, "go-logstash-forwarder.conf");
		}

		static bool IsWindows ()
		{
			return Environment.OSVersion.VersionString.Contains ("Windows");
		}
	}
}

