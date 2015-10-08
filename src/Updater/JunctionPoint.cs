﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSearchShipper.Updater
{
	public static class JunctionPoint
	{
		public static bool Exists(string path)
		{
			var dirInfo = new DirectoryInfo(path);
			var res = dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
			return res;
		}

		public static void Create(string path, string target)
		{
			var processArgs = new ProcessStartInfo
			{
				FileName = Environment.SystemDirectory + @"\cmd.exe",
				Arguments = string.Format("/C mklink /j \"{0}\" \"{1}\"", path, target),
				UseShellExecute = false,
				RedirectStandardError = true,
			};
			var process = Process.Start(processArgs);
			if (!process.WaitForExit(10 * 1000))
				throw new ApplicationException("Timeout when running mklink");

			if (process.ExitCode != 0)
				throw new ApplicationException(string.Format("mklink: exit code {0}", process.ExitCode));

			var errors = process.StandardError.ReadToEnd();
			if (!string.IsNullOrEmpty(errors))
				throw new ApplicationException(errors);
		}
	}
}