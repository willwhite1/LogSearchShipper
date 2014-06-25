using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace IntegrationTests
{
	[TestFixture]
	class IntegrationTest
	{
		[TestFixtureTearDown]
		public void TearDown()
		{
			StopShipperService();
		}

		[Test]
		public void TestIntegration()
		{
			_basePath = Path.Combine(Environment.CurrentDirectory, "LogSearchShipper.Test");
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);
			Cleanup();

			StartShipperService();

			while (_currentIteration < MaxIterationsCount)
				RunTestIteration();
		}

		private void Cleanup()
		{
			Utils.Cleanup(_basePath);
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

			WriteLogFiles(path);
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
					"{{\"@timestamp\":\"{0}\",\"message\":\"{1}\",\"group_id\":\"{2}\",\"@source.name\":\"LogSearchShipper.Test\"," +
					"\"logger\":\"Test\",\"level\":\"INFO\"}}",
					DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), id, _currentIterationId);
				buf.AppendLine(message);
				tmp.Add(id);
				i++;
			}
			ids = tmp.ToArray();
			return buf.ToString();
		}

		void StartShipperService()
		{
			var exeFile = "LogsearchShipper.Service.exe";
			var exeFileCopy = Path.Combine(_basePath, exeFile);
			File.Copy(exeFile, exeFileCopy);

			File.Copy("LogsearchShipper.Service.exe.config.Test", Path.Combine(_basePath, "LogsearchShipper.Service.exe.config"));

			foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.dll"))
			{
				var newFile = Path.Combine(_basePath, Path.GetFileName(file));
				File.Copy(file, newFile);
			}

			_shipperProcess = Utils.StartProcess(exeFileCopy, "-instance:integrationtest002");
		}

		void StopShipperService()
		{
			Utils.ShutdownProcess(_shipperProcess);
			_shipperProcess = null;
		}

		string GetTestPath(string testName)
		{
			var res = Path.Combine(_basePath, _currentIteration.ToString(), testName);
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
			return res;
		}

		private int _currentIteration;
		private string _currentIterationId;

		private string _basePath;

		private Process _shipperProcess;

		private const int MaxIterationsCount = 3;
		private const int LinesPerFile = 1000;
		private const int LogFilesCount = 10;
	}
}
