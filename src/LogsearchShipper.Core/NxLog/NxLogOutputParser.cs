using System;
using System.Text.RegularExpressions;
using log4net;
using log4net.Core;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogOutputParser
	{
		public NxLogEvent Parse(string logString)
		{
			var logOutput = new NxLogEvent
			{
				Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
				LogLevel = "INFO",
				Message = logString
			};

			var myRegex = new Regex(@"^(?<timestamp>\d\d\d\d-\d\d-\d\d \d\d:\d\d:\d\d) (?<level>\w+) (?<message>.*$)",
				RegexOptions.None);
			Match match = myRegex.Match(logString);

			if (!match.Success) return logOutput;

			logOutput.Timestamp = match.Groups["timestamp"].Value;
			logOutput.LogLevel = match.Groups["level"].Value;
			logOutput.Message = match.Groups["message"].Value;

			return logOutput;
		}

		public LoggingEvent ConvertToLog4Net(ILog logger, NxLogEvent nxLogEvent)
		{
			Level level = logger.Logger.Repository.LevelMap[nxLogEvent.LogLevel] ?? Level.Warn;
				//Treat unrecognised levels (like WARNING) as WARN
			return new LoggingEvent(typeof (NxLogEvent), logger.Logger.Repository, "nxlog.exe",
				level, nxLogEvent.Message, null);
		}

		public class NxLogEvent
		{
			public string LogLevel { get; set; }
			public string Timestamp { get; set; }
			public string Message { get; set; }
		}
	}
}