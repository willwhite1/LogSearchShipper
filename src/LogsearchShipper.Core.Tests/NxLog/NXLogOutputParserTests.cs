using log4net;
using log4net.Core;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests.NxLog
{
	[TestFixture]
	public class NXLogOutputParserTests
	{
		[SetUp]
		public void Setup()
		{
			_nxlogOutputParser = new NxLogOutputParser();
		}

		private NxLogOutputParser _nxlogOutputParser;
		private readonly ILog _logger = LogManager.GetLogger(typeof (NXLogOutputParserTests));

		private LoggingEvent CreateLog4NetLogEvent(string log)
		{
			LoggingEvent logEvent = _nxlogOutputParser.ConvertToLog4Net(_logger, _nxlogOutputParser.Parse(log));
			return logEvent;
		}

		[Test]
		public void ShouldCorrectlyConvertDEBUG()
		{
			string log =
				@"2014-07-14 11:39:54 DEBUG evaluating expression 'substr' at C:\Apps\LogSearchShipper-DEV-data\nxlog.conf:69";

			Assert.AreEqual(Level.Debug, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual(@"{Message=evaluating expression 'substr' at C:\Apps\LogSearchShipper-DEV-data\nxlog.conf:69}",
				CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertERROR()
		{
			string log =
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.";

			Assert.AreEqual(Level.Warn, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual("{Message=failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied., Category=MISSING_FILE}",
				CreateLog4NetLogEvent(log).RenderedMessage);
		}

	 
		[Test]
		public void ShouldCorrectlyConvertERROR2()
		{
			string log =
					 @"2014-07-19 07:18:03 ERROR [router.c:351/nx_add_route()] route route_to_file is not functional without output modules, ignored at C:\Users\david.laing\AppData\Local\Temp\nxlog-data-1c380b9197e94d37a0e64cf6b29034e7\nxlog.conf:35";

			Assert.AreEqual(Level.Error, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual(@"{Message=[router.c:351/nx_add_route()] route route_to_file is not functional without output modules, ignored at C:\Users\david.laing\AppData\Local\Temp\nxlog-data-1c380b9197e94d37a0e64cf6b29034e7\nxlog.conf:35}",
				CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertINFO()
		{
			string log =
				"2014-07-14 09:38:04 INFO connecting to ingestor.cityindex.logsearch.io:5514";

			Assert.AreEqual(Level.Info, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual("{Message=connecting to ingestor.cityindex.logsearch.io:5514}", CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertWARNING()
		{
			string log =
				"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log";

			Assert.AreEqual(Level.Warn, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual(CreateLog4NetLogEvent(log).RenderedMessage,
				"{Message=input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log, Category=MISSING_FILE}");
		}

		[Test]
		public void ShouldExtractLogLevel()
		{
			NxLogOutputParser.NxLogEvent nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
			Assert.AreEqual("WARN", nxLogOutput.LogLevel);

			nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log");
			Assert.AreEqual("WARN", nxLogOutput.LogLevel);
		}

		[Test]
		public void ShouldExtractMessage()
		{
			NxLogOutputParser.NxLogEvent nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
			Assert.AreEqual("failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.",
				nxLogOutput.Message);
		}

		[Test]
		public void ShouldExtractTimeStamp()
		{
			NxLogOutputParser.NxLogEvent nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
			Assert.AreEqual("2014-07-14 11:43:36", nxLogOutput.Timestamp);
		}

		[Test]
		public void ShouldSetCategoryTo_MISSING_FILE_WhenContains()
		{
			{
				var log = "2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log";
				var logEvent = CreateLog4NetLogEvent(log);
				Assert.AreEqual(Level.Warn, logEvent.Level);
				Assert.AreEqual(
					"{Message=input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log, Category=MISSING_FILE}",
					logEvent.RenderedMessage);
			}

			{
				var log = "2014-07-14 12:48:27 ERROR failed to open \\\\INX-SRV-APPL09\\Logs\\tibco\\*.log; The filename, directory name, or volume label syntax is incorrect.";
				var logEvent = CreateLog4NetLogEvent(log);
				Assert.AreEqual(Level.Warn, logEvent.Level);
				Assert.AreEqual(
					"{Message=failed to open \\\\INX-SRV-APPL09\\Logs\\tibco\\*.log; The filename, directory name, or volume label syntax is incorrect., Category=MISSING_FILE}",
					logEvent.RenderedMessage);
			}

			{
				var log = "2014-07-14 12:48:27 ERROR apr_stat failed on file \\\\INX-SRV-TIB03\\Logs\\tibco\\*.log; The filename, directory name, or volume label syntax is incorrect.";
				var logEvent = CreateLog4NetLogEvent(log);
				Assert.AreEqual(Level.Warn, logEvent.Level);
				Assert.AreEqual(
					"{Message=apr_stat failed on file \\\\INX-SRV-TIB03\\Logs\\tibco\\*.log; The filename, directory name, or volume label syntax is incorrect., Category=MISSING_FILE}",
					logEvent.RenderedMessage);
			}
		}

		[Test]
		public void ShouldNotSetCategoryWhen()
		{
			var log = "2014-07-14 09:38:04 INFO connecting to ingestor.cityindex.logsearch.io:5514";
			var logEvent = CreateLog4NetLogEvent(log);
			Assert.AreEqual(Level.Info, logEvent.Level);
			Assert.AreEqual("{Message=connecting to ingestor.cityindex.logsearch.io:5514}", logEvent.RenderedMessage);
		}
	}
}