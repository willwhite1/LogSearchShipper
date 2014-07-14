using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
	[TestFixture]
	public class LogsearchShipperProcessManagerTests
	{
		[SetUp]
		public void Setup()
		{
			_logsearchShipperProcessManager = new LogsearchShipperProcessManager();
		}

		[TearDown]
		public void TearDown()
		{
			_logsearchShipperProcessManager.Stop();
		}

		private LogsearchShipperProcessManager _logsearchShipperProcessManager;

		[Test]
		public void ShouldCreateDataFolderIfItDoesntExist()
		{
			if (Directory.Exists(_logsearchShipperProcessManager.LogsearchShipperConfig.DataFolder))
			{
					Directory.Delete(_logsearchShipperProcessManager.LogsearchShipperConfig.DataFolder, true);
			}

			var dataFolder = _logsearchShipperProcessManager.NXLogDataFolder;  //this should create it

				Assert.IsTrue(Directory.Exists(dataFolder), string.Format("DataFolder {0} should have been created, but wasn't", dataFolder));
		}

		[Test]
		public void ShouldStoreConfigFileInDataFolder()
		{
				_logsearchShipperProcessManager.SetupConfigFile();
				Assert.AreEqual(Path.Combine(_logsearchShipperProcessManager.NXLogDataFolder,"nxlog.conf"), _logsearchShipperProcessManager.ConfigFile);
		}


		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectModuledir()
		{
				AssertConfigContains(@"ModuleDir	{0}\modules", Path.GetFullPath(_logsearchShipperProcessManager.NXLogBinFolder));
		}

		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectPidfile()
		{
				AssertConfigContains(@"PidFile		{0}\nxlog.pid", Path.GetFullPath(_logsearchShipperProcessManager.NXLogDataFolder));
		}

		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectSpoolDir()
		{
				AssertConfigContains(@"SpoolDir	{0}", Path.GetDirectoryName(Assembly.GetAssembly(typeof(LogsearchShipperProcessManager)).Location));
		}
			
		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectCacheDir()
		{
				AssertConfigContains(@"CacheDir	{0}", Path.GetFullPath(_logsearchShipperProcessManager.NXLogDataFolder));
		}

		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectLogLevelBasedOnLog4NETSetting()
		{
				var log4NetLogLevel = LogManager.GetLogger(typeof(LogsearchShipperProcessManager)).IsDebugEnabled ? "DEBUG" : "INFO";
				AssertConfigContains("LogLevel {0}", log4NetLogLevel);
		}

		[Test]
		public void ShouldGenerateNXLogConfigWithCorrectSyslogOutputSettings()
		{
				AssertConfigContains("<Extension syslog>");
				AssertConfigContains("Module	xm_syslog");

				AssertConfigContains("<Output out>");
				AssertConfigContains("Module	om_ssl");

				AssertConfigContains("Host	ingestor.example.com");
				AssertConfigContains("Port	443");
				AssertConfigContains("Exec	to_syslog_ietf();");
		}
		private void AssertConfigContains(string containing, params object[] substitutions)
		{
			_logsearchShipperProcessManager.SetupConfigFile();
			var config = File.ReadAllText(_logsearchShipperProcessManager.ConfigFile);
			StringAssert.Contains(string.Format(containing, substitutions), config);
		}

		[Test, Ignore("Needs valid logstash ingestor to connect to")]
		[Platform(Exclude = "Mono")]
		public void ShouldLaunchNxLogProcess()
		{
			_logsearchShipperProcessManager.Start();

			Process[] processes = Process.GetProcessesByName("nxlog.exe");

			Assert.AreEqual(1, processes.Count(), "a NXLog process wasn't started");
		}
	}
}