using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests.NxLog
{
 [TestFixture]
 [Platform(Exclude = "Mono")]
	public class NxLogProcessFileWatcherTests
	{
		[SetUp]
		public void Setup()
		{
			_nxLogProcessManager = new NxLogProcessManager
			{
				InputFiles = new List<FileWatchElement>(),
				OutputSyslog = new SyslogEndpoint("ingestor.example.com", 443)
			};
			_nxLogProcessManager.SetupConfigFile();
		}

		[TearDown]
		public void TearDown()
		{
			_nxLogProcessManager.Stop();
		}

		private NxLogProcessManager _nxLogProcessManager;

		[Test]

		public void ShouldLogEventsSendToNxLogFile()
		{
			_nxLogProcessManager.Start();

		 Thread.Sleep(TimeSpan.FromSeconds(1));

		 CollectionAssert.Contains(GetLoggedRenderedMessages(), "nxlog-ce-2.8.1248 started");
		}

	 public string[] GetLoggedRenderedMessages()
	 {
		 return GetLoggedEvents().Select(item => item.RenderedMessage).ToArray();
	 }

	 private static LoggingEvent[] GetLoggedEvents()
	 {
		 Hierarchy hierarchy = LogManager.GetLoggerRepository() as Hierarchy;
		 MemoryAppender appender = hierarchy.Root.GetAppender("MemoryAppender") as MemoryAppender;
		 var eventList = appender.GetEvents();
		 return eventList;
	 }


	 private static void ClearMemoryAppenderEvents()
	 {
		 Hierarchy hierarchy = LogManager.GetLoggerRepository() as Hierarchy;
		 MemoryAppender appender = hierarchy.Root.GetAppender("MemoryAppender") as MemoryAppender;
		 appender.Clear();
	 }

		[Test]
		public void NxLogFileShouldBeRotated()
		{
			_nxLogProcessManager.MaxNxLogFileSize = "0K";
			_nxLogProcessManager.RotateNxLogFileEvery = "1 sec";
		 _nxLogProcessManager.Start();
		 //Console.WriteLine(_nxLogProcessManager.Config);

		 Thread.Sleep(TimeSpan.FromSeconds(3));

		 Assert.IsTrue(File.Exists(_nxLogProcessManager.NxLogFile +".1"));
		 
		}

		//TODO:  We should ship some data through to ensure that around the time of rotation we don't miss any lines.
		[Test]
		public void ShouldLogAllEventsSentToNxLogFile()
		{
			ClearMemoryAppenderEvents();
			//Get number of lines without log rotation
			_nxLogProcessManager.Start();
			//Console.WriteLine(_nxLogProcessManager.Config);

			Thread.Sleep(TimeSpan.FromSeconds(5));

			var withoutRotation = GetLoggedEvents()
										.Where(item => item.LoggerName == "nxlog.exe")
										.Select(item => item.MessageObject.ToString())
										.ToList();

			_nxLogProcessManager.Stop();
			Thread.Sleep(TimeSpan.FromSeconds(2)); //Give it time to shutdown

			//Get number of lines with lots of log rotation
			ClearMemoryAppenderEvents();
			_nxLogProcessManager.MaxNxLogFileSize = "0K";
			_nxLogProcessManager.RotateNxLogFileEvery = "2 sec";
			_nxLogProcessManager.SetupConfigFile();
			_nxLogProcessManager.StartNxLogProcess();

			Thread.Sleep(TimeSpan.FromSeconds(5));

			var withRotation = GetLoggedEvents()
												 .Where(item => item.LoggerName == "nxlog.exe")
												 .Select(item => item.MessageObject.ToString())
												 .ToList();
			_nxLogProcessManager.Stop();

			Console.WriteLine("{4}Without rotation:{0}\n{1}\n\n{4}With rotation:{2}\n{3}\n{4}",
				withoutRotation.Count, string.Join("\n", withoutRotation),
				withRotation.Count, string.Join("\n", withRotation),
				"\n=================================\n");

			//There should be more lines logged with rotation than without due to extra "LogFile C:\Users\david.laing\AppData\Local\Temp\nxlog-data-565083914ab7404aaee03df6e8f759e7\nxlog.log reopened" messages
			Assert.GreaterOrEqual(withRotation.Count, withoutRotation.Count);

			//Every line logged without rotation should appear once with rotation
			foreach (var logEntry in withoutRotation)
			{
				var matches = withRotation.FindAll(l => l == logEntry);
				Assert.AreEqual(1, matches.Count, string.Format("{0} should only appear 1 time, but actually appears {1} times with rotation", logEntry, matches.Count));
			}
		}

		[Test]
		public void WatcherShouldNotMissAnyLogMessagesDuringNxLogFileRotation()
 		{
			INxLogProcessManager mockNxLogProcessManager = new MockNxLogProcessManager("mockNxLogFile.log");
 			var nxLogFileWatcher = new NxLogFileWatcher(mockNxLogProcessManager);
			if (File.Exists("mockNxLogFile.log")) File.Delete("mockNxLogFile.log");
 			var allLinesRead = new List<string>();

		  //Before log file rotation
			AppendLines("mockNxLogFile.log", 
			 "Line 1",
			 "Line 2",
			 "Line 3");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

 			RotateLogFile("mockNxLogFile.log");
			AppendLines("mockNxLogFile.log",
				"Line 4");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

		  //Add some more
			AppendLines("mockNxLogFile.log",
				"Line 5");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

			AppendLines("mockNxLogFile.log",
				"Line 6");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

 			Console.WriteLine("Append a few lines just before the log rotation happens - these should be read from the rotated log");
		  AppendLines("mockNxLogFile.log",
				"Line 7",
				"Line 8",
				"Line 9");

			RotateLogFile("mockNxLogFile.log");
			AppendLines("mockNxLogFile.log",
				"Line 10");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

			//Add some more
			AppendLines("mockNxLogFile.log",
				"Line 11");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

			AppendLines("mockNxLogFile.log",
				"Line 12");

			ReadLogLines(allLinesRead, nxLogFileWatcher);

 			Assert.AreEqual(12, allLinesRead.Count);
		  Assert.AreEqual(@"Line 1
Line 2
Line 3
Line 4
Line 5
Line 6
Line 7
Line 8
Line 9
Line 10
Line 11
Line 12".Replace("\r",""), string.Join("\n", allLinesRead));
		}

	 private static void ReadLogLines(List<string> allLinesRead, NxLogFileWatcher nxLogFileWatcher)
	 {
		 allLinesRead.AddRange(nxLogFileWatcher.ReadNewLinesAddedToLogFile());
		 Console.WriteLine("{0}: {1} \n=========\n{2}\n=========\n", DateTime.Now, "Logs read", string.Join("\r\n", allLinesRead));
	 }

	 private void RotateLogFile(string fileName)
	 {
		 if (File.Exists(fileName + ".1")) File.Delete(fileName + ".1");
		 File.Move(fileName, fileName + ".1");
	 }
	 private void AppendLines(string fileName, params string[] lines )
	 {
		  if (!File.Exists(fileName)) File.WriteAllText(fileName,"");
		  File.AppendAllLines(fileName, lines);

		  Console.WriteLine("{0}: {1} \n=========\n{2}\n=========\n", DateTime.Now, fileName, File.ReadAllText(fileName));
	 }
	 public class MockNxLogProcessManager	 : INxLogProcessManager
	 {
		 public MockNxLogProcessManager(string mocknxlogfileLog)
		 {
			 NxLogFile = mocknxlogfileLog;
			 NxLogProcess = Process.GetCurrentProcess();
		 }

		 public SyslogEndpoint InputSyslog { get; set; }
		 public List<FileWatchElement> InputFiles { get; set; }
		 public SyslogEndpoint OutputSyslog { get; set; }
		 public string OutputFile { get; set; }
		 public string ConfigFile { get; private set; }
		 public string BinFolder { get; private set; }
		 public string DataFolder { get; private set; }
		 public string Config { get; private set; }
		 public string MaxNxLogFileSize { get; set; }

		 public string NxLogFile { get; private set; }

		 public string RotateNxLogFileEvery { get; set; }

		 public Process NxLogProcess { get; private set; }

		 public int Start()
		 {
			 throw new NotImplementedException();
		 }

		 public void Dispose()
		 {
			 throw new NotImplementedException();
		 }

		 public void Stop()
		 {
			 throw new NotImplementedException();
		 }
	 }
	}
}