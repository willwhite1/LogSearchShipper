using System.Configuration;
using LogsearchShipper.Core.ConfigurationSections;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
	[TestFixture]
	public class LogsearchShipperSectionTests
	{
		[SetUp]
		public void Setup()
		{
			_logsearchShipper =
				ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
		}

		private LogsearchShipperSection _logsearchShipper;

		[Test]
		public void ShouldHaveEDBFileWatchArray()
		{
			Assert.GreaterOrEqual(_logsearchShipper.EDBFileWatchers.Count, 1);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithDataFileAttribute()
		{
			Assert.AreEqual(@"SampleData\EDB\ENV1\Latest.xml", _logsearchShipper.EDBFileWatchers[0].DataFile);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("edb_key/subkey", _logsearchShipper.EDBFileWatchers[0].Fields[0].Key);
			Assert.AreEqual("edb_value/subvalue", _logsearchShipper.EDBFileWatchers[0].Fields[0].Value);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithLogEnvironmentDiagramDataEveryMinutes()
		{
			Assert.AreEqual(42, _logsearchShipper.EDBFileWatchers[0].LogEnvironmentDiagramDataEveryMinutes);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithNetworkAreasAttribute()
		{
			Assert.AreEqual("DMZ|APP", _logsearchShipper.EDBFileWatchers[1].NetworkAreas);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithServerNamesAttribute()
		{
			Assert.AreEqual("(.*01|.*02)", _logsearchShipper.EDBFileWatchers[1].ServerNames);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithServiceNamesAttribute()
		{
			Assert.AreEqual("nolio.*", _logsearchShipper.EDBFileWatchers[1].ServiceNames);
		}

		[Test]
		public void ShouldHaveFileWatchWithFieldsArray()
		{
			Assert.GreaterOrEqual(2, _logsearchShipper.FileWatchers[0].Fields.Count);
		}

		[Test]
		public void ShouldHaveFileWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("field1", _logsearchShipper.FileWatchers[0].Fields[0].Key);
			Assert.AreEqual("field1 value", _logsearchShipper.FileWatchers[0].Fields[0].Value);
		}

		[Test]
		public void ShouldHaveFileWatchWithFilesAttribute()
		{
			Assert.AreEqual("myfile.log", _logsearchShipper.FileWatchers[0].Files);
		}

		[Test]
		public void ShouldHaveFileWatchWithTypeAttribute()
		{
			Assert.AreEqual("myfile_type", _logsearchShipper.FileWatchers[0].Type);
		}

		[Test]
		public void ShouldHaveFileWatchersArray()
		{
			Assert.GreaterOrEqual(_logsearchShipper.FileWatchers.Count, 1);
		}

		[Test]
		public void ShouldHaveDataFolder()
		{
				StringAssert.Contains("data", _logsearchShipper.DataFolder);
		}

		[Test]
		public void ShouldHaveIngestorHostAttribute()
		{
			Assert.AreEqual("ingestor.example.com", _logsearchShipper.IngestorHost);
		}

		[Test]
		public void ShouldHaveIngestorPortAttribute()
		{
			Assert.AreEqual(443, _logsearchShipper.IngestorPort);
		}

		[Test]
		public void ShouldHaveSSLCAAttribute()
		{
			StringAssert.Contains("mycert.crt", _logsearchShipper.SSL_CA);
		}
	}
}