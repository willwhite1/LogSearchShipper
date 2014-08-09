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
		private readonly NxLogProcessManager _nxLogProcessManager;

		private int offset;

		public NxLogFileWatcher(NxLogProcessManager nxLogProcessManager)
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
			var lines = FaultTolerantReadAllTextFromFile(_nxLogProcessManager.NxLogFile);
			
			if (offset > lines.Count)
			{
				//The log file has rotated; so we need to grab some lines from the old file
				var linesFromOldLogFile = FaultTolerantReadAllTextFromFile(_nxLogProcessManager.NxLogFile + ".1");
				lines.InsertRange(0, linesFromOldLogFile.Skip(offset).ToList());
				
				//And reset the offset 
				offset = 0;
			}

			var linesToReturn = lines.Skip(offset).ToList();
			offset = lines.Count;

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