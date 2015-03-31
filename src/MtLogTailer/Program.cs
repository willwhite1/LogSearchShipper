using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MtLogTailer
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				if (args.Length != 1)
					throw new ApplicationException("Invalid args.  Should use MtLogTailer.exe <filePath> <encoding: optional>");
				var path = args[0];

				var encoding = 936;  //Default to gb2312-> ANSI/OEM Simplified Chinese (PRC, Singapore); Chinese Simplified (GB2312)
				if (args.Length >= 2)
					encoding = Convert.ToInt32(args[1]);

				var watcher = new PathWatcher(path, encoding);

				Console.CancelKeyPress +=
					(sender, eventArgs) =>
					{
						Terminate = true;
						watcher.Stop();
						eventArgs.Cancel = true;
					};

				watcher.Process();
			}
			catch (ThreadInterruptedException)
			{ }
			catch (ApplicationException exc)
			{
				Console.WriteLine("{0} {1}", FormatTime(), Escape(exc.Message));
			}
			catch (Exception exc)
			{
				Console.WriteLine("{0} {1}", FormatTime(), Escape(exc.ToString()));
			}
		}

		static string Escape(string val)
		{
			return val.Replace(Environment.NewLine, "\\r\\n");
		}

		static string FormatTime()
		{
			return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
		}

		public static volatile bool Terminate;
	}
}
