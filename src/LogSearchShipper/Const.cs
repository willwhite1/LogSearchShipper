using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSearchShipper
{
	static class Const
	{
		public const string AppName = "LogSearchShipper";

		public static string WorkingAreaPath
		{
			get
			{
				var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				var res = Path.Combine(basePath, "City Index", AppName);
				return res;
			}
		}

		public static string UpdateAreaPath
		{
			get { return Path.Combine(WorkingAreaPath, "Update"); }
		}
	}
}
