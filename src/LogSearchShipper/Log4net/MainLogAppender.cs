using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Core;
using log4net.Filter;

namespace LogSearchShipper.Log4net
{
	// Write a modest amount of logging (>= INFO ) to LogSearchShipper.log; which should be shipped to LogSearch
	public sealed class MainLogAppender : RollingFileAppender
	{
		public MainLogAppender()
		{
			var layout = new JsonLayout();
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
