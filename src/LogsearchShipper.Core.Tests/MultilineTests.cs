using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LogSearchShipper.Core.ConfigurationSections;
using LogSearchShipper.Core.NxLog;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	[TestFixture]
	public class MultilineTests
	{
		private readonly List<NxLogProcessManager> _nxLogProcessManagers = new List<NxLogProcessManager>();

		[TestFixtureTearDown]
		public void CleanUp()
		{
			_nxLogProcessManagers.ForEach(n => n.Stop());
		}

		[Test, Ignore("WIP:  Multiline not implemented yet")]
		public void ShouldHandleMultilineLog4NetExceptions()
		{
		 var syslogEndpoint = new SyslogEndpoint("localhost", 10121);

			var tmpDir = Path.Combine(Path.GetTempPath(), "ShouldHandleMultilineLog4NetExceptions");
			string sourceLogFile = Path.Combine(tmpDir, "sourceLogFile.log");
			var outputFile = Path.Combine(tmpDir, "recieverOutputFile.log");
			if (File.Exists(sourceLogFile)) File.WriteAllText(sourceLogFile, string.Empty);
			if (File.Exists(outputFile)) File.WriteAllText(outputFile, string.Empty);

			var reciever = new NxLogProcessManager(Path.Combine(tmpDir, "reciever"))
			{
				InputSyslog = new SyslogEndpoint("localhost", 10121),
				OutputFile = outputFile
			};
			_nxLogProcessManagers.Add(reciever);
			reciever.Start();
			//Console.WriteLine(reciever.Config);

			var shipper = new NxLogProcessManager(Path.Combine(tmpDir, "shipper"))
			{
				InputFiles = new List<FileWatchElement>
				{
					new FileWatchElement
					{
						Files = sourceLogFile,
						Type = "plain"
					}
				},
				OutputSyslog = syslogEndpoint
			};
			_nxLogProcessManagers.Add(shipper);
			shipper.Start();
			//Console.WriteLine(shipper.Config);

			Thread.Sleep(TimeSpan.FromSeconds(2)); // Ensure the shipper has had time to startup

			File.WriteAllText(sourceLogFile,
				@"INFO  2014-07-15 08:20:18,016 44 UTPMessaging.ActiveMQ.Server.ResponseChannel Response stats : CurrentMessageProcesses 2, CurrentMessageHandlerProcesses: 1, Stats for CorrelationId '39db50d8-86d0-4a58-bfd0-4ef95ae64b55': ChannelName: OrderGateway2, ResponseQueueName: temp-queue://ID:INX-SRV-WEBL24-63107-635409895714389415-1:1:1, MessagingRequest: t=2014-07-15T07:20:18.0163031Z, MessageHandlerRequest: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, MessageHandlerResponseDateTime: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, ResponseQueued: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, ResponseDequeued: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, MessagingResponse: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, Error: N/A.
ERROR 2014-07-15 08:20:18,172 6 OrderGateway.OrderGatewayHost System.Data.SqlClient.SqlException (0x80131904): Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding. ---> System.ComponentModel.Win32Exception (0x80004005): The wait operation timed out
at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
at System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
at System.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
at System.Data.SqlClient.SqlDataReader.TryConsumeMetaData()
at System.Data.SqlClient.SqlDataReader.get_MetaData()
at System.Data.SqlClient.SqlCommand.FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, String resetOptionsString)
at System.Data.SqlClient.SqlCommand.RunExecuteReaderTds(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, Boolean async, Int32 timeout, Task& task, Boolean asyncWrite, SqlDataReader ds)
at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method, TaskCompletionSource`1 completion, Int32 timeout, Task& task, Boolean asyncWrite)
at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method)
at System.Data.SqlClient.SqlCommand.ExecuteReader(CommandBehavior behavior, String method)
at System.Data.SqlClient.SqlCommand.ExecuteReader()
at OrderGateway.DataAccess.ApiStopLimitOrderHistoryDataAccess.GetApiStopLimitOrderHistoryForClientAccount(Int32 clientAccountId, Int32 maxResults) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\DataAccess\ApiStopLimitOrderHistoryDataAccess.cs:line 65
at OrderGateway.Query.StopLimitOrderHistoryQuery.GetStopLimitOrderHistory(StopLimitOrderHistoryRequestDTO request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\Query\StopLimitOrderHistoryQuery.cs:line 45
at OrderGateway.OrderApplication.GetStopLimitOrderHistory(StopLimitOrderHistoryRequestDTO request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\OrderApplication.cs:line 66
at OrderGateway.OrderGatewayHost.ProcessRestRequest(Object request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\OrderGatewayHost.cs:line 93
ClientConnectionId:aee2ec82-19b0-4f86-8fca-c8bb2a988c15
INFO  2014-07-15 08:20:18,172 44 UTPMessaging.ActiveMQ.Server.ResponseChannel Response stats : CurrentMessageProcesses 1, CurrentMessageHandlerProcesses: 0, Stats for CorrelationId '5b5f4538-8857-4f85-b4a9-d603c72d951e': ChannelName: OrderGateway2, ResponseQueueName: temp-queue://ID:INX-SRV-WEBL24-63107-635409895714389415-1:1:1, MessagingRequest: t=2014-07-15T07:19:48.1580945Z, MessageHandlerRequest: t=2014-07-15T07:19:48.1580945Z, d=0.0000ms, MessageHandlerResponseDateTime: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, ResponseQueued: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, ResponseDequeued: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, MessagingResponse: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, Error: N/A.
");

			Thread.Sleep(TimeSpan.FromSeconds(10));
			_nxLogProcessManagers.ForEach(n => n.Stop());
		  Thread.Sleep(TimeSpan.FromSeconds(3));


			string[] shippedLogs = File.ReadAllLines(outputFile);
			Console.WriteLine(string.Join("\n", shippedLogs));

			Assert.AreEqual(3, shippedLogs.Length, string.Join("\n", shippedLogs));

			Assert.AreEqual(shippedLogs[0],
				"INFO  2014-07-15 08:20:18,016 44 UTPMessaging.ActiveMQ.Server.ResponseChannel Response stats : CurrentMessageProcesses 2, CurrentMessageHandlerProcesses: 1, Stats for CorrelationId '39db50d8-86d0-4a58-bfd0-4ef95ae64b55': ChannelName: OrderGateway2, ResponseQueueName: temp-queue://ID:INX-SRV-WEBL24-63107-635409895714389415-1:1:1, MessagingRequest: t=2014-07-15T07:20:18.0163031Z, MessageHandlerRequest: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, MessageHandlerResponseDateTime: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, ResponseQueued: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, ResponseDequeued: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, MessagingResponse: t=2014-07-15T07:20:18.0163031Z, d=0.0000ms, Error: N/A.");
			Assert.AreEqual(shippedLogs[1],
				@"ERROR 2014-07-15 08:20:18,172 6 OrderGateway.OrderGatewayHost System.Data.SqlClient.SqlException (0x80131904): Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding. ---> System.ComponentModel.Win32Exception (0x80004005): The wait operation timed out
   at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
   at System.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
   at System.Data.SqlClient.SqlDataReader.TryConsumeMetaData()
   at System.Data.SqlClient.SqlDataReader.get_MetaData()
   at System.Data.SqlClient.SqlCommand.FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, String resetOptionsString)
   at System.Data.SqlClient.SqlCommand.RunExecuteReaderTds(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, Boolean async, Int32 timeout, Task& task, Boolean asyncWrite, SqlDataReader ds)
   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method, TaskCompletionSource`1 completion, Int32 timeout, Task& task, Boolean asyncWrite)
   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method)
   at System.Data.SqlClient.SqlCommand.ExecuteReader(CommandBehavior behavior, String method)
   at System.Data.SqlClient.SqlCommand.ExecuteReader()
   at OrderGateway.DataAccess.ApiStopLimitOrderHistoryDataAccess.GetApiStopLimitOrderHistoryForClientAccount(Int32 clientAccountId, Int32 maxResults) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\DataAccess\ApiStopLimitOrderHistoryDataAccess.cs:line 65
   at OrderGateway.Query.StopLimitOrderHistoryQuery.GetStopLimitOrderHistory(StopLimitOrderHistoryRequestDTO request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\Query\StopLimitOrderHistoryQuery.cs:line 45
   at OrderGateway.OrderApplication.GetStopLimitOrderHistory(StopLimitOrderHistoryRequestDTO request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\OrderApplication.cs:line 66
   at OrderGateway.OrderGatewayHost.ProcessRestRequest(Object request) in d:\Agent2\work\aac3f06c823b988c\Main\WinServices\OrderGateway\OrderGatewayHost.cs:line 93
ClientConnectionId:aee2ec82-19b0-4f86-8fca-c8bb2a988c15");
			Assert.AreEqual(shippedLogs[2],
				"INFO  2014-07-15 08:20:18,172 44 UTPMessaging.ActiveMQ.Server.ResponseChannel Response stats : CurrentMessageProcesses 1, CurrentMessageHandlerProcesses: 0, Stats for CorrelationId '5b5f4538-8857-4f85-b4a9-d603c72d951e': ChannelName: OrderGateway2, ResponseQueueName: temp-queue://ID:INX-SRV-WEBL24-63107-635409895714389415-1:1:1, MessagingRequest: t=2014-07-15T07:19:48.1580945Z, MessageHandlerRequest: t=2014-07-15T07:19:48.1580945Z, d=0.0000ms, MessageHandlerResponseDateTime: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, ResponseQueued: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, ResponseDequeued: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, MessagingResponse: t=2014-07-15T07:20:18.1723021Z, d=30014.2076ms, Error: N/A.");
		}
	}
}