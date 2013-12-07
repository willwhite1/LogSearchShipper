using System;
using log4net.Appender;
using log4net.Core;
using System.Linq;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using System.Net;
using LogstashForwarder.Core;

namespace LogstashForwarderAppender
{
	//TODO: This probably doesn't work; it just illustrates how the LogstashForwarder could be used in a Log4Net Appender
	public class LogstashForwarderAppender : FileAppender
	{
		private LogstashForwarderProcessManager _logstashForwarderProcess;

		protected override void PrepareWriter ()
		{
			_logstashForwarderProcess.Start ();
			base.PrepareWriter ();
		}
		protected override void OnClose ()
		{
			_logstashForwarderProcess.Stop ();

			base.OnClose ();
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			var val = Convert(loggingEvent);
			base.Append(val);
		}

		protected override void Append(LoggingEvent[] loggingEvents)
		{
			var vals = loggingEvents.Select(Convert).ToArray();
			base.Append(vals);
		}

		private static LoggingEvent Convert(LoggingEvent loggingEvent)
		{
			var eventData = loggingEvent.GetLoggingEventData();
			eventData.ExceptionString = Convert(eventData.ExceptionString);
			eventData.Message = Convert(eventData.Message);
			var val = new LoggingEvent(eventData);
			return val;
		}

		static string Convert(object val)
		{
			var res = val.ToString();
			res = res.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
			return res;
		}
	}
}
