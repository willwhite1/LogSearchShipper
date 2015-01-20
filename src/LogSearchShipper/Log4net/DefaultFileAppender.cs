using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Appender;

namespace LogSearchShipper.Log4net
{
	// Write a modest amount of logging (>= INFO ) to LogSearchShipper.log; which should be shipped to LogSearch
	public class DefaultFileAppender : RollingFileAppender
	{
		public DefaultFileAppender()
		{
			RollingStyle = RollingMode.Size;
			AppendToFile = true;
			MaximumFileSize = "50MB";
			MaxSizeRollBackups = 2;
		}

		public override void ActivateOptions()
		{
			if (Layout == null)
			{
				var layout = new JsonLayout();
				layout.ActivateOptions();
				Layout = layout;
			}

			base.ActivateOptions();
		}
	}
}
