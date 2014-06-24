using System;
using System.Collections.Generic;
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
		[Test]
		public void TestIntegration()
		{
			_basePath = Path.Combine(Environment.CurrentDirectory, "LogSearchShipper.Test.Logs");
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);
			Cleanup();

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

				var newName = filePath + "." + i;
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

		private const int MaxIterationsCount = 3;
		private const int LinesPerFile = 1000;
		private const int LogFilesCount = 10;
	}
}
