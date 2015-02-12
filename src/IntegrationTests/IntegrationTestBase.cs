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

			var exeFile = "LogsearchShipper.exe";
			var exeFileCopy = Path.Combine(_basePath, exeFile);
			File.Copy(exeFile, exeFileCopy);

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

		protected void GetAndValidateRecords(string[] requiredFields, int expectedCount,
			Action<List<Record>> validate, int waitMinutes = 10)
		{
			var queryArgs = new Dictionary<string, string>
			{
				{ "@source.environment", TestName },
				{ "@source.currentGroupId", CurrentGroupId },
			};
			GetAndValidateRecords(queryArgs, requiredFields, expectedCount, validate, waitMinutes);
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
	}
}
