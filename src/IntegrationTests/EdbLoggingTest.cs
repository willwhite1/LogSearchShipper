using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	class EdbLoggingTest : IntegrationTestBase
	{
		void Init()
		{
			InitAndStart("LogsearchShipper.exe.config.EdbLoggingTest");
		}

		[Test]
		public void TestEdbLogging()
		{
			Init();

			var queryArgs = new Dictionary<string, string>
			{
				{ "@source.environment", TestName },
				{ "@source.groupId", CurrentGroupId },
				{ "@source.path", "EDB_expected_event_sources.log" }
			};

			var expectedLines = File.ReadAllText(@"Expected\EdbLoggingTest.txt").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var expected = new HashSet<string>(expectedLines);

			GetAndValidateRecords(queryArgs, new [] { "@message" }, expected.Count(),
				records =>
				{
					foreach (var record in records)
					{
						var message = (JContainer)JsonConvert.DeserializeObject(record.Fields["@message"]);
						message.Children().First().Remove();
						var lineText = JsonConvert.SerializeObject(message);
						Assert.IsTrue(expected.Contains(lineText));
					}
				}, 3);
		}

		public override string TestName
		{
			get { return "LogSearchShipper.EdbLoggingTest"; }
		}
	}
}
