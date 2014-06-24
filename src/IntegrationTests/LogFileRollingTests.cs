using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	public class LogFileRollingTests
	{
		[Test]
		public void ShippingShouldNotBlockLogFileRolling()
		{
			Process shipper = null;
			Process processWithLogFileRolling = null;

			DeleteOldLogFiles();

			File.Delete("LogsearchShipper.Service.exe.config.bak");
			File.Move("LogsearchShipper.Service.exe.config", "LogsearchShipper.Service.exe.config.bak");
			File.Move("LogsearchShipper.Service.exe.config.ShipDummyService", "LogsearchShipper.Service.exe.config");
			try
			{
				shipper = Utils.StartProcess(Environment.CurrentDirectory + @"\LogsearchShipper.Service.exe", "-instance:integrationtest001");
				processWithLogFileRolling = Utils.StartProcess(Environment.CurrentDirectory + @"\DummyServiceWithLogRolling.exe", "");

				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

				//There should be 6 DummyServiceWithLogRolling.log.* files, unless the shipper has blocked file rolling
				var logFiles =
					new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*");
				Assert.AreEqual(6, logFiles.Count());

			}
			finally
			{
				Utils.ShutdownProcess(shipper);
				Utils.ShutdownProcess(processWithLogFileRolling);

				File.Delete("LogsearchShipper.Service.exe.config");
				File.Move("LogsearchShipper.Service.exe.config.bak", "LogsearchShipper.Service.exe.config");
			}

		}

		private static void DeleteOldLogFiles()
		{
			foreach (FileInfo f in new DirectoryInfo(Environment.CurrentDirectory).GetFiles("DummyServiceWithLogRolling.log.*"))
			{
				f.Delete();
			}
		}
	}
}
