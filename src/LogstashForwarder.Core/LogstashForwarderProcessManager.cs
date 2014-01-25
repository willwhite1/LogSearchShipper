using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web;
using LogstashForwarder.Core.ConfigurationSections;

namespace LogstashForwarder.Core
{
	public class LogstashForwarderProcessManager
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LogstashForwarderProcessManager));

		static Process _process;
	    private string _logType;
        public string ConfigFile { get; private set; }
        public string GoLogstashForwarderFile { get; private set; }

	    public void Start()
	    {
	        ExtractGoLogstashForwarder();
            SetupConfigFile();
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
            var logstashForwarderConfig = ConfigurationManager.GetSection("logstashForwarderGroup/logstashForwarder") as LogstashForwarderSection;
            Debug.Assert(logstashForwarderConfig != null, "logstashForwarderConfig != null");

	        ConfigFile = Path.GetTempFileName();
	        var networkSection =
	            @"""network"": {
    ""servers"": [ ""{0}"" ],
    ""ssl ca"": ""{1}"",
    ""timeout"": {2}
  },"
	            .Replace("{0}", HttpUtility.JavaScriptStringEncode(logstashForwarderConfig.Servers))
	            .Replace("{1}", HttpUtility.JavaScriptStringEncode(logstashForwarderConfig.SSL_CA))
	            .Replace("{2}", HttpUtility.JavaScriptStringEncode(logstashForwarderConfig.Timeout.ToString(CultureInfo.InvariantCulture)));

            var filesSection = " \"files\": [\n";
            for (int i = 0; i < logstashForwarderConfig.Watchs.Count; i++)
            {
                WatchElement watch = logstashForwarderConfig.Watchs[i];
                filesSection += "  {\n";
                filesSection += "    \"paths\": [ \""+HttpUtility.JavaScriptStringEncode(watch.Files)+"\" ],\n";
                filesSection += "    \"fields\": {\n";
                filesSection += "      \"@type\": \""+HttpUtility.JavaScriptStringEncode(watch.Type)+"\"\n";
                foreach (FieldElement field in watch.Fields)
                {
                    filesSection += "      ,\"" + HttpUtility.JavaScriptStringEncode(field.Key) + "\": \"" + HttpUtility.JavaScriptStringEncode(field.Value) + "\"\n";
                }
                filesSection += "    }\n";
                filesSection += "  }";
                if (i < logstashForwarderConfig.Watchs.Count - 1) { filesSection += ","; }
                filesSection += "\n";
            }
            filesSection += "]\n";
            var config = "{\n" + networkSection + "\n" + filesSection + "}";
	       
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
	        try
	        {
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                File.Delete(GoLogstashForwarderFile);
	        }
	        catch (Exception)
	        {
                //Wait a bit more, then try again
	            try
	            {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                    File.Delete(GoLogstashForwarderFile);
	            }
	            catch (Exception e)
	            {
                    _log.Warn(string.Format("Unable to delete {0}.  Giving up.", GoLogstashForwarderFile), e);
	            }
                
	        }
            
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

