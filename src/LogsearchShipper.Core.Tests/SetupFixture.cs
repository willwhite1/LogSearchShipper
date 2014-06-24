using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using NUnit.Framework;

namespace LogsearchShipper.Core.Tests
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void RunBeforeAnyTests()
		{
			BasicConfigurator.Configure(
				new ConsoleAppender
				{
					Layout = new SimpleLayout()
				});
		}
	}
}