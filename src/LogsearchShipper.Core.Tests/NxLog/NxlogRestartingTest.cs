using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using NUnit.Framework;

namespace LogSearchShipper.Core.Tests.NxLog
{
	class NxlogRestartingTest
	{
		[SetUp]
		public void Init()
		{
			_lssManager = new LogSearchShipperProcessManager();
			_edbConfig = _lssManager.LogSearchShipperConfig.EDBFileWatchers[0].DataFile;
			
			_bakFile = _edbConfig + ".bak";
			File.Copy(_edbConfig, _bakFile);
		}

		[TearDown]
		public void Cleanup()
		{
			File.Delete(_edbConfig);
			File.Move(_bakFile, _edbConfig);
		}

		[Test]
		public void TestNxlogRestartsOnConfigUpdate()
		{
			var nxLogProcessId = _lssManager.Start();

			try
			{
				var config = new XmlDocument();
				config.Load(_edbConfig);

				var nodes = config.SelectNodes("/Environment/Servers/Server/Name");
				foreach (XmlElement node in nodes)
				{
					node.InnerText += "_TEST_UPDATE";
				}

				config.Save(_edbConfig);

				// give the LSS manager some time to restart the nxlog service
				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId != _lssManager.NxLogProcessManager.NxLogProcess.Id);
			}
			finally
			{
				_lssManager.Stop();
			}
		}

		[Test]
		public void TestNxlogDoesntRestartWhenConfigIsTheSame()
		{
			var nxLogProcessId = _lssManager.Start();

			try
			{
				// test restarting when the config file last write time is updated without changing content
				File.SetLastWriteTime(_edbConfig, DateTime.Now);

				// give the LSS manager some time to restart the nxlog service
				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId == _lssManager.NxLogProcessManager.NxLogProcess.Id);

				// test restarting when a config file update doesn't affect the nxlog config
				var config = new XmlDocument();
				config.Load(_edbConfig);

				var nodes = config.SelectNodes("/Environment");
				foreach (XmlElement node in nodes)
				{
					node.InsertAfter(config.CreateElement("TEST_UPDATE"), node.ChildNodes[0]);
				}

				config.Save(_edbConfig);

				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId == _lssManager.NxLogProcessManager.NxLogProcess.Id);
			}
			finally
			{
				_lssManager.Stop();
			}
		}

		private LogSearchShipperProcessManager _lssManager;
		private string _edbConfig;
		private string _bakFile;
	}
}
