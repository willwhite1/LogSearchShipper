using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Core
{
	public static class ProcessUtils
	{
		public static Process StartProcess(string processPath, string processArgs, Action<string> logOutput = null)
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

			if (logOutput == null)
				logOutput = DebugTrace;
			process.OutputDataReceived +=
				(sender, args) =>
				{
					var message = string.Format("{0}: {1}", processPath, args.Data);
					logOutput(message);
				};
			process.ErrorDataReceived +=
				(sender, args) =>
				{
					var message = string.Format("{0}: {1}", processPath, args.Data);
					logOutput(message);
				};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			return process;
		}

		static void DebugTrace(string message)
		{
			Trace.WriteLine(message);
		}

		public static string Execute(string processPath, string processArgs)
		{
			var buf = new StringBuilder();
			var process = StartProcess(processPath, processArgs,
				message => buf.AppendLine(message));
			if (!process.WaitForExit(10 * 1000) || process.ExitCode != 0)
				throw new ApplicationException(buf.ToString());
			return buf.ToString();
		}
	}
}
