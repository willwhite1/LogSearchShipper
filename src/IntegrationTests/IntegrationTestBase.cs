using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using LogSearchShipper.Core;

using NUnit.Framework;

namespace IntegrationTests
{
	abstract class IntegrationTestBase
	{
		protected void Init(string configName)
		{
			_currentGroupId = Guid.NewGuid().ToString();

			if (_initDone)
				return;

			_basePath = Path.Combine(Environment.CurrentDirectory, TestName);
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);

			Utils.Cleanup(_basePath);
			Directory.CreateDirectory(LogsPath);

			foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.exe"))
			{
				var newFile = Path.Combine(_basePath, Path.GetFileName(file));
				File.Copy(file, newFile);
			}

			var exeFile = "LogsearchShipper.exe";
			var exeFileCopy = Path.Combine(_basePath, exeFile);

			var configPath = Path.Combine(_basePath, "LogsearchShipper.exe.config");
			var config = new XmlDocument();
			config.Load(configName);
			AdjustConfig(config);
			config.Save(configPath);

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

		public virtual void AdjustConfig(XmlDocument config)
		{
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			StopShipperService();
		}

		protected void StartShipperService()
		{
			Trace.WriteLine("    Starting the shipper service");

			_shipperProcess = ProcessUtils.StartProcess(_exePath, "-instance:OverallTest");

			Trace.WriteLine("    Waiting 30 seconds for shipper to startup...");
			Thread.Sleep(TimeSpan.FromSeconds(30));
		}

		protected void StopShipperService()
		{
			Utils.WriteDelimiter();
			Trace.WriteLine("    Stopping the shipper service");

			Utils.ShutdownProcess(_shipperProcess);
			_shipperProcess = null;
		}

		protected string GetTestPath(string testName)
		{
			var res = Path.Combine(LogsPath, testName);
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
			return res;
		}

		protected void GetAndValidateRecords(Dictionary<string, string> queryArgs, string[] requiredFields, int expectedCount,
			Action<List<Record>> validate, int waitMinutes = 10)
		{
			Trace.WriteLine("Getting records from ES...");

			var startTime = DateTime.UtcNow;

			while (true)
			{
				Thread.Sleep(TimeSpan.FromMinutes(1));
				var records = EsUtil.GetRecords(queryArgs);
				Trace.WriteLine(string.Format("    Checking ES records: {0} total found", records.Count));
				var filtered = records.Where(record => requiredFields.Any(requiredField => record.Fields.ContainsKey(requiredField))).ToList();

				if (filtered.Count >= expectedCount || DateTime.UtcNow - startTime > TimeSpan.FromMinutes(waitMinutes))
				{
					validate(filtered);
					break;
				}
			}

			Trace.WriteLine("Validation is successful");
		}

		protected string GetLog(out string[] ids, int linesCount = LinesPerFile)
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
					DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), id, CurrentGroupId);
				buf.AppendLine(message);
				tmp.Add(id);
				i++;
			}
			ids = tmp.ToArray();
			return buf.ToString();
		}

		protected void Validate(ICollection<Record> records, IEnumerable<string> ids)
		{
			var recordIdsCount = new Dictionary<string, int>();
			foreach (var record in records)
			{
				var id = record.Fields["message"];

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

		protected void GetAndValidateRecords(string[] ids, int waitMinutes = WaitResultPeriodMinutes)
		{
			var queryArgs = new Dictionary<string, string>
			{
				{ "source", TestName },
				{ "group_id", CurrentGroupId },
			};

			GetAndValidateRecords(queryArgs, new[] { "message" }, ids.Count(),
				records => Validate(records, ids), waitMinutes);
		}

		string LogsPath { get { return Path.Combine(_basePath, "logs"); } }

		private string _basePath;
		private string _exePath;

		private Process _shipperProcess;

		private bool _initDone;

		protected string CurrentGroupId
		{
			get { return _currentGroupId; }
		}

		private string _currentGroupId;

		public abstract string TestName { get; }

		private const int LinesPerFile = 100;
		private const int WaitResultPeriodMinutes = 10;
	}
}
