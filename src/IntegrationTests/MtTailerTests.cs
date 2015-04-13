using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

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
		public void Test_ReadFromLastTrue()
		{
			_readFromLast = true;
			var encoding = Encoding.UTF8;

			Init();

			var path = GetTestPath("TestMtLogImitation");
			var filePath = Path.Combine(path, "TestFile.log");

			var ids = new List<string>();
			var position = 0L;

			Trace.WriteLine("Writing the file");

			FillWithZeros(filePath);

			// these records must be discarded due to readFromLast=true
			{
				string[] tmpIds;
				var log = GetLog(out tmpIds, 10);
				AppendToLog(filePath, ref position, log, encoding);
			}

			StartShipperService();

			for (var i = 0; i < 5; i++)
			{
				Thread.Sleep(TimeSpan.FromSeconds(3));

				var log = GetLog(ids);
				AppendToLog(filePath, ref position, log, encoding);
			}

			GetAndValidateRecords(ids.ToArray());

			StopShipperService();
		}

		[Test]
		public void Test_ReadFromLastFalse_Bom()
		{
			Test_ReadFromLastFalse(Encoding.UTF8);
		}

		[Test]
		public void Test_ReadFromLastFalse_NoBom()
		{
			Test_ReadFromLastFalse(null);
		}

		void Test_ReadFromLastFalse(Encoding encoding)
		{
			_readFromLast = false;
			Init();

			var path = GetTestPath("TestMtLogImitation");
			var filePath = Path.Combine(path, "TestFile.log");

			var ids = new List<string>();
			var position = 0L;

			Trace.WriteLine("Writing the file");

			FillWithZeros(filePath);

			{
				var log = GetLog(ids, 10);
				AppendToLog(filePath, ref position, log, encoding);
			}

			StartShipperService();

			for (var i = 0; i < 5; i++)
			{
				Thread.Sleep(TimeSpan.FromSeconds(3));

				var log = GetLog(ids);
				AppendToLog(filePath, ref position, log, encoding);
			}

			GetAndValidateRecords(ids.ToArray());

			StopShipperService();
		}

		[Test]
		public void TestConcurrentAccess()
		{
			Init();

			var path = GetTestPath("TestMtLogImitation");
			var filePath = Path.Combine(path, "TestFile.log");

			var ids = new List<string>();
			var position = 0L;

			Trace.WriteLine("Writing the file");

			FillWithZeros(filePath);

			StartShipperService();

			for (var i = 0; i < 5; i++)
			{
				var line = GetLog(ids, 1);
				var splitIndex = line.Length / 2;

				AppendToLog(filePath, ref position, line.Substring(0, splitIndex), null);
				Thread.Sleep(TimeSpan.FromSeconds(2));
				AppendToLog(filePath, ref position, line.Substring(splitIndex), null);
			}

			GetAndValidateRecords(ids.ToArray());
			StopShipperService();
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

		private static void AppendToLog(string filePath, ref long position, string text, Encoding encoding)
		{
			using (var writer = CreateWriter(filePath, encoding, position))
			{
				writer.Write(text);
				writer.Flush();
				position = writer.BaseStream.Position;
			}
		}

		static StreamWriter CreateWriter(string filePath, Encoding encoding, long position)
		{
			var stream = new FileStream(filePath, FileMode.Open) { Position = position };
			var writer = (encoding != null)
				? new StreamWriter(stream, encoding)
				: new StreamWriter(stream); // UTF-8 without BOM
			return writer;
		}

		private bool _readFromLast = true;

		public override void AdjustConfig(XmlDocument config)
		{
			var nodes = config.SelectNodes("/configuration/LogSearchShipperGroup/LogSearchShipper/fileWatchers/watch");
			foreach (XmlElement node in nodes)
			{
				node.SetAttribute("readFromLast", _readFromLast.ToString());
			}
		}
	}
}
