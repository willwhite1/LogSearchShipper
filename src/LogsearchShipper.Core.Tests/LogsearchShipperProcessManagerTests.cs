using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
    [TestFixture]
    public class LogsearchShipperProcessManagerTests
    {
        private LogsearchShipperProcessManager _logsearchShipperProcessManager;

        [SetUp]
        public void Setup()
        {
            _logsearchShipperProcessManager = new LogsearchShipperProcessManager();
        }

        [TearDown]
        public void TearDown()
        {
            _logsearchShipperProcessManager.Stop();
        }

        [Test, Ignore("Needs valid logstash ingestor to connect to")]
		[Platform(Exclude="Mono")]
        public void ShouldLaunchGoLogsearchShipperProcess()
        {
            
			_logsearchShipperProcessManager.Start();

            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_logsearchShipperProcessManager.GoLogsearchShipperFile));
            
            Assert.AreEqual(1, processes.Count(), "a logstash-forwarder process wasn't started");
        }

        [Test]
        public void ShouldCorrectlyGenerateGoLogsearchShipperConfigFromAppConfigSettings()
        {

            _logsearchShipperProcessManager.SetupConfigFile();
            var config = File.ReadAllText(_logsearchShipperProcessManager.ConfigFile);
			// Console.WriteLine(config);

            /* We're expecting a config that looks like this:
            * 
            {
            "network": {
            "servers": [ "endpoint.example.com:5034" ],
            "ssl ca": "C:\\Logs\\mycert.crt",
            "timeout": 23
            },
            "files": [
            {
                "paths": [ "myfile.log" ],
                "fields": {
                "@type": "myfile_type",
                "field1": "field1 value"
                "field2": "field2 value"
                }
            },
            {
                "paths": [ "C:\\Logs\\myfile.log" ],
                "fields": {
                "@type": "type/subtype",
                "key/subkey": "value/subvalue"
                }
            },
            {
                "paths": [ "\\PKH-PPE-APP10\logs\Apps\PriceHistoryService\log.log" ],
                "fields": {
	                "@type": "log4net",
	                "edb_key/subkey": "edb_value/subvalue"
                }
            },
            {
                "paths": [ "\\PKH-PPE-APP10\logs\Apps\PriceHistoryService\PriceHistoryStats.log" ],
                "fields": {
	                "@type": "log4net_stats",
	                "edb_key/subkey": "edb_value/subvalue"
                }
            },
            {
                "paths": [ "\\ENV1-DB01\Logs\Nolio\all.log" ],
                "fields": {
	                "@type": "log4j",
	                "edb_key/subkey": "edb_value/subvalue"
                }
            }
            ]
        }
            */

			StringAssert.Contains("\"servers\": [ \"ingestor.example.com:5043\" ]", config);
            StringAssert.Contains("\"ssl ca\": \"C:\\\\Logs\\\\mycert.crt\"", config);
            StringAssert.Contains("\"timeout\": 23", config);
            StringAssert.Contains("\"@type\": \"myfile_type\"", config);
            StringAssert.Contains("\"paths\": [ \"myfile.log\" ]", config);
            StringAssert.Contains("\"field1\": \"field1 value\"", config);
            StringAssert.Contains("\"field2\": \"field2 value\"", config);

            StringAssert.Contains("\"paths\": [ \"C:\\\\Logs\\\\myfile.log\" ]", config);
            StringAssert.Contains("\"@type\": \"type/subtype\"", config);
            StringAssert.Contains("\"key/subkey\": \"value/subvalue\"", config);

			//TODO
//			StringAssert.Contains("\"paths\": [ \"\\\\\\\\PKH-PPE-APP10\\\\logs\\\\Apps\\\\PriceHistoryService\\\\log.log\" ]", config);
//			StringAssert.Contains("\"@type\": \"log4net\"", config);
//			StringAssert.Contains("\"edb_key/subkey\": \"edb_value/subvalue\"", config);
        }
    }
}
