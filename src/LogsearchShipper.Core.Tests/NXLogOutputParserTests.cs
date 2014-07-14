using System.Configuration;
using log4net;
using log4net.Core;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
	[TestFixture]
	public class NXLogOutputParserTests
	{
		[SetUp]
		public void Setup()
		{
				_nxlogOutputParser = new NXLogOutputParser();
		}

		private NXLogOutputParser _nxlogOutputParser;
		private ILog _logger = LogManager.GetLogger(typeof (NXLogOutputParserTests));

		[Test]
		public void ShouldExtractTimeStamp()
		{
			var nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
			Assert.AreEqual("2014-07-14 11:43:36", nxLogOutput.Timestamp);
		}

		[Test]
		public void ShouldExtractLogLevel()
		{
				var nxLogOutput = _nxlogOutputParser.Parse(
					"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
				Assert.AreEqual("ERROR", nxLogOutput.LogLevel);

				nxLogOutput = _nxlogOutputParser.Parse(
	"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log");
				Assert.AreEqual("WARNING", nxLogOutput.LogLevel);
		}

		[Test]
		public void ShouldExtractMessage()
		{
				var nxLogOutput = _nxlogOutputParser.Parse(
					"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
				Assert.AreEqual("failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.", nxLogOutput.Message);
		}

		[Test]
		public void ShouldCorrectlyConvertDEBUG()
		{
				var log =
					@"2014-07-14 11:39:54 DEBUG evaluating expression 'substr' at C:\Apps\LogSearchShipper-DEV-data\nxlog.conf:69";

				Assert.AreEqual(Level.Debug, CreateLog4NetLogEvent(log).Level);
				Assert.AreEqual(@"evaluating expression 'substr' at C:\Apps\LogSearchShipper-DEV-data\nxlog.conf:69", CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertINFO()
		{
				var log =
					"2014-07-14 09:38:04 INFO connecting to ingestor.cityindex.logsearch.io:5514";

				Assert.AreEqual(Level.Info, CreateLog4NetLogEvent(log).Level);
				Assert.AreEqual("connecting to ingestor.cityindex.logsearch.io:5514", CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertERROR()
		{
			var log =
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.";

				Assert.AreEqual(Level.Error, CreateLog4NetLogEvent(log).Level);
				Assert.AreEqual("failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.", CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertWARNING()
		{
				var log =
					"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log";

				Assert.AreEqual(Level.Warn, CreateLog4NetLogEvent(log).Level);
				Assert.AreEqual(CreateLog4NetLogEvent(log).RenderedMessage, "input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log");
		}

		private LoggingEvent CreateLog4NetLogEvent(string log)
		{
			var logEvent = _nxlogOutputParser.ConvertToLog4Net(_logger, _nxlogOutputParser.Parse(log));
			return logEvent;
		}
	}
}