using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;
using RunProcess;

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

		 CollectionAssert.Contains(GetLoggedRenderedMessages(),"nxlog-ce-2.7.1191 started");
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

		 CollectionAssert.DoesNotContain(GetLoggedRenderedMessages(), 
			string.Format("failed to rename file from '{0}' to '{0}.1': The process cannot access the file because it is being used by another process.", 
			_nxLogProcessManager.NxLogFile));
		 CollectionAssert.Contains(GetLoggedRenderedMessages(), string.Format("LogFile {0} reopened",
			_nxLogProcessManager.NxLogFile));

		 Assert.IsTrue(File.Exists(_nxLogProcessManager.NxLogFile +".1"));
		 
		}

		[Test]
		public void ShouldLogLogAllEventsSentToNxLogFile()
		{
		 //Get number of lines without log rotation
		 _nxLogProcessManager.Start();
		 //Console.WriteLine(_nxLogProcessManager.Config);

		 Thread.Sleep(TimeSpan.FromSeconds(2));

			var withoutRotation = new NxLogFileWatcher(_nxLogProcessManager).ReadAllLines();
			_nxLogProcessManager.Stop();

		 //Get number of lines with lots of log rotation
			ClearMemoryAppenderEvents();
			_nxLogProcessManager.MaxNxLogFileSize = "0K";
			_nxLogProcessManager.RotateNxLogFileEvery = "1 sec";
			_nxLogProcessManager.SetupConfigFile();
			_nxLogProcessManager.StartNxLogProcess();

			Thread.Sleep(TimeSpan.FromSeconds(2));

			var withRotation = GetLoggedEvents()
												 .Where(item => item.LoggerName == "nxlog.exe")
												 .Select(item => item.MessageObject.ToString())
												 .ToArray();
		 //Check we get approx the same number of lines with log rotation as without
			Console.WriteLine("{4}Without rotation:{0}\n{1}\n\n{4}With rotation:{2}\n{3}\n{4}", 
				withoutRotation.Length, string.Join("\n", withoutRotation),
				withRotation.Length, string.Join("\n", withRotation),
				"\n=================================\n");
			
			Assert.GreaterOrEqual(withRotation.Length, withoutRotation.Length);
			Assert.LessOrEqual(withRotation.Length, withoutRotation.Length + 3);
		}												 
	}
 		
}