using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web;
using LogsearchShipper.Core.ConfigurationSections;
using System.Linq;
using System.Collections.Generic;

namespace LogsearchShipper.Core
{
	public class EDBFileWatchParser
	{
		EnvironmentWatchElement _environmentWatchElement;

		public EDBFileWatchParser (EnvironmentWatchElement environmentWatchElement)
		{
			_environmentWatchElement = environmentWatchElement;
		}

		public IEnumerable<FileWatchElement> ToFileWatchCollection ()
		{
			var watches = new List<FileWatchElement> ();
			watches.Add (new FileWatchElement { Files = @"\\PKH-PPE-APP10\logs\Apps\PriceHistoryService\log.log", Type = "log4net", Fields = _environmentWatchElement.Fields });
			watches.Add (new FileWatchElement { Files = @"c:\foo\bar2.log", Type = "log4net", Fields = _environmentWatchElement.Fields });
			return watches;
		}
	}
}

