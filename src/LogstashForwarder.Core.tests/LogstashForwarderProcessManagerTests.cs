using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace LogstashForwarder.Core.Tests
{
    [TestFixture]
    public class LogstashForwarderProcessManagerTests
    {
        private LogstashForwarderProcessManager _logstashForwarderProcessManager;

        [SetUp]
        public void Setup()
        {
            _logstashForwarderProcessManager = new LogstashForwarderProcessManager();
        }

        [TearDown]
        public void TearDown()
        {
            _logstashForwarderProcessManager.Stop();
        }

        [Test]
        public void ShouldLaunchGoLogstashForwarderProcess()
        {
            
            _logstashForwarderProcessManager.Start();

            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_logstashForwarderProcessManager.GoLogstashForwarderFile));
            
            Assert.AreEqual(1, processes.Count(), "a logstash-forwarder process wasn't started");
        }

        [Test]
        public void ShouldCorrectlyGenerateGoLogstashForwarderConfigFromAppConfigSettings()
        {

            _logstashForwarderProcessManager.SetupConfigFile();
            var config = File.ReadAllText(_logstashForwarderProcessManager.ConfigFile);
            Console.WriteLine(config);

            StringAssert.Contains(string.Format("\"servers\": [ \"endpoint.example.com:5034\" ]"), config);
            StringAssert.Contains(string.Format("\"ssl ca\": \"mycert.crt\""), config);
            StringAssert.Contains(string.Format("\"timeout\": 23"), config);
            StringAssert.Contains(string.Format("\"@type\": \"myfile_type\""), config);
        }
    }
}
