using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	class EdbLoggingTest : IntegrationTestBase
	{
		void Init()
		{
			Init("LogsearchShipper.exe.config.EdbLoggingTest");
		}

		[Test]
		public void TestEdbLogging()
		{
			Init();

			GetAndValidateRecords(
				records =>
				{
					var filtered = records.Where(record => record.Fields.ContainsKey("Message")).ToList();

					if (filtered.Count == 0)
						return false;

					return true;
				});
		}

		public override string TestName
		{
			get { return "LogSearchShipper.EdbLoggingTest"; }
		}

		public override void AdjustConfig(XmlDocument config)
		{
			var nodes = config.SelectNodes("/configuration/LogSearchShipperGroup/LogSearchShipper/fileWatchers/watch");
			foreach (XmlElement node in nodes)
			{
				var groupSpec = config.CreateElement("field");
				groupSpec.SetAttribute("key", "currentGroupId");
				groupSpec.SetAttribute("value", CurrentGroupId);
				node.AppendChild(groupSpec);
			}
		}
	}
}
