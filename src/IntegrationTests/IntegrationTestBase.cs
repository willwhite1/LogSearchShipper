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
	class IntegrationTestBase
	{
		[TestFixtureTearDown]
		public void TearDown()
		{
			StopShipperService();
		}

		protected void InitCommon()
		{
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

		string LogsPath { get { return Path.Combine(_basePath, "logs"); } }

		private string _basePath;
		private string _exePath;

		private Process _shipperProcess;

		private bool _initDone;
	}
}
