using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Util;

using LogSearchShipper.Core;

namespace LogSearchShipper.Appenders
{
	// Write a modest amount of logging (>= INFO ) to LogSearchShipper.log; which should be shipped to LogSearch
	public sealed class MainLogAppender : RollingFileAppender
	{
		public MainLogAppender()
		{
			var layout = new PatternLayout
			{
				ConversionPattern = "{\"@timestamp\":\"%iso8601_date\",%event_as_json,\"logger\":\"%logger\",\"level\":\"%level\"}%n",
				IgnoresException = false,
			};
			layout.AddConverter(new ConverterInfo { Name = "event_as_json", Type = typeof(JSONFragmentPatternConverter) });
			layout.AddConverter(new ConverterInfo { Name = "iso8601_date", Type = typeof(ISO8601DatePatternConverter) });
			layout.ActivateOptions();

			Name = GetType().Name;
			File = "LogSearchShipper.log";
			RollingStyle = RollingMode.Size;
			AppendToFile = true;
			MaximumFileSize = "250MB";
			MaxSizeRollBackups = 2;
			Layout = layout;

			AddFilter(new LevelRangeFilter { LevelMin = Level.Info, LevelMax = Level.Fatal, });
			AddFilter(new LoggerMatchFilter { LoggerToMatch = "EnvironmentDiagramLogger", AcceptOnMatch = false, });
		}
	}
}
