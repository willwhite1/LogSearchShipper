﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;
using RunProcess;

namespace LogSearchShipper.Core.Tests.NxLog
{
	[TestFixture]
	public class NxLogProcessManagerTests
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

		private void AssertConfigContains(string containing, params object[] substitutions)
		{
			string config = File.ReadAllText(_nxLogProcessManager.ConfigFile);
			StringAssert.Contains(string.Format(containing, substitutions), config);
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectCacheDir()
		{
			AssertConfigContains(@"CacheDir	{0}", Path.GetFullPath(_nxLogProcessManager.DataFolder));
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectModuledir()
		{
			AssertConfigContains(@"ModuleDir	{0}\modules", Path.GetFullPath(_nxLogProcessManager.BinFolder));
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectPidfile()
		{
			AssertConfigContains(@"PidFile		{0}\nxlog.pid", Path.GetFullPath(_nxLogProcessManager.DataFolder));
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectSpoolDir()
		{
			AssertConfigContains(@"SpoolDir	{0}",
			Path.GetDirectoryName(Assembly.GetAssembly(typeof (NxLogProcessManager)).Location));
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectLogFile()
		{
		 AssertConfigContains(@"LogFile		{0}", _nxLogProcessManager.NxLogFile);
		}

	 [Test]
	 public void ShouldGenerateNXLogConfigWithCorrectLogLevelBasedOnLog4NETSetting()
	 {
		 var expectedLevel = LogManager.GetLogger(typeof(NxLogProcessManager)).IsDebugEnabled ? "DEBUG" : "INFO";
		 AssertConfigContains("LogLevel	{0}", expectedLevel);
	 }

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectSyslogOutputSettings()
		{
			AssertConfigContains("<Extension syslog>");
			AssertConfigContains("Module	xm_syslog");

			AssertConfigContains("<Output out_syslog");
			AssertConfigContains("Module	om_ssl");

			AssertConfigContains("Host	ingestor.example.com");
			AssertConfigContains("Port	443");
			AssertConfigContains("Exec	to_syslog_ietf();");
		}

		[Test]
		[Platform(Exclude = "Mono")]
		public void ShouldLaunchNxLogProcess()
		{
			var _processId = _nxLogProcessManager.Start();

		 Thread.Sleep(TimeSpan.FromSeconds(1));

			Assert.IsNotNull(Process.GetProcessById(Convert.ToInt32(_processId)), "a NXLog process wasn't started");
		}

		[Test]
		public void ShouldStoreConfigFileInDataFolder()
		{
			Assert.AreEqual(Path.Combine(_nxLogProcessManager.DataFolder, "nxlog.conf"), _nxLogProcessManager.ConfigFile);
		}
	}
}