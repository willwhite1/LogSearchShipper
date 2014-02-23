using System;
using NUnit.Framework;
using System.Configuration;
using System.Collections.Generic;
using LogsearchShipper.Core.ConfigurationSections;
using Newtonsoft.Json;

namespace LogsearchShipper.Core.Tests
{
	[TestFixture]
	public class EDBFileWatchParserTests
	{

		[Test]
		public void JsonShouldIncludeServersWithNoServices()
		{
			StringAssert.Contains ("\"Name\":\"ENV1-NO-SERVICES\"", GenerateJsonForWatch0());
		}

		[Test]
		public void ShouldGenerateLogsearchEnvironmentDiagramJson()
		{
			StringAssert.AreEqualIgnoringCase ("[{\"Name\":\"ENV1\",\"ServerGroups\":[{\"Name\":\"APP\",\"Servers\":[{\"Name\":\"ENV1-APP01\",\"Description\":\"The app server\",\"Domain\":\"example.org\",\"Environment\":\"ENV1\",\"NetworkArea\":\"APP\",\"Services\":[{\"Entity\":{\"@xsi:type\":\"WindowsService\",\"Name\":\"PriceHistoryServiceHost\",\"ServiceName\":\"Price History Service Host\",\"BinaryPath\":\"\\\"D:\\\\Apps\\\\PriceHistoryServiceHost\\\\PriceHistoryServiceHost.exe\\\"\",\"BundlePath\":null,\"LogPath\":\"\\\\\\\\PKH-PPE-APP10\\\\logs\\\\Apps\\\\PriceHistoryService\\\\log.log\",\"LogPathType\":\"log4net\",\"LogPath1\":\"\\\\\\\\PKH-PPE-APP10\\\\logs\\\\Apps\\\\PriceHistoryService\\\\PriceHistoryStats.log\",\"LogPath1Type\":\"log4net_stats\",\"LogPath2\":\"\\\\\\\\PKH-PPE-APP10\\\\\",\"LogPath2Type\":null,\"SystemArea\":\"CORE\",\"State\":\"stopped\"}},{\"Entity\":{\"@xsi:type\":\"WebService\",\"Name\":\"ENV1-APP01 webservices\",\"Website\":\"ENV1-APP01 webservices\",\"ApplicationUri\":\"/\",\"BinaryPath\":\"D:\\\\Websites\\\\ROOT01\",\"LogPath\":\"\\\\\\\\ENV1-APP01\\\\Logs\\\\u*.log\",\"LogPathType\":\"IIS7\",\"LogPath1\":\"\\\\\\\\ENV1-APP01\\\\\",\"LogPath1Type\":null,\"LogPath2\":\"\\\\\\\\ENV1-APP01\\\\\",\"LogPath2Type\":null,\"SystemArea\":\"CORE\",\"State\":\"running\"}}]},{\"Name\":\"ENV1-NO-SERVICES\",\"Description\":\"A server with no services\",\"Domain\":\"example.org\",\"Environment\":\"ENV1\",\"NetworkArea\":\"APP\",\"Services\":[]}]},{\"Name\":\"DB\",\"Servers\":[{\"Name\":\"ENV1-DB01\",\"Description\":null,\"Domain\":\"example.org\",\"Environment\":\"ENV1\",\"NetworkArea\":\"DB\",\"Services\":[{\"Entity\":{\"@xsi:type\":\"WindowsService\",\"Name\":\"nolioagent2\",\"ServiceName\":\"Nolio Agent 2.0\",\"BinaryPath\":\"D:\\\\Nolio\\\\NolioAgent\\\\bin\\\\nolio_w.exe -s D:\\\\Nolio\\\\NolioAgent\\\\conf\\\\wrapper.conf\",\"BundlePath\":null,\"LogPath\":\"\\\\\\\\ENV1-DB01\\\\Logs\\\\Nolio\\\\all.log\",\"LogPathType\":\"log4j\",\"LogPath1\":\"\\\\\\\\ENV1-DB01\\\\\",\"LogPath1Type\":null,\"LogPath2\":\"\\\\\\\\ENV1-DB01\\\\\",\"LogPath2Type\":null,\"SystemArea\":\"CORE\",\"State\":\"running\"}}]}]}]}]",
				GenerateJsonForWatch0());
		}

		public string GenerateJsonForWatch0()
		{
			var config = ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
			var edbFileWatchParser = new EDBFileWatchParser (config.EDBFileWatchers [0]);   
			var environmentHierarchy = edbFileWatchParser.GenerateLogsearchEnvironmentDiagram ();
			var environmentHierarchyJSON = JsonConvert.SerializeObject(environmentHierarchy, Newtonsoft.Json.Formatting.None);   
			Console.WriteLine (environmentHierarchyJSON);   
			return environmentHierarchyJSON;  
		}

		[Test]
		public void ShouldGenerateConfiguredFileWatches()
		{
			var config = ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
			var edbFileWatchParser = new EDBFileWatchParser (config.EDBFileWatchers [1]); 
			var fileWatches = edbFileWatchParser.ToFileWatchCollection ();

			Assert.AreEqual (4, fileWatches.Count, "edbFileWatch filters not working correctly");

			Assert.AreEqual ("\\\\PKH-STG-WEB01\\Logs\\Nolio\\all.log", fileWatches [0].Files);
			Assert.AreEqual ("log4j", fileWatches [0].Type);
			Assert.AreEqual ("@service.name", fileWatches [0].Fields[0].Key);
			Assert.AreEqual ("nolioagent2", fileWatches [0].Fields[0].Value);
			Assert.AreEqual ("@environment", fileWatches [0].Fields[1].Key);
			Assert.AreEqual ("ENV2", fileWatches [0].Fields[1].Value);
		}
	}
}

