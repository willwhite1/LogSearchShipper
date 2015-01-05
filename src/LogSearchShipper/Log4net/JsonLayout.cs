using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Layout;
using log4net.Util;
using LogSearchShipper.Core;

namespace LogSearchShipper.Log4net
{
	public sealed class JsonLayout : PatternLayout
	{
		public JsonLayout()
		{
			ConversionPattern = "{\"@timestamp\":\"%iso8601_date\",%event_as_json,\"logger\":\"%logger\",\"level\":\"%level\"}%n";
			IgnoresException = false;

			AddConverter(new ConverterInfo { Name = "event_as_json", Type = typeof(JSONFragmentPatternConverter) });
			AddConverter(new ConverterInfo { Name = "iso8601_date", Type = typeof(ISO8601DatePatternConverter) });
		}
	}
}
