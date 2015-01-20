using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Filter;

namespace LogSearchShipper.Log4net
{
	public sealed class EnvironmentDiagramAppender : DefaultFileAppender
	{
		public EnvironmentDiagramAppender()
		{
			File = "EnvironmentDiagramData.log";
			MaximumFileSize = "50MB";

			AddFilter(new LoggerMatchFilter { LoggerToMatch = "EnvironmentDiagramLogger" });
			AddFilter(new DenyAllFilter());
		}
	}
}
