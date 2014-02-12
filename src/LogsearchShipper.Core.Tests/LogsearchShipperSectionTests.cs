using System.Configuration;
using NUnit.Framework;
using LogsearchShipper.Core.ConfigurationSections;

namespace LogsearchShipper.Core.Tests
{
    [TestFixture]
    public class LogsearchShipperSectionTests
    {
		private LogsearchShipperSection _LogsearchShipper;

        [SetUp]
        public void Setup()
        {
            _LogsearchShipper = ConfigurationManager.GetSection("LogsearchShipperGroup/LogsearchShipper") as LogsearchShipperSection;
        }

        [Test]
        public void ShouldHaveServersAttribute()
        {
			Assert.AreEqual("ingestor.example.com:5043", _LogsearchShipper.Servers);
        }
        [Test]
        public void ShouldHaveSSLCAAttribute()
        {
            StringAssert.Contains("mycert.crt", _LogsearchShipper.SSL_CA);
        }
        [Test]
        public void ShouldHaveTimeoutAttribute()
        {
            Assert.AreEqual(23, _LogsearchShipper.Timeout);
        }

		[Test]
		public void ShouldHaveWatchArray()
		{
			Assert.GreaterOrEqual(_LogsearchShipper.FileWatchers.Count, 1);
		}

		[Test]
		public void ShouldHaveWatchWithFilesAttribute()
		{
			Assert.AreEqual("myfile.log", _LogsearchShipper.FileWatchers[0].Files);
		}

		[Test]
		public void ShouldHaveWatchWithTypeAttribute()
		{
			Assert.AreEqual("myfile_type", _LogsearchShipper.FileWatchers[0].Type);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsArray()
		{
			Assert.GreaterOrEqual(2, _LogsearchShipper.FileWatchers[0].Fields.Count);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("field1", _LogsearchShipper.FileWatchers[0].Fields[0].Key);
			Assert.AreEqual("field1 value", _LogsearchShipper.FileWatchers[0].Fields[0].Value);
		}
    }
}