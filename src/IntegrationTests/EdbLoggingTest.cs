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
			Init("LogsearchShipper.exe.config.EdbLoggingTest");
		}

		[Test]
		public void TestEdbLogging()
		{
			Init();
		}

		public override string TestName
		{
			get { return "LogSearchShipper.EdbLoggingTest"; }
		}
	}
}
