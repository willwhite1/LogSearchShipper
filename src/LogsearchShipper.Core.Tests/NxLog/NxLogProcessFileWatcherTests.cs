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

		 Assert.IsTrue(File.Exists(_nxLogProcessManager.NxLogFile +".1"));
		 
		}

		//TODO:  We should ship some data through to ensure that around the time of rotation we don't miss any lines.
		[Test]
		public void ShouldLogLogAllEventsSentToNxLogFile()
		{
		 //Get number of lines without log rotation
		 _nxLogProcessManager.Start();
		 //Console.WriteLine(_nxLogProcessManager.Config);

		 Thread.Sleep(TimeSpan.FromSeconds(5));

		 _nxLogProcessManager.Stop();
		 Thread.Sleep(TimeSpan.FromSeconds(2)); //Give it time to shutdown

		 var withoutRotation = GetLoggedEvents()
										 .Where(item => item.LoggerName == "nxlog.exe")
										 .Select(item => item.MessageObject.ToString())
										 .ToArray();

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
												 .ToArray();
			_nxLogProcessManager.Stop();

			Console.WriteLine("{4}Without rotation:{0}\n{1}\n\n{4}With rotation:{2}\n{3}\n{4}", 
				withoutRotation.Length, string.Join("\n", withoutRotation),
				withRotation.Length, string.Join("\n", withRotation),
				"\n=================================\n");

			//There should be more lines logged with rotation than without due to extra "LogFile C:\Users\david.laing\AppData\Local\Temp\nxlog-data-565083914ab7404aaee03df6e8f759e7\nxlog.log reopened" messages
			Assert.GreaterOrEqual(withRotation.Length, withoutRotation.Length);

			//Every line logged without rotation should appear with rotation
			foreach (var logEntry in withoutRotation)
			{
			 Assert.Contains(logEntry, withRotation);
			}
			
		}												 
	}
 		
}