﻿using System;
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
				if (args.Length != 4)
					throw new ApplicationException("Invalid args");

				var parentProcessId = int.Parse(args[0]);

				var appMode = (AppMode)Enum.Parse(typeof(AppMode), args[1], true);

				var targetPath = args[2];
				var serviceName = args[3];

				try
				{
					var parentProcess = Process.GetProcessById(parentProcessId);
					if (!parentProcess.WaitForExit(10 * 1000))
						throw new ApplicationException("Parent process didn't stop");
				}
				catch (ArgumentException)
				{
					// process already has stopped
				}

				FileUtil.DeleteAllFiles(targetPath, UpdateFileTypes);
				DoUpdate(targetPath);
				Start(appMode, serviceName, targetPath);
			}
			catch (ApplicationException exc)
			{
				Report(exc.Message);
			}
			catch (Exception exc)
			{
				Report(exc.ToString());
			}
		}

		static void Report(string message)
		{
			Trace.WriteLine(message);
			Console.WriteLine(message);
		}

		static void DoUpdate(string targetPath)
		{
			var sourcePath = Const.AppPath;

			foreach (var wildcard in UpdateFileTypes)
			{
				foreach (var file in Directory.GetFiles(sourcePath, wildcard))
				{
					var targetFilePath = Path.Combine(targetPath, Path.GetFileName(file));
					File.Copy(file, targetFilePath, true);
				}
			}
		}

		static void Start(AppMode appMode, string serviceName, string targetPath)
		{
			switch (appMode)
			{
				case AppMode.Console:
					var processFilePath = Path.Combine(targetPath, "LogSearchShipper.exe");
					Process.Start(processFilePath, "");
					break;
				case AppMode.Service:
					var service = new ServiceController(serviceName);
					service.Start();
					break;
				default:
					throw new ArgumentOutOfRangeException("appMode", appMode, null);
			}
		}

		private static readonly string[] UpdateFileTypes = { "*.exe", "*.dll", "*.pdb" };
	}
}
