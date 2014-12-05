using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

			File.Copy(configName, Path.Combine(_basePath, "LogsearchShipper.exe.config"));

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

		protected void GetAndValidateRecords(Func<List<Record>, bool> validate, int waitMinutes = 10)
		{
			Trace.WriteLine("Getting records from ES...");

			var startTime = DateTime.UtcNow;

			while (true)
			{
				Thread.Sleep(TimeSpan.FromMinutes(1));
				var records = EsUtil.GetRecords(TestName, CurrentGroupId, "message");

				var result = validate(records);
				if (result)
					break;
				if (DateTime.UtcNow - startTime > TimeSpan.FromMinutes(waitMinutes))
					throw new ApplicationException("Can't retrieve data from ES - timed out");
			}

			Trace.WriteLine("Validation is successful");
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
