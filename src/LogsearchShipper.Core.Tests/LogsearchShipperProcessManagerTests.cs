using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
	[TestFixture]
	public class LogsearchShipperProcessManagerTests
	{
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

		private LogsearchShipperProcessManager _logsearchShipperProcessManager;

		[Test]
		public void ShouldCorrectlyGenerateNXLogConfigFromAppConfigSettings()
		{
			_logsearchShipperProcessManager.SetupConfigFile();
			string config = File.ReadAllText(_logsearchShipperProcessManager.ConfigFile);
			Console.WriteLine(config);

			/* We're expecting a config that looks like this:
            * 
define ROOT C:\Dev\logsearch-shipper.NET\vendor\nxlog

Moduledir %ROOT%\modules
CacheDir %ROOT%\data
Pidfile %ROOT%\data\nxlog.pid
SpoolDir %ROOT%\data
LogLevel INFO

<Extension syslog>
    Module      xm_syslog
</Extension>

<Output out>
    Module	om_tcp
    Host	endpoint.example.com
    Port	5514
    Exec	to_syslog_ietf();
	Exec    log_debug("Sending syslog data: " + $raw_event);
    #OutputType	Syslog_TLS
</Output>

<Route 1>
    Path        file0, file1, file2, file3, file4 => out
</Route>

<Input file0>
    Module	im_file
    File	"myfile.log"
    ReadFromLast TRUE
	SavePos	TRUE
	CloseWhenIdle TRUE
	Exec	$path = file_name(); $type = "myfile_type"; $field1="field1 value"; $field2="field2 value" $Message = $raw_event;
</Input>

<Input file1>
    Module	im_file
    File	"C:\\Logs\\myfile.log"
    ReadFromLast TRUE
	SavePos	TRUE
	CloseWhenIdle TRUE
	Exec	$path = file_name(); $type = "type/subtype"; $field1="field1 value"; $Message = $raw_event;
</Input>
             
<Input file2>
    Module	im_file
    File	"\\\\PKH-PPE-APP10\\logs\\Apps\\PriceHistoryService\\log.log"
    ReadFromLast TRUE
	SavePos	TRUE
	CloseWhenIdle TRUE
	Exec	$path = file_name(); $type = "log4net"; $host="PKH-PPE-APP10"; $service="PriceHistoryService"; $Message = $raw_event;
</Input>
            */

			StringAssert.Contains("define ROOT ", config);
			StringAssert.Contains(@"Moduledir %ROOT%\modules", config);
			StringAssert.Contains(@"CacheDir %ROOT%\data", config);
			StringAssert.Contains(@"Pidfile %ROOT%\data\nxlog.pid", config);
			StringAssert.Contains(@"SpoolDir %ROOT%\data", config);
			StringAssert.Contains("LogLevel INFO", config);

			StringAssert.Contains("<Extension syslog>", config);
			StringAssert.Contains("Module      xm_syslog", config);

			StringAssert.Contains("<Output out>", config);
			StringAssert.Contains("Module	om_tcp", config);

			StringAssert.Contains("Host	ingestor.example.com", config);
			StringAssert.Contains("Port	443", config);
			StringAssert.Contains("Exec	to_syslog_ietf();", config);
		}

		[Test, Ignore("Needs valid logstash ingestor to connect to")]
		[Platform(Exclude = "Mono")]
		public void ShouldLaunchNxLogProcess()
		{
			_logsearchShipperProcessManager.Start();

			Process[] processes = Process.GetProcessesByName("nxlog.exe");

			Assert.AreEqual(1, processes.Count(), "a NXLog process wasn't started");
		}
	}
}