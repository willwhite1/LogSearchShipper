using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace IntegrationTests
{
	class HeaderlineTests : IntegrationTestBase
	{
		void Init()
		{
			InitAndStart("LogsearchShipper.exe.config.HeaderlineTest");
		}

		public override string TestName
		{
			get { return "LogSearchShipper.Test.Headerline"; }
		}

		[Test]
		public void TestLog4netHeaderlineRule()
		{
			Init();

			var queryArgs = new Dictionary<string, string>
			{
				{ "@source.groupId", CurrentGroupId },
			};

			GetAndValidateRecords(queryArgs, new[] { "message", "@message" }, 4,
				records =>
				{
					var testExpected = File.ReadAllText(@"Expected\Log4netTest.txt").Replace("\r\n", "  ");
					Assert.IsTrue(records.Any(record => record.Fields["@message"] == testExpected));
				});
		}
	}
}
