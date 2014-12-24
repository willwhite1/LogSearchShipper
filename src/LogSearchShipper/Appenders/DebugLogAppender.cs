using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Layout;

namespace LogSearchShipper.Appenders
{
	// Write an excessive amount of info to LogSearchShipperDebug.log; NOT recommended to ship this to LogSearch
	public class DebugLogAppender : RollingFileAppender
	{
		public DebugLogAppender()
		{
			var layout = new PatternLayout("%utcdate{ISO8601} [%thread] %-5level %logger - %.255message%newline");
			layout.ActivateOptions();

			Name = "RollingFileAppenderDebug";
			File = "LogSearchShipperDebug.log";
			RollingStyle = RollingMode.Size;
			AppendToFile = true;
			MaximumFileSize = "250MB";
			MaxSizeRollBackups = 2;
			Layout = layout;
		}
	}
}
