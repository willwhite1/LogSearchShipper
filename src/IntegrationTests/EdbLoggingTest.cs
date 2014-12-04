using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	class EdbLoggingTest : IntegrationTestBase
	{
		void Init()
		{
			Init("LogSearchShipper.EdbLoggingTest", "LogsearchShipper.exe.config.EdbLoggingTest");
		}

		[Test]
		public void TestEdbLogging()
		{
			Init();
		}
	}
}
