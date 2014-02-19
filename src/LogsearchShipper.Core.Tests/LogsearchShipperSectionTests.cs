using System.Configuration;
using NUnit.Framework;
using LogsearchShipper.Core.ConfigurationSections;

namespace LogsearchShipper.Core.Tests
{
    [TestFixture]
    public class LogsearchShipperSectionTests
    {
		private LogsearchShipperSection _logsearchShipper;

        [SetUp]
        public void Setup()
        {
            _logsearchShipper = ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
        }

        [Test]
        public void ShouldHaveServersAttribute()
        {
			Assert.AreEqual("ingestor.example.com:5043", _logsearchShipper.Servers);
        }
        [Test]
        public void ShouldHaveSSLCAAttribute()
        {
            StringAssert.Contains("mycert.crt", _logsearchShipper.SSL_CA);
        }
        [Test]
        public void ShouldHaveTimeoutAttribute()
        {
            Assert.AreEqual(23, _logsearchShipper.Timeout);
        }

		[Test]
		public void ShouldHaveFileWatchersArray()
		{
			Assert.GreaterOrEqual(_logsearchShipper.FileWatchers.Count, 1);
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
        public void ShouldHaveEDBFileWatchWithLogEnvironmentDiagramDataEveryMinutes()
        {
            Assert.AreEqual(42, _logsearchShipper.EDBFileWatchers[0].LogEnvironmentDiagramDataEveryMinutes);
        }

		[Test]
		public void ShouldHaveEDBFileWatchWithServerGroupNamesAttribute()
		{
			Assert.AreEqual("DMZ|APP", _logsearchShipper.EDBFileWatchers[0].NetworkAreas);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithServiceNamesAttribute()
		{
			Assert.AreEqual("nolio.*", _logsearchShipper.EDBFileWatchers[0].ServiceNames);
		}

		[Test]
		public void ShouldHaveEDBFileWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("edb_key/subkey", _logsearchShipper.EDBFileWatchers[0].Fields[0].Key);
			Assert.AreEqual("edb_value/subvalue", _logsearchShipper.EDBFileWatchers[0].Fields[0].Value);
		}

	}
}