using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using log4net;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests.NxLog
{
	[TestFixture]
	public class NxLogProcessManagerTests
	{
		private const string NxlogCustomConfigLine1 = "Exec if ($raw_event =~ /DEBUG/ ) drop();";
		private const string NxlogCustomConfigLine2 = "	Exec   if not ($raw_event =~ /MarginCalculationHandler/ ) drop();";

		[SetUp]
		public void Setup()
		{
			var dataPath = Path.GetFullPath(@".\NxLogProcessManagerTests");

			_nxLogProcessManager = new NxLogProcessManager(dataPath, "NxLogProcessManagerTests")
			{
				InputFiles = new List<FileWatchElement>{
				new FileWatchElement
					{
						Files = @"C:\Logs\mylog.log",
						Type = "plain",
						CustomNxlogConfig = new CustomNxlogConfig
							{
								Value = NxlogCustomConfigLine1 + Environment.NewLine + NxlogCustomConfigLine2
							},
					}
				},
				OutputIngestor = new Endpoint("ingestor.example.com", 443),
				FilePollIntervalSeconds = 5,
			};
			_nxLogProcessManager.RegisterNxlogService();
			_nxLogProcessManager.SetupConfigFile();
		}

		[TearDown]
		public void TearDown()
		{
			_nxLogProcessManager.Stop();
			_nxLogProcessManager.Dispose();
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
			Path.GetDirectoryName(Assembly.GetAssembly(typeof(NxLogProcessManager)).Location));
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithCorrectLogFile()
		{
			AssertConfigContains(@"LogFile		{0}", _nxLogProcessManager.NxLogFile);
		}

		[Test]
		public void ShouldGenerateNxLogConfigWithMultiline()
		{
			AssertConfigContains(@"Module	xm_multiline");
			AssertConfigContains(@"HeaderLine	/^([^ ]+).*/");
			AssertConfigContains(@"InputType	multiline");
		}

		[Test]
		public void ShouldEscapeConfigPathsCorrectly()
		{
			var config = File.ReadAllText(_nxLogProcessManager.ConfigFile);

			var inputRegex = new Regex("<Input[ _a-z0-9]+>(.*?)</Input>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

			// looks for unescaped slash char
			var notEscapedRegex = new Regex(@"(?<!\\)\\(?!\\)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

			var inputMatch = inputRegex.Match(config);
			var atLeastOneInput = false;
			while (inputMatch.Success)
			{
				var inputText = inputMatch.Groups[1].Value;
				var lines = inputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				lines = lines.Select(val => val.Trim()).ToArray();

				foreach (var filePathLine in lines.Where(val => val.StartsWith("File")))
				{
					Assert.IsTrue(!notEscapedRegex.Match(filePathLine).Success,
						string.Format("The File value within '{0}' SHOULD be escaped, but isn't", filePathLine));
				}

				foreach (var execLine in lines.Where(val => val.StartsWith("Exec")))
				{
					if (!execLine.Contains('\\'))
						continue;
					Assert.IsTrue(notEscapedRegex.Match(execLine).Success,
						string.Format("The $path value within '{0}' should NOT be escaped but is", execLine));
				}

				atLeastOneInput = true;
				inputMatch = inputMatch.NextMatch();
			}

			Assert.IsTrue(atLeastOneInput);
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

			AssertConfigContains("<Output out_logz");
			AssertConfigContains("Module  om_ssl");

            AssertConfigContains("Host    ingestor.example.com");
            AssertConfigContains("Port    443");			
		}


		[Test]
		public void ShouldStoreConfigFileInDataFolder()
		{
			Assert.AreEqual(Path.Combine(_nxLogProcessManager.DataFolder, "nxlog.conf"), _nxLogProcessManager.ConfigFile);
		}

		[Test]
		public void ShouldContainCustomNxlog()
		{
			AssertConfigContains(NxlogCustomConfigLine1);
			AssertConfigContains(NxlogCustomConfigLine2);
		}

		[Test]
		public void ShouldSetPollingIntervals()
		{
			AssertConfigContains(@"PollInterval 5");
			AssertConfigContains(@"DirCheckInterval 10");
		}
	}
}