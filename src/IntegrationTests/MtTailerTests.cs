using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace IntegrationTests
{
	class MtTailerTests : IntegrationTestBase
	{
		void Init()
		{
			Init("LogsearchShipper.exe.config.MtTailerTest");
		}

		public override string TestName
		{
			get { return "LogSearchShipper.Test.MtTailer"; }
		}

		[Test]
		public void TestMtLogImitation()
		{
			Init();

			var path = GetTestPath("TestMtLogImitation");
			var filePath = Path.Combine(path, "TestFile.log");

			Trace.WriteLine("Writing the file");
			string[] ids;

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				// pre-allocate file and fill with zeros, the same as MT does
				for (int i = 0; i < 100000; i++)
				{
					stream.WriteByte(0);
				}

				stream.Seek(0, SeekOrigin.Begin);

				var log = GetLog(out ids);
				using (var writer = new StreamWriter(stream, Encoding.UTF8))
				{
					writer.Write(log);
				}
			}

			GetAndValidateRecords(ids);
		}
	}
}
