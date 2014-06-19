using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web;
using LogsearchShipper.Core.ConfigurationSections;
using System.Linq;
using System.Collections.Generic;

namespace LogsearchShipper.Core
{
	public class LogsearchShipperProcessManager
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LogsearchShipperProcessManager));
        private static readonly log4net.ILog _logLogstashForwarder = log4net.LogManager.GetLogger("go-logstash-forwarder.exe");
		
        private List<FileSystemWatcher> _watchedConfigFiles = new List<FileSystemWatcher> ();
        private Dictionary<string, System.Threading.Timer> _environmentDiagramLoggingTimers = new Dictionary<string, System.Threading.Timer>();

		static Process _process;
        public string ConfigFile { get; private set; }
        public string GoLogsearchShipperFile { get; private set; }

        public class CodeBlockLocker
        {
            public bool isBusy { get; set; }
        }

	    public void Start()
	    {
            SetupConfigFile();
			ExtractGoLogsearchShipper();
			StartProcess();

            var configChanging = new CodeBlockLocker { isBusy = false };

			WhenConfigFileChanges (() => {
                if (configChanging.isBusy)
                {
                    _log.Debug("Already in the process of updating config; ignoring trigger");
                    return;
                }

                lock (configChanging)
                {
                    configChanging.isBusy = true;

                    _log.Debug("Updating config and restarting shipping...");
				    Stop ();
				    SetupConfigFile ();
                    ExtractGoLogsearchShipper();
				    StartProcess ();

                    configChanging.isBusy = false; 
                }

            });

		}

		private void WhenConfigFileChanges(Action actionsToRun) {
			foreach (var watcher in _watchedConfigFiles) {
				watcher.Changed += new FileSystemEventHandler((s,e) => {
                    _log.DebugFormat("Detected change in file: {0}", e.FullPath);
					actionsToRun ();
				});
				watcher.EnableRaisingEvents = true;
			}
		}

		private void AddWatchedConfigFile(string filePath) {
            var fullPath = Path.GetFullPath(filePath);
            if (WatcherAlreadyExists(fullPath)) return;
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
			_watchedConfigFiles.Add (watcher);
		}

        private bool WatcherAlreadyExists(string fullPath)
        {
            foreach (var watcher in _watchedConfigFiles)
            {
                if (Path.Combine(watcher.Path, watcher.Filter) == fullPath)
                    return true;
            }
            return false;
        }

	    private void ExtractGoLogsearchShipper()
	    {
            if (!Environment.OSVersion.VersionString.Contains("Windows"))
                throw new NotSupportedException("LogsearchShipperProcessManager only supports Windows");

			var tempFile = Path.GetTempFileName();
			var exeFile = String.Format(@"{0}\go-logstash-forwarder-{1}.exe",Path.GetDirectoryName(tempFile), Path.GetFileNameWithoutExtension(tempFile));
            File.Move(tempFile, exeFile);

            using (var fStream = new FileStream(exeFile, FileMode.Create))
            {
                fStream.Write(Resources.Resource.go_logstash_forwarder_exe,
                    0, Resources.Resource.go_logstash_forwarder_exe.Length);
            }

            _log.Debug(string.Format("go-logstash-forwarder.exe => {0}", exeFile));

            GoLogsearchShipperFile = exeFile;
	    }

        /// <summary>
        /// We're expecting a config that looks like this:
        ///    
        ///   {
        ///   "network": {
        ///   "servers": [ "endpoint.example.com:5034" ],
        ///   "ssl ca": "C:\\Logs\\mycert.crt",
        ///   "timeout": 23
        ///   }
        ///   ,"files": [
        ///   {
        ///       "paths": [ "myfile.log" ],
        ///       "fields": {
        ///       "@type": "myfile_type",
        ///       "field1": "field1 value"
        ///       "field2": "field2 value"
        ///       }
        ///   },
        ///   {
        ///       "paths": [ "C:\\Logs\\myfile.log" ],
        ///       "fields": {
        ///       "@type": "type\/subtype",
        ///       "key\/subkey": "value\/subvalue"
        ///       }
        ///   }
        ///   ]
        ///}
        /// </summary>
	    internal void SetupConfigFile()
	    {
            var LogsearchShipperConfig = ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
            Debug.Assert(LogsearchShipperConfig != null, "LogsearchShipperConfig != null");
		    
			var watches = new List<FileWatchElement> ();

			ExtractFileWatchers (LogsearchShipperConfig, watches);
			ExtractEDBFileWatchers (LogsearchShipperConfig, watches);

            var config = string.Format ("{{\n{0}\n{1}}}", 
				GenerateNetworkSection (LogsearchShipperConfig),
				GenerateFilesSection (watches));
	       
			ConfigFile = Path.GetTempFileName();
            File.WriteAllText(ConfigFile, config);
	    }

		static void ExtractFileWatchers (LogsearchShipperSection LogsearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogsearchShipperConfig.FileWatchers.Count; i++) {
				watches.Add (LogsearchShipperConfig.FileWatchers [i]);
			}
		}

		void ExtractEDBFileWatchers (LogsearchShipperSection LogsearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogsearchShipperConfig.EDBFileWatchers.Count; i++) {

                var envWatchElement = LogsearchShipperConfig.EDBFileWatchers[i];

                StartLoggingEnvironmentData(envWatchElement);
                AddWatchedConfigFile(envWatchElement.DataFile);

                var parser = new EDBFileWatchParser(envWatchElement);
                watches.AddRange(parser.ToFileWatchCollection());
			}
		}

        /// <summary>
        /// Logs the environment diagram data as extracted from the EDB file that is being used to determine what log files to ship
        /// </summary>
        /// <param name="edbDataFilePath"></param>
        private void StartLoggingEnvironmentData(EnvironmentWatchElement envWatchElement)
        {
            var key = envWatchElement.DataFile;
            if (_environmentDiagramLoggingTimers.ContainsKey(key))
            {
                _environmentDiagramLoggingTimers[key].Dispose();
            }

            var timer = new System.Threading.Timer(
                callback:LogEnvironmentData,
                state: envWatchElement,
                dueTime: 0, //Run once immediately
                period: Convert.ToInt64(TimeSpan.FromMinutes(envWatchElement.LogEnvironmentDiagramDataEveryMinutes).TotalMilliseconds)
             ); 
            
            _environmentDiagramLoggingTimers[key] = timer;
        }

        public static void LogEnvironmentData(object state)
        {
            var parser = new EDBFileWatchParser((EnvironmentWatchElement)state);
            var environments = parser.GenerateLogsearchEnvironmentDiagram();

            _log.Info(string.Format("Logged environment diagram data for {0}", string.Join(",", environments.Select(e => e.Name))));

            log4net.LogManager.GetLogger("EnvironmentDiagramLogger").Info(new { Environments = environments });
        }

		static string GenerateNetworkSection (LogsearchShipperSection LogsearchShipperConfig)
		{
			var networkSection = @"""network"": {
    ""servers"": [ ""{0}"" ],
    ""ssl ca"": ""{1}"",
    ""timeout"": {2}
  },".Replace ("{0}", HttpUtility.JavaScriptStringEncode (LogsearchShipperConfig.Servers)).Replace ("{1}", HttpUtility.JavaScriptStringEncode (LogsearchShipperConfig.SSL_CA)).Replace ("{2}", HttpUtility.JavaScriptStringEncode (LogsearchShipperConfig.Timeout.ToString (CultureInfo.InvariantCulture)));
			return networkSection;
		}

		static string GenerateFilesSection (List<FileWatchElement> watches)
		{
			var filesSection = " \"files\": [\n";
			for (int i = 0; i < watches.Count; i++) {
				FileWatchElement watch = watches [i];
				filesSection += "  {\n";
				filesSection += "    \"paths\": [ \"" + HttpUtility.JavaScriptStringEncode (watch.Files) + "\" ],\n";
				filesSection += "    \"fields\": {\n";
				filesSection += "      \"@type\": \"" + HttpUtility.JavaScriptStringEncode (watch.Type) + "\"\n";
				foreach (FieldElement field in watch.Fields) {
					filesSection += "      ,\"" + HttpUtility.JavaScriptStringEncode (field.Key) + "\": \"" + HttpUtility.JavaScriptStringEncode (field.Value) + "\"\n";
				}
				filesSection += "    }\n";
				filesSection += "  }";
				if (i < watches.Count - 1) {
					filesSection += ",";
				}
				filesSection += "\n";
			}
			filesSection += "]\n";
			return filesSection;
		}

	    public void Stop()
	    {
	        const int waitForGoLogstashForwarderToExitSeconds = 60;

            _log.Info("Stopping and cleaning up go-logstash-forwarder process.");

	        if (_process == null)
	        {
                _log.Info("go-logstash-forwarder process doesn't exist - nothing to Stop.");
                return;
	        }

	        _log.Info("sending Ctrl-C to go-logstash-forwarder process so it can clean up");
            _process.StandardInput.WriteLine(char.ConvertFromUtf32(3));

            _log.InfoFormat("Waiting for {0}sec for go-logstash-forwarder process to shut down gracefully", waitForGoLogstashForwarderToExitSeconds);
            _process.WaitForExit(waitForGoLogstashForwarderToExitSeconds * 1000);
            if (!_process.HasExited)
            {
                _log.WarnFormat("Killing go-logstash-forwarder process since it didn't exit within {0}sec",waitForGoLogstashForwarderToExitSeconds);
                _process.Kill();
            }

            //TODO:  Figure out why logstash-forwarder isn't doing this cleanup itself.  It must be something to do with the way we're terminating the process above
            if (File.Exists(".logstash-forwarder.new") && File.GetLastWriteTime(".logstash-forwarder.new") > File.GetLastWriteTime(".logstash-forwarder"))
            {
                _log.Warn("go-logstash-forwarder didn't cleaup after itself correctly.");
                //File.Copy(".logstash-forwarder", ".logstash-forwarder.old", true);
                //File.Copy(".logstash-forwarder.new", ".logstash-forwarder", true);
            }
           
            //Cleanup
	        try
	        {
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                File.Delete(GoLogsearchShipperFile);
                _log.InfoFormat("Deleting {0}", GoLogsearchShipperFile);
	        }
	        catch (Exception)
	        {
                //Wait a bit more, then try again
	            try
	            {
                    _log.InfoFormat("Failed to delete {0}.  Waiting 1 sec, then trying again", GoLogsearchShipperFile);
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                    File.Delete(GoLogsearchShipperFile);
	            }
	            catch (Exception e)
	            {
                    _log.Warn(string.Format("Unable to delete {0}.  Giving up.", GoLogsearchShipperFile), e);
	            }
                
	        }

            _log.Info("Successfully stopped and cleaned up go-logstash-forwarder process");
		}

	    private void StartProcess()
		{
			var startInfo = new ProcessStartInfo(GoLogsearchShipperFile)
			{
                Arguments = "-from-beginning=false -config " + ConfigFile,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
            _log.DebugFormat("Running {0} {1}", GoLogsearchShipperFile, startInfo.Arguments);
			_process = Process.Start(startInfo);

	        _process.OutputDataReceived += LogGoLogstashForwarderOutput;
			_process.BeginOutputReadLine();

            _process.ErrorDataReceived += LogGoLogstashForwarderOutput;
			_process.BeginErrorReadLine();
		}

	    private void LogGoLogstashForwarderOutput(object s, DataReceivedEventArgs e)
	    {
	        if (e.Data == null) return;

            if (e.Data.Contains("Registrar received"))
	        {
	            _logLogstashForwarder.Debug(e.Data);
	        }
	        else
	        {
	            _logLogstashForwarder.Info(e.Data);
	        }
	    }
	}
}

