using System;
using System.Diagnostics;
using System.IO;

using LogSearchShipper.Core;
using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	public class SimpleTests
	{
		private static readonly string JsonLogFilePath = Environment.CurrentDirectory +
		                                           @"\json.log";
		[Test]
		public void ShouldSuccessfullyShipSimpleJson()
		{
			Process shipper = null;

			Console.WriteLine("Truncating {0}", JsonLogFilePath);
			File.WriteAllText(JsonLogFilePath, string.Empty);

			File.Delete("LogSearchShipper.exe.config.bak");
			File.Move("LogSearchShipper.exe.config", "LogSearchShipper.exe.config.bak");
			File.Copy("LogSearchShipper.exe.config.ShouldSuccessfullyShipSimpleJSON", "LogSearchShipper.exe.config");
			try
			{
				shipper = ProcessUtils.StartProcess(Environment.CurrentDirectory + @"\LogSearchShipper.exe",
					"-instance:ShouldSuccessfullyShipSimpleJSON");

				Console.WriteLine("Waiting 30 seconds for shipper to startup...");
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));

				Console.WriteLine("Writing data to {0}", JsonLogFilePath);
				File.WriteAllLines(JsonLogFilePath, new[] { string.Format(@"{{ ""@timestamp"":""{0}"", ""foo"":""bar"" }}", DateTime.UtcNow.ToString("O")) });

				Console.WriteLine("Waiting 30 seconds for shipper to ship data...");
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(45));

			}
			finally
			{
				Utils.ShutdownProcess(shipper);

				File.Delete("LogSearchShipper.exe.config");
				File.Move("LogSearchShipper.exe.config.bak", "LogSearchShipper.exe.config");
			}

		}

	}
}
