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
				if (args.Length != 1)
					throw new ApplicationException("Invalid args");

				var parentProcessId = int.Parse(args[0]);
				var parentProcess = Process.GetProcessById(parentProcessId);
				
				if (!parentProcess.WaitForExit(10 * 1000))
					throw new ApplicationException("Parent process didn't stop");
			}
			catch (ApplicationException exc)
			{
				Trace.WriteLine(exc.Message);
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc.ToString());
			}
		}
	}
}
