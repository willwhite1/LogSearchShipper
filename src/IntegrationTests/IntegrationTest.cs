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
	[TestFixture]
	class IntegrationTest : IntegrationTestBase
	{
		void Init()
		{
			Init("LogsearchShipper.exe.config.Test");
		}

		[Test]
		public void TestSimpleFileWriting()
		{
			Init();

			var path = GetTestPath("TestSimpleFileWriting");
			var filePath = Path.Combine(path, "TestFile.log");

			Trace.WriteLine("Writing the file");

			string[] ids;
			File.WriteAllText(filePath, GetLog(out ids));

			GetAndValidateRecords(ids);
		}

		[Test]
		public void TestNxlogStatusLogging()
		{
			Init();

			var queryArgs = new Dictionary<string, string>
			{
				{ "@source.sessionId", CurrentGroupId },
				{ "logger", "nxlog.exe" },
			};

			GetAndValidateRecords(queryArgs, new[] { "nxlog_message" }, 1,
				records => Assert.IsTrue(records.Count > 0));
		}

		[Test]
		public void TestSlowFileWriting()
		{
			Init();

			var path = GetTestPath("TestSlowFileWriting");
			var filePath = Path.Combine(path, "TestFile.log");

			Trace.WriteLine("Writing the file");

			var ids = new List<string>();

			for (int i = 0; i < 10; i++)
			{
				string[] curIds;
				var text = GetLog(out curIds, 1);
				File.AppendAllText(filePath, text);
				ids.AddRange(curIds);

				Thread.Sleep(TimeSpan.FromSeconds(10));
			}

			GetAndValidateRecords(ids.ToArray());
		}

		[Test]
		public void TestFileRolling()
		{
			Init();

			var path = GetTestPath("TestFileRolling");

			Trace.WriteLine("Writing the files");
			var ids = WriteLogFiles(path);

			GetAndValidateRecords(ids);
		}

		[Test]
		public void TestResumingFileShipping()
		{
			Init();

			var path = GetTestPath("TestResumingFileShipping");
			var filePath = Path.Combine(path, "TestFile.log");

			var ids = new List<string>();
			string[] curIds;

			Trace.WriteLine("Writing the file");
			var text = GetLog(out curIds);
			File.WriteAllText(filePath, text);
			ids.AddRange(curIds);

			GetAndValidateRecords(curIds, 3);

			StopShipperService();

			Trace.WriteLine("    Appending to the file");
			text = GetLog(out curIds);
			File.AppendAllText(filePath, text);
			ids.AddRange(curIds);

			StartShipperService();

			Trace.WriteLine("    Appending to the file");
			text = GetLog(out curIds);
			File.AppendAllText(filePath, text);
			ids.AddRange(curIds);

			GetAndValidateRecords(ids.ToArray(), 3);
		}

		private string[] WriteLogFiles(string path)
		{
			var ids = new List<string>();

			int i = 0;
			while (i < LogFilesCount)
			{
				var filePath = Path.Combine(path, "TestFile.log");

				string[] curIds;
				File.WriteAllText(filePath, GetLog(out curIds));
				ids.AddRange(curIds);

				Thread.Sleep(TimeSpan.FromSeconds(30));

				var newName = filePath + "." + i + ".log";
				File.Move(filePath, newName);

				i++;
			}

			return ids.ToArray();
		}

		private const int LogFilesCount = 10;

		public override string TestName
		{
			get { return "LogSearchShipper.Test"; }
		}

		public override void AdjustConfig(XmlDocument config)
		{
			var nodes = config.SelectNodes("/configuration/LogSearchShipperGroup/LogSearchShipper");
			foreach (XmlElement node in nodes)
			{
				node.SetAttribute("sessionId", CurrentGroupId);
			}
		}
	}
}
