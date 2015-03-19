using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	public class NxLogProcessManagerTests2
	{
		[Test]
		public void TestCloseWhenIdle1()
		{
			var nxLogProcessManager = new NxLogProcessManager
			{
				InputFiles = new List<FileWatchElement>
				{
					new FileWatchElement
					{
						Files = @"C:\Logs\mylog.log",
						Type = "plain",
					}
				},
				OutputSyslog = new SyslogEndpoint("ingestor.example.com", 443),
				FilePollIntervalSeconds = 5,
			};
			nxLogProcessManager.SetupConfigFile();

			try
			{
				var config = File.ReadAllText(nxLogProcessManager.ConfigFile);
				Assert.IsTrue(config.Contains("CloseWhenIdle TRUE"));
			}
			finally
			{
				nxLogProcessManager.Stop();
			}
		}

		[Test]
		public void TestCloseWhenIdle2()
		{
			var nxLogProcessManager = new NxLogProcessManager
			{
				InputFiles = new List<FileWatchElement>
				{
					new FileWatchElement
					{
						Files = @"C:\Logs\mylog.log",
						Type = "plain",
						CloseWhenIdle = false,
					}
				},
				OutputSyslog = new SyslogEndpoint("ingestor.example.com", 443),
				FilePollIntervalSeconds = 5,
			};
			nxLogProcessManager.SetupConfigFile();

			try
			{
				var config = File.ReadAllText(nxLogProcessManager.ConfigFile);
				Assert.IsTrue(config.Contains("CloseWhenIdle FALSE"));
			}
			finally
			{
				nxLogProcessManager.Stop();
			}
		}
	}
}
