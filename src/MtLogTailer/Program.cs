using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
				InitLogging();

				var argsDic = CommandLineUtil.ParseArgs(args);

				string path;
				if (!argsDic.TryGetValue("", out path))
					ThrowInvalidArgs();

				//Default to gb2312-> ANSI/OEM Simplified Chinese (PRC, Singapore); Chinese Simplified (GB2312)
				var encoding = 936;
				string encodingText;
				if (argsDic.TryGetValue("encoding", out encodingText))
					encoding = Convert.ToInt32(encodingText);

				var readFromLast = true;
				string readFromLastText;
				if (argsDic.TryGetValue("readFromLast", out readFromLastText))
					readFromLast = bool.Parse(readFromLastText);

				Console.OutputEncoding = Encoding.UTF8;

				var watcher = new PathWatcher(path, encoding, readFromLast);

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
				LogError(exc.Message);
			}
			catch (Exception exc)
			{
				LogError(exc.ToString());
			}
		}

		private static void ThrowInvalidArgs()
		{
			throw new ApplicationException("Invalid args. Should use MtLogTailer.exe <filePath> (-encoding:int)? (-readFromLast:bool)?");
		}

		static void LogErrorFormat(string format, params object[] args)
		{
			try
			{
				var message = string.Format("{0} {1} {2}", FormatTime(), _version, string.Format(format, args));
				Console.WriteLine(message);
			}
			catch (Exception exc)
			{
				Console.WriteLine(Escape(exc.ToString()));
			}
		}

		static string Escape(string val)
		{
			return val.Replace(Environment.NewLine, "\\r\\n");
		}

		public static void LogError(string message)
		{
			LogErrorFormat(Escape(message));
		}

		static string FormatTime()
		{
			return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
		}

		static void InitLogging()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var version = FileVersionInfo.GetVersionInfo(assembly.Location);
			_version = string.Format("{0} {1}", version.OriginalFilename, version.FileVersion);
		}

		public static volatile bool Terminate;
		private static string _version;
	}
}
