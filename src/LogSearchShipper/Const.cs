using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSearchShipper
{
	static class Const
	{
		public const string AppName = "LogSearchShipper";

		public static string AppPath
		{
			get
			{
				var res = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				return res;
			}
		}

		public static string UpdateAreaPath
		{
			get { return Path.Combine(AppPath, "Update"); }
		}
	}
}
