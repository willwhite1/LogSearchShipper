using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Core;
using log4net.Filter;

namespace LogSearchShipper.Log4net
{
	public sealed class MainLogAppender : DefaultFileAppender
	{
		public MainLogAppender()
		{
			File = "LogSearchShipper.log";

			AddFilter(new LevelRangeFilter { LevelMin = Level.Info, LevelMax = Level.Fatal, });
			AddFilter(new LoggerMatchFilter { LoggerToMatch = "EnvironmentDiagramLogger", AcceptOnMatch = false, });
		}
	}
}
