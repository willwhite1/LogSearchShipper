using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace LogSearchShipper.Updater
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Updater.log");

				if (args.Length != 5)
					throw new ApplicationException("Invalid args: " + Environment.CommandLine);

				Log("Updating: " + Environment.CommandLine);

				var parentProcessId = int.Parse(args[0]);

				var appMode = (AppMode)Enum.Parse(typeof(AppMode), args[1], true);

				var startingName = args[2];
				var sourcePath = args[3];
				var targetPath = args[4];

				try
				{
					var parentProcess = Process.GetProcessById(parentProcessId);
					if (!parentProcess.WaitForExit(30 * 1000))
						throw new ApplicationException("Parent process didn't stop");
				}
				catch (ArgumentException)
				{
					// process already has stopped
				}

				FileUtil.Cleanup(targetPath, UpdateFileTypes, false, false);
				DoUpdate(sourcePath, targetPath);
				Start(appMode, startingName, targetPath);

				Log("Finished successfully");
			}
			catch (ApplicationException exc)
			{
				Log(exc.Message);
			}
			catch (Exception exc)
			{
				Log(exc.ToString());
			}
		}

		static void Log(string message)
		{
			Trace.WriteLine(message);
			Console.WriteLine(message);

			try
			{
				var line = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") + "\t" + message + Environment.NewLine;
				File.AppendAllText(LogFilePath, line);
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc.ToString());
				Console.WriteLine(exc.ToString());
			}
		}

		static void DoUpdate(string sourcePath, string targetPath)
		{
			foreach (var wildcard in UpdateFileTypes)
			{
				foreach (var file in Directory.GetFiles(sourcePath, wildcard, SearchOption.AllDirectories))
				{
					var targetFilePath = Path.Combine(targetPath, Path.GetFileName(file));
					File.Copy(file, targetFilePath, true);
				}
			}
		}

		static void Start(AppMode appMode, string startingName, string targetPath)
		{
			switch (appMode)
			{
				case AppMode.Console:
					var processFilePath = Path.Combine(targetPath, startingName);
					Process.Start(processFilePath, "");
					break;
				case AppMode.Service:
					var service = new ServiceController(startingName);
					service.Start();
					break;
				default:
					throw new ArgumentOutOfRangeException("appMode", appMode, null);
			}
		}

		private static readonly string[] UpdateFileTypes = { "*.exe", "*.dll", "*.pdb", "*.xml" };

		private static string LogFilePath;
	}
}
