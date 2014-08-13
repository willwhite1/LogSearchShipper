using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogFileWatcher
	{
		private const int MaxReadFails = 5;
		private static readonly ILog _log = LogManager.GetLogger(typeof (NxLogFileWatcher));
		private readonly INxLogProcessManager _nxLogProcessManager;

		private int offset;

		public NxLogFileWatcher(INxLogProcessManager nxLogProcessManager)
		{
			_nxLogProcessManager = nxLogProcessManager;
		}

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
				string[] lines = ReadNewLinesAddedToLogFile();
				foreach (string line in lines)
				{
					NxLogOutputParser.NxLogEvent logEvent = _nxLogOutputParser.Parse(line.Trim());
					_log.Logger.Log(_nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
				}
				Thread.Sleep(TimeSpan.FromMilliseconds(250));
			}
		}

		public string[] ReadNewLinesAddedToLogFile()
		{
		 var linesToReturn = new List<string>();
		 var linesFromCurrentLogFile = FaultTolerantReadAllTextFromFile(_nxLogProcessManager.NxLogFile);

		 if (offset > linesFromCurrentLogFile.Count)
		 {
			//The log file has rotated; so we need to grab any extra lines from the old file
			var linesFromOldLogFile = FaultTolerantReadAllTextFromFile(_nxLogProcessManager.NxLogFile + ".1");
			linesToReturn.AddRange(linesFromOldLogFile.Skip(offset));

			//Reset the [current file] offset, so we start from the beginning of the new current file
			offset = 0;
		 }

		 linesToReturn.AddRange(linesFromCurrentLogFile.Skip(offset));

		 //Increment the [current file] offset so we don't resend the same lines again next time we're called
		 offset = linesFromCurrentLogFile.Count;

		 return linesToReturn.ToArray();
		}

		private static List<string> FaultTolerantReadAllTextFromFile(string logFile)
		{
			string textReadFromLogFile = "";
			try
			{
				using (var fs = new FileStream(logFile,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite))
				{
					using (var sr = new StreamReader(fs))
					{
						int failCounter = 0;
						while (textReadFromLogFile.Length == 0 && failCounter < MaxReadFails)
						{
							try
							{
								textReadFromLogFile = sr.ReadToEnd();
							}
							catch (IOException ex)
							{
								failCounter++;
								if (failCounter == MaxReadFails)
								{
									_log.WarnFormat("Failed to read log lines from {0} due to {1}", logFile, ex.Message);
								}
								else
								{
									Thread.Sleep(TimeSpan.FromMilliseconds(1));
								}
							}
						}
					}
				}
			}
			catch (FileNotFoundException fnfe)
			{
				textReadFromLogFile = ""; //treat a missing file as one containing no data
			}

			return textReadFromLogFile.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		}
	}
}