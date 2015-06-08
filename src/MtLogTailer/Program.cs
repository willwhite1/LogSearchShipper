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
				Log(LogLevel.Info, "Starting MT4 tailer process");
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

				Log(LogLevel.Info, "path: {0}, readFromLast: {1}, default encoding: {2}", path, readFromLast, encoding);
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

		public static void Log(LogLevel level, string format, params object[] args)
		{
			try
			{
				var message = string.Format("{0}\t{1}\t{2}\t{3}", level.ToString().ToUpperInvariant(), FormatTime(), _version, string.Format(format, args));
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
			Log(LogLevel.Fatal, Escape(message));
			Environment.Exit(-1);
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

	public enum LogLevel { Debug, Info, Warn, Error, Fatal }
}
