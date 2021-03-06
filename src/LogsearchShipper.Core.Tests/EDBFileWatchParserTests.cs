using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using LogSearchShipper.Core.ConfigurationSections;
using Newtonsoft.Json;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	[TestFixture]
	public class EDBFileWatchParserTests
	{
		public string GenerateJsonForWatch0()
		{
			var config = ConfigurationManager.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;
			var edbFileWatchParser = new EDBFileWatchParser(config.EDBFileWatchers[0]);
			IEnumerable<EDBEnvironment> environmentHierarchy = edbFileWatchParser.GenerateLogsearchEnvironmentDiagram();
			string environmentHierarchyJSON = JsonConvert.SerializeObject(environmentHierarchy, Formatting.None);
			Console.WriteLine(environmentHierarchyJSON);
			return environmentHierarchyJSON;
		}

		[Test]
		public void JsonShouldIncludeServersWithNoServices()
		{
			StringAssert.Contains("\"Name\":\"ENV1-NO-SERVICES\"", GenerateJsonForWatch0());
		}

		[Test]
		public void ShouldGenerateConfiguredFileWatches()
		{
			var config = ConfigurationManager.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;
			var edbFileWatchParser = new EDBFileWatchParser(config.EDBFileWatchers[1]);
			var fileWatches = edbFileWatchParser.ToFileWatchCollection();

			Assert.AreEqual(6, fileWatches.Count, "edbFileWatch filters not working correctly");

			Assert.AreEqual("\\\\PKH-STG-WEB01\\Logs\\Nolio\\all.log", fileWatches[0].Files);
			Assert.AreEqual("log4j", fileWatches[0].Type);

			Assert.AreEqual("\\\\PKH-STG-WEB01\\Logs\\Nolio\\include1.log", fileWatches[1].Files);
			Assert.AreEqual("log4j1", fileWatches[1].Type);

			Assert.AreEqual("\\\\PKH-STG-PRICE01\\Logs\\Nolio\\include2.log", fileWatches[3].Files);
			Assert.AreEqual("log4j2", fileWatches[3].Type);

			var fileWatch = fileWatches[0];

			var hostField = fileWatch.Fields["host"];
			Assert.AreEqual("PKH-STG-WEB01", hostField.Value);

			var serviceField = fileWatch.Fields["service"];
			Assert.AreEqual("nolioagent2", serviceField.Value);

			var environmentField = fileWatch.Fields["environment"];
			Assert.AreEqual("ENV2", environmentField.Value);
		}

		[Test]
		public void ShouldGenerateLogsearchEnvironmentDiagramJson()
		{
			StringAssert.AreEqualIgnoringCase(
				@"[{""Name"":""ENV1"",""ServerGroups"":[{""Name"":""APP"",""Servers"":[{""Name"":""ENV1-APP01"",""Description"":""The app server"",""Tags"":null,""Domain"":""example.org"",""Environment"":""ENV1"",""NetworkArea"":""APP"",""Services"":[{""Entity"":{""@xsi:type"":""WindowsService"",""Name"":""PriceHistoryServiceHost"",""ServiceName"":""Price History Service Host"",""Tags"":""service_tag1,service_tag2"",""BinaryPath"":""\""D:\\Apps\\PriceHistoryServiceHost\\PriceHistoryServiceHost.exe\"""",""BundlePath"":null,""LogPath"":""\\\\PKH-PPE-APP10\\logs\\Apps\\PriceHistoryService\\log.log"",""LogPathType"":""log4net"",""LogPath1"":""\\\\PKH-PPE-APP10\\logs\\Apps\\PriceHistoryService\\PriceHistoryStats.log"",""LogPath1Type"":""log4net_stats"",""LogPath2"":""\\\\PKH-PPE-APP10\\"",""LogPath2Type"":null,""SystemArea"":""CORE"",""State"":""stopped""}},{""Entity"":{""@xsi:type"":""WebService"",""Name"":""ENV1-APP01 webservices"",""Website"":""ENV1-APP01 webservices"",""ApplicationUri"":""/"",""BinaryPath"":""D:\\Websites\\ROOT01"",""LogPath"":""\\\\ENV1-APP01\\Logs\\u*.log"",""LogPathType"":""IIS7"",""LogPath1"":""\\\\ENV1-APP01\\"",""LogPath1Type"":null,""LogPath2"":""\\\\ENV1-APP01\\"",""LogPath2Type"":null,""SystemArea"":""CORE"",""State"":""running""}}]},{""Name"":""ENV1-NO-SERVICES"",""Description"":""A server with no services"",""Tags"":null,""Domain"":""example.org"",""Environment"":""ENV1"",""NetworkArea"":""APP"",""Services"":[]},{""Name"":""PKH-ENV2-SHARED01"",""Description"":""A server that also appears in the ENV2 environment"",""Tags"":""server_tag1,server_tag2"",""Domain"":""cityindex.co.uk"",""Environment"":""ENV2"",""NetworkArea"":""APP"",""Services"":[{""Entity"":{""@xsi:type"":""WindowsService"",""Name"":""nolioagent2"",""ServiceName"":""Nolio Agent 2.0"",""BinaryPath"":""D:\\Nolio\\NolioAgent\\bin\\nolio_w.exe -s D:\\Nolio\\NolioAgent\\conf\\wrapper.conf"",""BundlePath"":null,""LogPath"":""\\\\PKH-ENV2-SHARED01\\Logs\\Nolio\\all.log"",""LogPathType"":""log4j"",""LogPath1"":null,""LogPath1Type"":null,""LogPath2"":null,""LogPath2Type"":null,""SystemArea"":""CORE"",""State"":""running""}}]}]},{""Name"":""DB"",""Servers"":[{""Name"":""ENV1-DB01"",""Description"":null,""Tags"":null,""Domain"":""example.org"",""Environment"":""ENV1"",""NetworkArea"":""DB"",""Services"":[{""Entity"":{""@xsi:type"":""WindowsService"",""Name"":""nolioagent2"",""ServiceName"":""Nolio Agent 2.0"",""BinaryPath"":""D:\\Nolio\\NolioAgent\\bin\\nolio_w.exe -s D:\\Nolio\\NolioAgent\\conf\\wrapper.conf"",""BundlePath"":null,""LogPath"":""\\\\ENV1-DB01\\Logs\\Nolio\\all.log"",""LogPathType"":""log4j"",""LogPath1"":""\\\\ENV1-DB01\\"",""LogPath1Type"":null,""LogPath2"":""\\\\ENV1-DB01\\"",""LogPath2Type"":null,""SystemArea"":""CORE"",""State"":""running""}}]}]}]}]",
				GenerateJsonForWatch0());
		}

		[Test]
		public void TestEdbNotMatchElements()
		{
			var fileMap = new ConfigurationFileMap(Path.GetFullPath(@"Configs\EdbNotMatch.config"));
			var configuration = ConfigurationManager.OpenMappedMachineConfiguration(fileMap);
			var config = configuration.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;

			{
				var edbFileWatchParser = new EDBFileWatchParser(config.EDBFileWatchers[0]);
				var watchElements = edbFileWatchParser.ToFileWatchCollection();
				Assert.AreEqual(1, watchElements.Count);
				Assert.IsTrue(!watchElements.First().Files.Contains("ENV1"));
				Assert.IsTrue(watchElements.First().Files.Contains("-ENV2-"));
			}
		}

		[Test]
		public void TestConfigOverrides()
		{
			var fileMap = new ConfigurationFileMap(Path.GetFullPath(@"Configs\EdbOverridesConfig.config"));
			var configuration = ConfigurationManager.OpenMappedMachineConfiguration(fileMap);
			var config = configuration.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;

			{
				var edbItem = config.EDBFileWatchers[0];
				var overrideConfig = edbItem.OverrideConfigs.First();
				var edbFileWatchParser = new EDBFileWatchParser(edbItem);
				var fileWatchItems = edbFileWatchParser.ToFileWatchCollection();
				Assert.AreEqual(5, fileWatchItems.Count);

				var matchedItems = fileWatchItems.Where(item => item.Files.Contains("Price")).ToArray();
				Assert.AreEqual(2, matchedItems.Count());
				Assert.IsTrue(matchedItems.All(item => item.CloseWhenIdle == false));
				Assert.IsTrue(matchedItems.All(item => item.CustomNxlogConfig.Value == overrideConfig.CustomNxlogConfig.Value));
				Assert.IsTrue(matchedItems.All(item => item.SourceTailer == TailerType.MT));
				Assert.IsTrue(matchedItems.All(item => item.Fields["environment"].Value == "CUSTOM_VALUE"));

				var notMatchedItems = fileWatchItems.ToList();
				foreach (var item in matchedItems)
				{
					notMatchedItems.Remove(item);
				}

				Assert.IsTrue(notMatchedItems.Count > 0);
				Assert.IsTrue(notMatchedItems.All(item => item.CloseWhenIdle == true));
				Assert.IsTrue(notMatchedItems.All(item => string.IsNullOrEmpty(item.CustomNxlogConfig.Value)));
				Assert.IsTrue(notMatchedItems.All(item => item.SourceTailer == TailerType.Normal));
				Assert.IsTrue(notMatchedItems.All(item => item.Fields["environment"].Value == "QAT"));
			}
		}
	}
}