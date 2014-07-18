using System.IO;
using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;
using Newtonsoft.Json;

namespace LogSearchShipper.Core
{
	public class ISO8601DatePatternConverter : PatternLayoutConverter
	{
		public const string ISO8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

		protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			writer.Write(loggingEvent.TimeStamp.ToUniversalTime().ToString(ISO8601));
		}
	}

	public class JSONFragmentPatternConverter : PatternLayoutConverter
	{
		protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			string json = string.Empty;
			if (loggingEvent.ExceptionObject != null)
			{
				json = JsonConvert.SerializeObject(new
				{
					Message = loggingEvent.RenderedMessage,
					Exception = loggingEvent.ExceptionObject
				});
			}
			else if (loggingEvent.MessageObject.GetType() != typeof (string)
			         && loggingEvent.MessageObject.GetType() != typeof (SystemStringFormat)
				)
			{
				json = JsonConvert.SerializeObject(loggingEvent.MessageObject, Formatting.None);
			}
			else
			{
				json = JsonConvert.SerializeObject(new
				{
					Message = loggingEvent.RenderedMessage
				}, Formatting.None);
			}

			writer.Write(json.Trim(new[] {'{', '}'}));
		}
	}
}