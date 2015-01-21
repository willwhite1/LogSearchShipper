using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Log4net
{
	public sealed class EdbExpectedEventSourcesAppender : DefaultFileAppender
	{
		public EdbExpectedEventSourcesAppender()
		{
			File = "EDB_expected_event_sources.log";
			MaximumFileSize = "50MB";
		}
	}
}
