using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogFileWatcher
	{
		private readonly NxLogProcessManager _nxLogProcessManager;
	 private static readonly ILog _log = LogManager.GetLogger(typeof(NxLogFileWatcher));
		/// <summary>
		///  log nxlog process output every 250ms
		/// </summary>
		/// <param name="nxLogProcess"></param>
		/// <param name="nxLogFilePath"></param>
		/// <param name="log"></param>
		public void WatchAndLog()
		{
			var _nxLogOutputParser = new NxLogOutputParser();
	
			while (!_nxLogProcessManager.NxLogProcess.HasExited) // reading the old data
			{
			 var lines = ReadAllLines();
				foreach (string line in lines)
				{
				 NxLogOutputParser.NxLogEvent logEvent = _nxLogOutputParser.Parse(line.Trim());
				 _log.Logger.Log(_nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
				}
				Thread.Sleep(TimeSpan.FromMilliseconds(250));
			}
		}

		private int offset = 0;

		public NxLogFileWatcher(NxLogProcessManager nxLogProcessManager)
		{
			_nxLogProcessManager = nxLogProcessManager;
		}

		public string[] ReadAllLines()
		{
		 string[] lines;
			using (var fs = new FileStream(_nxLogProcessManager.NxLogFile,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite))
			{
				using (var sr = new StreamReader(fs))
				{
					var textlines = sr.ReadToEnd();
					lines = textlines.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				}
			}
			if (offset > lines.Length) offset = 0;
		  
			var linesToReturn=lines.Skip(offset).ToArray();
			offset = lines.Length;

			return linesToReturn;
		}
	}
}