using System;
using System.Collections.Generic;
using System.Linq;
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

			if (MissingFileMessages.Any(logOutput.Message.StartsWith))
			{
				logOutput.LogLevel = "WARN";
				logOutput.Category = "MISSING_FILE";
			}

			return logOutput;
		}

		private static readonly string[] MissingFileMessages =
			{
				"failed to open",
				"input file does not exist:",
				"apr_stat failed on file"
			};

		public LoggingEvent ConvertToLog4Net(ILog logger, NxLogEvent nxLogEvent)
		{
			Level level = logger.Logger.Repository.LevelMap[nxLogEvent.LogLevel] ?? Level.Warn;
			//Treat unrecognised levels (like WARNING) as WARN

			var message = new Dictionary<string, object> { { "Message", nxLogEvent.Message } };
			if (nxLogEvent.Category != null)
				message.Add("Category", nxLogEvent.Category);

			return new LoggingEvent(typeof(NxLogEvent), logger.Logger.Repository, "nxlog.exe",
				level, message, null);
		}

		public class NxLogEvent
		{
			public string LogLevel { get; set; }
			public string Timestamp { get; set; }
			public string Message { get; set; }
			public string Category { get; set; }
		}
	}
}