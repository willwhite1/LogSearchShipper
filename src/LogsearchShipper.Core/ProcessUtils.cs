using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Core
{
	public static class ProcessUtils
	{
		public static Process StartProcess(string processPath, string processArgs)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = processPath,
					Arguments = processArgs,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
				}
			};
			process.OutputDataReceived += (sender, args) =>
				Trace.WriteLine(string.Format("{0}: {1}", processPath, args.Data));
			process.ErrorDataReceived += (sender, args) =>
				Trace.WriteLine(string.Format("{0}: {1}", processPath, args.Data));

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			return process;
		}
	}
}
