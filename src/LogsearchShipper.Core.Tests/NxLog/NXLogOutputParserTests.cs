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
			Assert.AreEqual(@"evaluating expression 'substr' at C:\Apps\LogSearchShipper-DEV-data\nxlog.conf:69",
				CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertERROR()
		{
			string log =
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.";

			Assert.AreEqual(Level.Error, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual("failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.",
				CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertINFO()
		{
			string log =
				"2014-07-14 09:38:04 INFO connecting to ingestor.cityindex.logsearch.io:5514";

			Assert.AreEqual(Level.Info, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual("connecting to ingestor.cityindex.logsearch.io:5514", CreateLog4NetLogEvent(log).RenderedMessage);
		}

		[Test]
		public void ShouldCorrectlyConvertWARNING()
		{
			string log =
				"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log";

			Assert.AreEqual(Level.Warn, CreateLog4NetLogEvent(log).Level);
			Assert.AreEqual(CreateLog4NetLogEvent(log).RenderedMessage,
				"input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log");
		}

		[Test]
		public void ShouldExtractLogLevel()
		{
			NxLogOutputParser.NxLogEvent nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 11:43:36 ERROR failed to open \\\\PKH-QAT-APP03\\Logs\\Apps\\MarginAutoCloseoutService; Access is denied.");
			Assert.AreEqual("ERROR", nxLogOutput.LogLevel);

			nxLogOutput = _nxlogOutputParser.Parse(
				"2014-07-14 12:48:27 WARNING input file does not exist: \\\\PKH-QAT-APP21\\Logs\\ClientPreferenceGateway\\Diagnostics.log");
			Assert.AreEqual("WARNING", nxLogOutput.LogLevel);
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
	}
}