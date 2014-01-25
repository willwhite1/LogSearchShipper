using System.Configuration;
using LogstashForwarder.Core.ConfigurationSections;
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
            StringAssert.Contains("mycert.crt", _logstashForwarder.SSL_CA);
        }
        [Test]
        public void ShouldHaveTimeoutAttribute()
        {
            Assert.AreEqual(23, _logstashForwarder.Timeout);
        }

		[Test]
		public void ShouldHaveWatchArray()
		{
			Assert.GreaterOrEqual(_logstashForwarder.Watchs.Count, 1);
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
        
        [Test]
        public void ShouldHaveEnvironmentWatchArray()
        {
            Assert.GreaterOrEqual(_logstashForwarder.EnvironmentWatches.Count, 1);
        }

        [Test]
        public void ShouldHaveEnvironmentWatchWithDataFileAttribute()
        {
            Assert.AreEqual("environment-data.json", _logstashForwarder.EnvironmentWatches[0].DataFile);
        }

        [Test]
        public void ShouldHaveEnvironmentWatchWithEnvironmentNamesAttribute()
        {
            Assert.AreEqual("TEST", _logstashForwarder.EnvironmentWatches[0].EnvironmentNames);
        }

        [Test]
        public void ShouldHaveEnvironmentWatchWithServerGroupNamesAttribute()
        {
            Assert.AreEqual("DMZ|APP", _logstashForwarder.EnvironmentWatches[0].ServerGroupNames);
        }

        [Test]
        public void ShouldHaveEnvironmentWatchWithServiceNamesAttribute()
        {
            Assert.AreEqual("nolio.*", _logstashForwarder.EnvironmentWatches[0].ServiceNames);
        }

        [Test]
        public void ShouldHaveEnvironmentWatchWithFieldsWithKeyAndValue()
        {
            Assert.AreEqual("key/subkey", _logstashForwarder.EnvironmentWatches[0].Fields[0].Key);
            Assert.AreEqual("value/subvalue", _logstashForwarder.EnvironmentWatches[0].Fields[0].Value);
        }
     
    }
}