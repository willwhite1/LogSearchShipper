using System.Configuration;
using NUnit.Framework;

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
            Assert.AreEqual("endpoint.example.com:5034", _LogsearchShipper.Servers);
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
			Assert.GreaterOrEqual(_LogsearchShipper.Watchs.Count, 1);
		}

		[Test]
		public void ShouldHaveWatchWithFilesAttribute()
		{
			Assert.AreEqual("myfile.log", _LogsearchShipper.Watchs[0].Files);
		}

		[Test]
		public void ShouldHaveWatchWithTypeAttribute()
		{
			Assert.AreEqual("myfile_type", _LogsearchShipper.Watchs[0].Type);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsArray()
		{
			Assert.GreaterOrEqual(2, _LogsearchShipper.Watchs[0].Fields.Count);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("field1", _LogsearchShipper.Watchs[0].Fields[0].Key);
			Assert.AreEqual("field1 value", _LogsearchShipper.Watchs[0].Fields[0].Value);
		}
    }
}