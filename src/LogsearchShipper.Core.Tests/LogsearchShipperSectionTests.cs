using System.Configuration;
using LogSearchShipper.Core.ConfigurationSections;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	[TestFixture]
	public class LogSearchShipperSectionTests
	{
		[SetUp]
		public void Setup()
		{
			_LogSearchShipper =
				ConfigurationManager.GetSection("LogSearchShipperGroup/LogSearchShipper") as LogSearchShipperSection;
		}

		private LogSearchShipperSection _LogSearchShipper;

		[Test]
		public void ShouldHaveDataFolder()
		{
			StringAssert.Contains("data", _LogSearchShipper.DataFolder);
		}

		[Test]
		public void ShouldHaveEDBFileWatchArray()
		{
			Assert.GreaterOrEqual(_LogSearchShipper.EDBFileWatchers.Count, 1);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithDataFileAttribute()
		{
			Assert.AreEqual(@"SampleData\EDB\ENV1\Latest.xml", _LogSearchShipper.EDBFileWatchers[0].DataFile);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("edb_key/subkey", _LogSearchShipper.EDBFileWatchers[0].Fields[0].Key);
			Assert.AreEqual("edb_value/subvalue", _LogSearchShipper.EDBFileWatchers[0].Fields[0].Value);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithLogEnvironmentDiagramDataEveryMinutes()
		{
			Assert.AreEqual(42, _LogSearchShipper.EDBFileWatchers[0].LogEnvironmentDiagramDataEveryMinutes);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithNetworkAreasAttribute()
		{
			Assert.AreEqual("DMZ|APP", _LogSearchShipper.EDBFileWatchers[1].NetworkAreas);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithServerNamesAttribute()
		{
			Assert.AreEqual("(.*01|.*02)", _LogSearchShipper.EDBFileWatchers[1].ServerNames);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithServiceNamesAttribute()
		{
			Assert.AreEqual("nolio.*", _LogSearchShipper.EDBFileWatchers[1].ServiceNames);
		}

		[Test]
		public void ShouldHaveFileWatchWithFieldsArray()
		{
			Assert.GreaterOrEqual(2, _LogSearchShipper.FileWatchers[0].Fields.Count);
		}

		[Test]
		public void ShouldHaveFileWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("field1", _LogSearchShipper.FileWatchers[0].Fields[0].Key);
			Assert.AreEqual("field1 value", _LogSearchShipper.FileWatchers[0].Fields[0].Value);
		}

		[Test]
		public void ShouldHaveFileWatchWithFilesAttribute()
		{
			Assert.AreEqual("myfile.log", _LogSearchShipper.FileWatchers[0].Files);
		}

		[Test]
		public void ShouldHaveFileWatchWithTypeAttribute()
		{
			Assert.AreEqual("myfile_type", _LogSearchShipper.FileWatchers[0].Type);
		}

		[Test]
		public void ShouldHaveFileWatchersArray()
		{
			Assert.GreaterOrEqual(_LogSearchShipper.FileWatchers.Count, 1);
		}

		[Test]
		public void ShouldHaveIngestorHostAttribute()
		{
			Assert.AreEqual("ingestor.example.com", _LogSearchShipper.IngestorHost);
		}

		[Test]
		public void ShouldHaveIngestorPortAttribute()
		{
			Assert.AreEqual(443, _LogSearchShipper.IngestorPort);
		}
	}
}