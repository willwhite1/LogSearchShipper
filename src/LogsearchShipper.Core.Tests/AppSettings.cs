using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace LogsearchShipper.Core.Tests
{
	static class AppSettings
	{
		public static string EsServerUrl { get { return Read("EsServerUrl"); } }

		static string Read(string name)
		{
			var res = ConfigurationManager.AppSettings[name];
			if (res == null)
				return "";
			return res;
		}
	}
}
