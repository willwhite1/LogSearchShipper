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

    }
}
