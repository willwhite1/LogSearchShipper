using System.Configuration;
using NUnit.Framework;

namespace LogstashForwarder.Core.Tests
{
    [TestFixture]
    public class LogstashForwarderSectionTests
    {
        private LogstashForwarderSection _logstashForwarder;

        [SetUp]
        public void Setup()
        {
            _logstashForwarder = ConfigurationManager.GetSection("logstashForwarderGroup/logstashForwarder") as LogstashForwarderSection;
        }

        [Test]
        public void ShouldHaveServersAttribute()
        {
            Assert.AreEqual("endpoint.example.com:5034", _logstashForwarder.Servers);
        }
        [Test]
        public void ShouldHaveSSLCAAttribute()
        {
            Assert.AreEqual("mycert.crt", _logstashForwarder.SSL_CA);
        }
        [Test]
        public void ShouldHaveTimeoutAttribute()
        {
            Assert.AreEqual(15, _logstashForwarder.Timeout);
        }

		[Test]
		public void ShouldHaveWatchArray()
		{
			Assert.GreaterOrEqual(1, _logstashForwarder.Watchs.Count);
		}

		[Test]
		public void ShouldHaveWatchWithFilesAttribute()
		{
			Assert.AreEqual("myfile.log", _logstashForwarder.Watchs[0].Files);
		}

		[Test]
		public void ShouldHaveWatchWithTypeAttribute()
		{
			Assert.AreEqual("myfile_type", _logstashForwarder.Watchs[0].Type);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsArray()
		{
			Assert.GreaterOrEqual(2, _logstashForwarder.Watchs[0].Fields.Count);
		}

		[Test]
		public void ShouldHaveWatchWithFieldsWithKeyAndValue()
		{
			Assert.AreEqual("field1", _logstashForwarder.Watchs[0].Fields[0].Key);
			Assert.AreEqual("field1 value", _logstashForwarder.Watchs[0].Fields[0].Value);
		}
    }
}
