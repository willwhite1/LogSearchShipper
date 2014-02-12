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

		static Process _process;
	    private string _logType;
        public string ConfigFile { get; private set; }
        public string GoLogsearchShipperFile { get; private set; }

	    public void Start()
	    {
	        ExtractGoLogsearchShipper();
            SetupConfigFile();
            StartProcess();
		}

	    private void ExtractGoLogsearchShipper()
	    {
            if (!Environment.OSVersion.VersionString.Contains("Windows"))
                throw new NotSupportedException("LogsearchShipperProcessManager only supports Windows");

	        var tempFile = Path.GetTempFileName();
	        var exeFile = tempFile + ".exe";
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

		static void ExtractEDBFileWatchers (LogsearchShipperSection LogsearchShipperConfig, List<FileWatchElement> watches)
		{
			for (int i = 0; i < LogsearchShipperConfig.EDBFileWatchers.Count; i++) {
				watches.AddRange( new EDBFileWatchParser(LogsearchShipperConfig.EDBFileWatchers[i]).ToFileWatchCollection() );
			}
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
			if (_process == null)
				return;
			_process.StandardInput.Close (); // send the close process signal
			_process.WaitForExit (5 * 1000);
            if (!_process.HasExited)
            {
                _process.Kill();
            }

            //Cleanup
	        try
	        {
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                File.Delete(GoLogsearchShipperFile);
	        }
	        catch (Exception)
	        {
                //Wait a bit more, then try again
	            try
	            {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                    File.Delete(GoLogsearchShipperFile);
	            }
	            catch (Exception e)
	            {
                    _log.Warn(string.Format("Unable to delete {0}.  Giving up.", GoLogsearchShipperFile), e);
	            }
                
	        }
            
		}

	    private void StartProcess()
		{
			var startInfo = new ProcessStartInfo(GoLogsearchShipperFile)
			{
                Arguments = "-config " + ConfigFile,
				WorkingDirectory = Path.GetTempPath(),
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
            _log.DebugFormat("go-logstash-forwarder.exe: running {0} -config {1}", GoLogsearchShipperFile, ConfigFile);
			_process = Process.Start(startInfo);

			_process.OutputDataReceived += (s, e) => _log.Info("go-logstash-forwarder.exe: " + e.Data);
			_process.BeginOutputReadLine();

            _process.ErrorDataReceived += (s, e) => _log.Info("go-logstash-forwarder.exe: " + e.Data);
			_process.BeginErrorReadLine();
		}
	}
}

