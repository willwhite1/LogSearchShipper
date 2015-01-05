using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace LogSearchShipper.Log4net
{
	// DEBUG is too verbose to see what is going on in the console
	public sealed class DefaultConsoleAppender : ConsoleAppender
	{
		public override void ActivateOptions()
		{
			if (Layout == null)
			{
				var layout = new PatternLayout("%utcdate{ISO8601} [%thread] %-5level %logger - %.255message%newline");
				layout.ActivateOptions();
				Layout = layout;
			}

			if (FilterHead == null)
			{
				AddFilter(new LevelRangeFilter { LevelMin = Level.Info, LevelMax = Level.Fatal, });
			}

			base.ActivateOptions();
		}
	}
}
