using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace IntegrationTests
{
	static class Utils
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
						RedirectStandardInput = true,
					}
				};
			process.OutputDataReceived += (sender, args) => Console.WriteLine("{0}: {1}", processPath, args.Data);
			process.Start();
			process.BeginOutputReadLine();

			return process;
		}

		public static void ShutdownProcess(Process process)
		{
			if (process == null)
				return;

			process.StandardInput.Close(); // send Ctrl-C to logstash-forwarder so it can clean up
			process.CancelOutputRead();
			process.WaitForExit(5 * 1000);
			if (!process.HasExited)
			{
				KillProcessAndChildren(process.Id);
			}
		}

		private static void KillProcessAndChildren(int pid)
		{
			var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
			var moc = searcher.Get();
			foreach (var cur in moc)
			{
				var mo = (ManagementObject)cur;
				KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
			}

			try
			{
				var proc = Process.GetProcessById(pid);
				proc.Kill();
			}
			catch (ArgumentException)
			{
				// Process has already exited
			}
		}
	}
}
