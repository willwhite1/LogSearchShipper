using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	public class UnicodeCharacterTests
	{
		private static string Log4NetLogFilePath = Environment.CurrentDirectory +
		                                           @"\Log4NetLogContainingUnicodeCharacters.log";
		[Test]
		public void ShouldSuccessfullyLog4NetLogContainingUnicodeCharacters()
		{

			Process shipper = null;

			File.WriteAllText(Log4NetLogFilePath,	string.Empty);

			File.Delete("LogSearchShipper.exe.config.bak");
			File.Move("LogSearchShipper.exe.config", "LogSearchShipper.exe.config.bak");
			File.Copy("LogSearchShipper.exe.config.ShouldSuccessfullyShipLog4NetLogContainingUnicodeCharacters", "LogSearchShipper.exe.config");
			try
			{
				shipper = Utils.StartProcess(Environment.CurrentDirectory + @"\LogSearchShipper.exe",
					"-instance:ShouldSuccessfullyShipLog4NetLogContainingUnicodeCharacters");

				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));

				var nowInBST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")).ToString("yyyy-MM-dd HH:mm:ss,fff");
				File.WriteAllLines(Log4NetLogFilePath, new[]{nowInBST + @" [112] DEBUG CityIndexGeneric.Logic.Utilities.LogWriter [(null)] - ContentData Id:38654759125 de-DE 'CFDs ֠eine Alternative zu Zertifikaten oder ETFs' cached until 2014-07-22 12:28:45."});

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
