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
		[TestFixtureSetUp]
		public void Init()
		{
			_basePath = Path.Combine(Environment.CurrentDirectory, "LogSearchShipper.Test");
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);

			Utils.Cleanup(_basePath);
			Directory.CreateDirectory(LogsPath);

			StartShipperService();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			StopShipperService();
		}

		[Test]
		public void TestIntegration()
		{
			while (_currentIteration < MaxIterationsCount)
				RunTestIteration();
		}

		void RunTestIteration()
		{
			_currentIterationId = Guid.NewGuid().ToString();

			SimpleTest();

			_currentIteration++;
		}

		void SimpleTest()
		{
			var path = GetTestPath("SimpleTest");

			var ids = WriteLogFiles(path);

			var startTime = DateTime.UtcNow;

			while (true)
			{
				Thread.Sleep(TimeSpan.FromMinutes(1));
				var records = EsUtil.GetRecords("LogSearchShipper.Test", _currentIterationId, "message");
				if (records.Count >= ids.Count() || DateTime.UtcNow - startTime > TimeSpan.FromMinutes(10))
				{
					Validate(records, ids);
					break;
				}
			}

			Trace.WriteLine("======================= Success =======================");
		}

		private string[] WriteLogFiles(string path)
		{
			var ids = new List<string>();

			int i = 0;
			while (i < LogFilesCount)
			{
				var filePath = Path.Combine(path, "TestFile");

				string[] curIds;
				File.WriteAllText(filePath, GetLog(out curIds));
				ids.AddRange(curIds);

				Thread.Sleep(TimeSpan.FromSeconds(10));

				var newName = filePath + "." + i + ".log";
				File.Move(filePath, newName);

				i++;
			}

			return ids.ToArray();
		}

		private string GetLog(out string[] ids)
		{
			var tmp = new List<string>();
			var buf = new StringBuilder();
			var i = 0;
			while (i < LinesPerFile)
			{
				var id = Guid.NewGuid().ToString();
				var message = string.Format(
					"{{\"timestamp\":\"{0}\",\"message\":\"{1}\",\"group_id\":\"{2}\",\"source\":\"LogSearchShipper.Test\"," +
					"\"logger\":\"Test\",\"level\":\"INFO\"}}",
					DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), id, _currentIterationId);
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

			if (missingCount != 0 || duplicatesCount != 0)
			{
				var message = string.Format("total - {0}, missing - {1}, duplicates - {2}", records.Count, missingCount, duplicatesCount);
				throw new ApplicationException(message);
			}
		}

		void StartShipperService()
		{
			var exeFile = "LogsearchShipper.exe";
			var exeFileCopy = Path.Combine(_basePath, exeFile);
			File.Copy(exeFile, exeFileCopy);

			File.Copy("LogsearchShipper.exe.config.Test", Path.Combine(_basePath, "LogsearchShipper.exe.config"));

			foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.dll"))
			{
				var newFile = Path.Combine(_basePath, Path.GetFileName(file));
				File.Copy(file, newFile);
			}

			_shipperProcess = Utils.StartProcess(exeFileCopy, "-instance:OverallTest");

			Console.WriteLine("Waiting 30 seconds for shipper to startup...");
			Thread.Sleep(TimeSpan.FromSeconds(30));
		}

		void StopShipperService()
		{
			Utils.ShutdownProcess(_shipperProcess);
			_shipperProcess = null;
		}

		string GetTestPath(string testName)
		{
			var res = Path.Combine(LogsPath, _currentIteration.ToString(), testName);
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
			return res;
		}

		string LogsPath { get { return Path.Combine(_basePath, "logs"); } }

		private int _currentIteration;
		private string _currentIterationId;

		private string _basePath;

		private Process _shipperProcess;

		private const int MaxIterationsCount = 3;
		private const int LinesPerFile = 100;
		private const int LogFilesCount = 10;
	}
}
