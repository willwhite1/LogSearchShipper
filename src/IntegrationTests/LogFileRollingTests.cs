using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	public class LogFileRollingTests
	{
		private Process StartProcess(string processPath, string processArgs)
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
					RedirectStandardInput = true
				}
			};
			process.OutputDataReceived += (sender, args) => Console.WriteLine("{0}: {1}", processPath, args.Data);
			process.Start();
			process.BeginOutputReadLine();

			return process;
		}

		private void ShutdownProcess(Process process)
		{
			if (process == null) return;

			process.StandardInput.Close(); // send Ctrl-C to logstash-forwarder so it can clean up
			process.CancelOutputRead();
			process.WaitForExit(5*1000);
			if (!process.HasExited)
			{
				KillProcessAndChildren(process.Id);
			}
		}

		private static void KillProcessAndChildren(int pid)
		{
			var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
			ManagementObjectCollection moc = searcher.Get();
			foreach (ManagementBaseObject cur in moc)
			{
				var mo = (ManagementObject) cur;
				KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
			}

			try
			{
				Process proc = Process.GetProcessById(pid);
				proc.Kill();
			}
			catch (ArgumentException)
			{
				// Process has already exited
			}
		}

		private static void DeleteOldLogFiles()
		{
			foreach (FileInfo f in new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*"))
			{
				f.Delete();
			}
		}

		[Test]
		public void ShippingShouldNotBlockLogFileRolling()
		{
			Process shipper = null;
			Process processWithLogFileRolling = null;

			DeleteOldLogFiles();

			File.Delete("LogsearchShipper.Service.exe.config.bak");
			File.Move("LogsearchShipper.Service.exe.config", "LogsearchShipper.Service.exe.config.bak");
			File.Move("LogsearchShipper.Service.exe.config.ShipDummyService", "LogsearchShipper.Service.exe.config");
			try
			{
				shipper = StartProcess(Environment.CurrentDirectory + @"\LogsearchShipper.Service.exe",
					"-instance:integrationtest001");
				processWithLogFileRolling = StartProcess(Environment.CurrentDirectory + @"\DummyServiceWithLogRolling.exe", "");

				Thread.Sleep(TimeSpan.FromSeconds(10));

				//There should be 6 DummyServiceWithLogRolling.log.* files, unless the shipper has blocked file rolling
				FileInfo[] logFiles =
					new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*");
				Assert.AreEqual(6, logFiles.Count());
			}
			finally
			{
				ShutdownProcess(shipper);
				ShutdownProcess(processWithLogFileRolling);

				File.Delete("LogsearchShipper.Service.exe.config");
				File.Move("LogsearchShipper.Service.exe.config.bak", "LogsearchShipper.Service.exe.config");
			}
		}
	}
}