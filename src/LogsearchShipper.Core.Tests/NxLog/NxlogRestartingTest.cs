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

				File.SetLastWriteTime(edbConfig, DateTime.Now);

				// give the LSS manager some time to restart the nxlog service
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
