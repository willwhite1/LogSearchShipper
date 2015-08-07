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
		[Test]
		public void TestNxlogRestartsOnConfigUpdate()
		{
			var lssManager = new LogSearchShipperProcessManager();
			var nxLogProcessId = lssManager.Start();

			try
			{
				var edbConfig = lssManager.LogSearchShipperConfig.EDBFileWatchers[0].DataFile;

				var config = new XmlDocument();
				config.Load(edbConfig);

				var nodes = config.SelectNodes("/Environment/Servers/Server/Name");
				foreach (XmlElement node in nodes)
				{
					node.InnerText += "_TEST_UPDATE";
				}

				config.Save(edbConfig);

				// give the LSS manager some time to restart the nxlog service
				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId != lssManager.NxLogProcessManager.NxLogProcess.Id);
			}
			finally
			{
				lssManager.Stop();
			}
		}

		[Test]
		public void TestNxlogDoesntRestartWhenConfigIsTheSame()
		{
			var lssManager = new LogSearchShipperProcessManager();
			var nxLogProcessId = lssManager.Start();

			try
			{
				var edbConfig = lssManager.LogSearchShipperConfig.EDBFileWatchers[0].DataFile;

				// test restarting when the config file last write time is updated without changing content
				File.SetLastWriteTime(edbConfig, DateTime.Now);

				// give the LSS manager some time to restart the nxlog service
				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId == lssManager.NxLogProcessManager.NxLogProcess.Id);

				// test restarting when a config file update doesn't affect the nxlog config
				var config = new XmlDocument();
				config.Load(edbConfig);

				var nodes = config.SelectNodes("/Environment");
				foreach (XmlElement node in nodes)
				{
					node.InsertAfter(config.CreateElement("TEST_UPDATE"), node.ChildNodes[0]);
				}

				config.Save(edbConfig);

				Thread.Sleep(TimeSpan.FromSeconds(10));

				Assert.IsTrue(nxLogProcessId == lssManager.NxLogProcessManager.NxLogProcess.Id);
			}
			finally
			{
				lssManager.Stop();
			}
		}
	}
}
