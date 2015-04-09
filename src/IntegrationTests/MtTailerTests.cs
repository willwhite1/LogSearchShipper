using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
			var ids = new List<string>();
			var position = 0L;

			FillWithZeros(filePath);

			// these records must be discarded due to readFromLast=true
			{
				string[] tmpIds;
				var log = GetLog(out tmpIds, 10);

				AppendToLog(filePath, ref position, log);
			}

			StartShipperService();

			for (var i = 0; i < 5; i++)
			{
				Thread.Sleep(TimeSpan.FromSeconds(3));

				var log = GetLog(ids);
				AppendToLog(filePath, ref position, log);
			}

			GetAndValidateRecords(ids.ToArray());
		}

		private static void FillWithZeros(string filePath)
		{
			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				// pre-allocate file and fill with zeros, the same as MT does
				for (int i = 0; i < 100000; i++)
				{
					stream.WriteByte(0);
				}
			}
		}

		private static void AppendToLog(string filePath, ref long position, string text)
		{
			using (var stream = new FileStream(filePath, FileMode.Open))
			{
				stream.Position = position;

				using (var writer = new StreamWriter(stream, Encoding.UTF8))
				{
					writer.Write(text);
					writer.Flush();
					position = stream.Position;
				}
			}
		}
	}
}
