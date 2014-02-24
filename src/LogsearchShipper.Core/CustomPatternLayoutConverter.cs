using System;
using System.IO;
using log4net.Layout.Pattern;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace LogsearchShipper.Core
{
	public class ISO8601DatePatternConverter : PatternLayoutConverter
	{
		public const string ISO8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

		protected override void Convert(TextWriter writer, log4net.Core.LoggingEvent loggingEvent)
		{
			writer.Write(loggingEvent.TimeStamp.ToUniversalTime().ToString(ISO8601));
		}
	}

	public class JSONFragmentPatternConverter : PatternLayoutConverter
	{
		protected override void Convert(TextWriter writer, log4net.Core.LoggingEvent loggingEvent)
		{
			var json = string.Empty;
		    if (loggingEvent.ExceptionObject != null)
		    {
                json = JsonConvert.SerializeObject(new
                {
                    Message = loggingEvent.RenderedMessage,
                    Exception = loggingEvent.ExceptionObject
                });
            }
            else if (loggingEvent.MessageObject != null)
            {
                json = JsonConvert.SerializeObject(new
                {
                    Message = loggingEvent.RenderedMessage != null ? loggingEvent.MessageObject : loggingEvent.RenderedMessage
                }, Formatting.None);
            }

		    writer.Write(json.Trim(new [] { '{','}'}));
		}
	}

}