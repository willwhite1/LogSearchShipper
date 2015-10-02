using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LogSearchShipper.Updater
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				if (args.Length != 5)
					throw new ApplicationException("Invalid args: " + Environment.CommandLine);

				LogInfo("Updating: " + Environment.CommandLine);

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

				LogInfo("Finished successfully");

				LogFile.Dispose();
			}
			catch (ApplicationException exc)
			{
				LogError(exc.Message);
			}
			catch (Exception exc)
			{
				LogError(exc.ToString());
			}
		}

		static void Log(string message, string level)
		{
			Trace.WriteLine(message);
			Console.WriteLine(message);

			try
			{
				if (LogFile == null)
				{
					var path = Path.Combine(Directory.GetCurrentDirectory(), "Updater.log");
					LogFile = new StreamWriter(path, true);
				}

				var line = string.Format("{{\"timestamp\":\"{0}\", \"level\":\"{1}\", \"Message\":\"{2}\"}}",
					DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"), EscapeJsonVal(level), EscapeJsonVal(message));
				LogFile.WriteLine(line);
				LogFile.Flush();
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc.ToString());
				Console.WriteLine(exc.ToString());
			}
		}

		static void LogError(string message)
		{
			Log(message, "ERROR");
		}

		static void LogInfo(string message)
		{
			Log(message, "INFO");
		}

		static string EscapeJsonVal(string val)
		{
			return HttpUtility.JavaScriptStringEncode(val);
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

		private static TextWriter LogFile;
	}
}
