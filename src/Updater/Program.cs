using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LogSearchShipper.Updater
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
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
				FileUtil.DeleteAllFiles(targetPath, new[] { "*.exe", "*.dll", "*.pdb" });
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
	}
}
