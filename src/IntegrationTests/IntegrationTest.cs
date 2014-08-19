﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	class IntegrationTest
	{
		void Init()
		{
			_currentGroupId = Guid.NewGuid().ToString();

			if (_initDone)
				return;

			_basePath = Path.Combine(Environment.CurrentDirectory, "LogSearchShipper.Test");
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);

			Utils.Cleanup(_basePath);
			Directory.CreateDirectory(LogsPath);

			var exeFile = "LogsearchShipper.exe";
			var exeFileCopy = Path.Combine(_basePath, exeFile);
			File.Copy(exeFile, exeFileCopy);

			File.Copy("LogsearchShipper.exe.config.Test", Path.Combine(_basePath, "LogsearchShipper.exe.config"));

			foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.dll"))
			{
				var newFile = Path.Combine(_basePath, Path.GetFileName(file));
				File.Copy(file, newFile);
			}
			_exePath = exeFileCopy;

			StartShipperService();

			_initDone = true;

			Utils.WriteDelimiter();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			StopShipperService();
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

				Thread.Sleep(TimeSpan.FromSeconds(15));

				var newName = filePath + "." + i + ".log";
				File.Move(filePath, newName);

				i++;
			}

			return ids.ToArray();
		}

		private string GetLog(out string[] ids, int linesCount = LinesPerFile)
		{
			var tmp = new List<string>();
			var buf = new StringBuilder();
			var i = 0;
			while (i < linesCount)
			{
				var id = Guid.NewGuid().ToString();
				var message = string.Format(
					"{{\"timestamp\":\"{0}\",\"message\":\"{1}\",\"group_id\":\"{2}\",\"source\":\"LogSearchShipper.Test\"," +
					"\"logger\":\"Test\",\"level\":\"INFO\"}}",
					DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), id, _currentGroupId);
				buf.AppendLine(message);
				tmp.Add(id);
				i++;
			}
			ids = tmp.ToArray();
			return buf.ToString();
		}

		void Validate(ICollection<Record> records, IEnumerable<string> ids)
		{
			var recordIdsCount = new Dictionary<string, int>();
			foreach (var record in records)
			{
				var id = (string)record.Value;

				int count;
				if (!recordIdsCount.TryGetValue(id, out count))
					recordIdsCount.Add(id, 1);
				else
					recordIdsCount[id] = ++count;
			}

			int duplicatesCount = 0, missingCount = 0;

			foreach (var id in ids)
			{
				int count;
				if (!recordIdsCount.TryGetValue(id, out count))
					missingCount++;
				else if (count > 1)
					duplicatesCount++;
			}

			var message = string.Format("total - {0}, missing - {1}, duplicates - {2}", records.Count, missingCount, duplicatesCount);
			Trace.WriteLine(message);

			if (missingCount != 0)
				throw new ApplicationException("Validation failed - some records are missing");

			if (duplicatesCount != 0)
				Trace.WriteLine("--- Validation warning - there are some duplicate records");
		}

		void GetAndValidateRecords(string[] ids, int waitMinutes = 10)
		{
			Trace.WriteLine("Getting records from the server...");

			var startTime = DateTime.UtcNow;

			while (true)
			{
				Thread.Sleep(TimeSpan.FromMinutes(1));
				var records = EsUtil.GetRecords("LogSearchShipper.Test", _currentGroupId, "message");
				if (records.Count >= ids.Count() || DateTime.UtcNow - startTime > TimeSpan.FromMinutes(waitMinutes))
				{
					Trace.WriteLine("Validating retrieved records");
					Validate(records, ids);
					break;
				}
			}

			Trace.WriteLine("Success");
		}

		void StartShipperService()
		{
			Trace.WriteLine("    Starting the shipper service");

			_shipperProcess = Utils.StartProcess(_exePath, "-instance:OverallTest");

			Trace.WriteLine("    Waiting 30 seconds for shipper to startup...");
			Thread.Sleep(TimeSpan.FromSeconds(30));
		}

		void StopShipperService()
		{
			Utils.WriteDelimiter();
			Trace.WriteLine("    Stopping the shipper service");

			Utils.ShutdownProcess(_shipperProcess);
			_shipperProcess = null;
		}

		string GetTestPath(string testName)
		{
			var res = Path.Combine(LogsPath, testName);
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
			return res;
		}

		string LogsPath { get { return Path.Combine(_basePath, "logs"); } }

		private string _currentGroupId;

		private string _basePath;

		private string _exePath;
		private Process _shipperProcess;

		private const int LinesPerFile = 100;
		private const int LogFilesCount = 10;

		private bool _initDone;
	}
}
