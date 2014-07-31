using System;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogFileWatcher
	{
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
				string[] lines = ReadAllLines();
				foreach (string line in lines)
				{
					NxLogOutputParser.NxLogEvent logEvent = _nxLogOutputParser.Parse(line.Trim());
					_log.Logger.Log(_nxLogOutputParser.ConvertToLog4Net(_log, logEvent));
				}
				Thread.Sleep(TimeSpan.FromMilliseconds(250));
			}
		}

		public string[] ReadAllLines()
		{
			string textReadFromLogFile = "";
			const int maxReadFails = 5;

			try
			{
			 using (var fs = new FileStream(_nxLogProcessManager.NxLogFile,
				 FileMode.Open,
				 FileAccess.Read,
				 FileShare.ReadWrite))
			 {
				 using (var sr = new StreamReader(fs))
				 {
					 int failCounter = 0;
					 while (textReadFromLogFile.Length == 0 && failCounter < maxReadFails)
					 {
						 try
						 {
							 textReadFromLogFile = sr.ReadToEnd();
						 }
						 catch (IOException ex)
						 {
							 failCounter++;
							 if (failCounter == maxReadFails)
							 {
								 _log.WarnFormat("Failed to read log lines from {0} due to {1}", _nxLogProcessManager.NxLogFile, ex.Message);
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
			  //Ignore - will try again later
			}

			if (textReadFromLogFile.Length == 0)
			{
				return new string[] {};
			}

			string[] lines = textReadFromLogFile.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			if (offset > lines.Length) offset = 0;

			var linesToReturn = lines.Skip(offset).ToList();
			offset = lines.Length;

			return linesToReturn.ToArray();
		}
	}
}