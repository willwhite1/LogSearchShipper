using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace MtLogTailer
{
	static class Program
	{
		static void Main(string[] args)
		{
			ConfigureLogging();

			try
			{
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
				var message = _version + " " + string.Format(format, args);
				Logger.Error(message);
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

		private static void ConfigureLogging()
		{
			var pattern = "%utcdate{ISO8601}Z %message%newline";

			var layout = new PatternLayout(pattern);
			layout.ActivateOptions();

			var appender1 = new ConsoleAppender
			{
				Layout = layout,
			};
			appender1.ActivateOptions();

			var appender2 = new RollingFileAppender
			{
				Layout = layout,
				File = "MtLogTailer.log",
				AppendToFile = true,
				RollingStyle = RollingFileAppender.RollingMode.Size,
				MaximumFileSize = "1MB",
				MaxSizeRollBackups = 2,
			};
			appender2.ActivateOptions();

			BasicConfigurator.Configure(appender1, appender2);

			var assembly = Assembly.GetExecutingAssembly();
			var version = FileVersionInfo.GetVersionInfo(assembly.Location);
			_version = string.Format("{0} {1}", version.OriginalFilename, version.FileVersion);
		}

		public static volatile bool Terminate;
		private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
		private static string _version;
	}
}
