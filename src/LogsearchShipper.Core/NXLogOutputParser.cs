using System;
using System.Text.RegularExpressions;
using log4net;
using log4net.Core;
using log4net.Repository;

namespace LogsearchShipper.Core
{

	public class NXLogOutputParser
	{
			public class NXLogEvent
			{
					public string LogLevel { get; set; }
					public string Timestamp { get; set; }
					public string Message { get; set; }
			}
		public NXLogEvent Parse(string logString)
		{
			var logOutput = new NXLogEvent()
			{
				Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
				LogLevel = "INFO",
				Message = String.Format(logString)
			};

			var myRegex = new Regex(@"^(?<timestamp>\d\d\d\d-\d\d-\d\d \d\d:\d\d:\d\d) (?<level>\w+) (?<message>.*$)", RegexOptions.None);
			var match = myRegex.Match(logString);

			if (!match.Success) return logOutput;

			logOutput.Timestamp = match.Groups["timestamp"].Value;
			logOutput.LogLevel = match.Groups["level"].Value;
			logOutput.Message = match.Groups["message"].Value;

			return logOutput;
		}

		public LoggingEvent ConvertToLog4Net(ILog logger, NXLogEvent nxLogEvent)
		{
			var level = logger.Logger.Repository.LevelMap[nxLogEvent.LogLevel] ?? Level.Warn; //Treat unrecognised levels (like WARNING) as WARN
			return new LoggingEvent(typeof(NXLogEvent), logger.Logger.Repository, "nxlog.exe", 
					level, nxLogEvent.Message, null);
		}
	}


}