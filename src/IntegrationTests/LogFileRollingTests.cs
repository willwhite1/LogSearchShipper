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

			File.Delete("LogSearchShipper.exe.config.bak");
			File.Move("LogSearchShipper.exe.config", "LogSearchShipper.exe.config.bak");
			File.Move("LogSearchShipper.exe.config.ShipDummyService", "LogSearchShipper.exe.config");
			try
			{
				shipper = Utils.StartProcess(Environment.CurrentDirectory + @"\LogSearchShipper.exe",
					"-instance:integrationtest001");
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

				File.Delete("LogSearchShipper.exe.config");
				File.Move("LogSearchShipper.exe.config.bak", "LogSearchShipper.exe.config");
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
