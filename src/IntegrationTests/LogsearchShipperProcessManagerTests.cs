using System;
using System.Diagnostics;

using NUnit.Framework;

using LogSearchShipper.Core;

namespace IntegrationTests
{
	[TestFixture]
	[Platform(Exclude = "Mono")]
	public class LogSearchShipperProcessManagerTests
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			_LogSearchShipperProcessManager = new LogSearchShipperProcessManager();
			_nxLogProcessId = _LogSearchShipperProcessManager.Start();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			_LogSearchShipperProcessManager.Stop();
		}

		private LogSearchShipperProcessManager _LogSearchShipperProcessManager;
		private int _nxLogProcessId;

		[Test]
		public void ShouldLaunchNxLogProcess()
		{
			Assert.IsNotNull(Process.GetProcessById(Convert.ToInt32(_nxLogProcessId)), "a NXLog process wasn't started");
		}

		[Test]
		public void ShouldLaunchWithCorrectInputFiles()
		{
			Assert.AreEqual("myfile.log", _LogSearchShipperProcessManager.NxLogProcessManager.InputFiles[0].Files);
			Assert.AreEqual("myfile_type", _LogSearchShipperProcessManager.NxLogProcessManager.InputFiles[0].Type);

			Assert.AreEqual("\\\\ENV1-APP01\\Logs\\u*.log",
				_LogSearchShipperProcessManager.NxLogProcessManager.InputFiles[4].Files);
			Assert.AreEqual("IIS7", _LogSearchShipperProcessManager.NxLogProcessManager.InputFiles[4].Type);
		}

		[Test]
		public void ShouldLaunchWithCorrectOutputSyslog()
		{
			Assert.AreEqual("ingestor.example.com", _LogSearchShipperProcessManager.NxLogProcessManager.OutputIngestor.Host);
			Assert.AreEqual(443, _LogSearchShipperProcessManager.NxLogProcessManager.OutputIngestor.Port);
		}
	}
}