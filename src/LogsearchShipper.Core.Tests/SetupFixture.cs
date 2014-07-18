using log4net.Config;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void RunBeforeAnyTests()
		{
			XmlConfigurator.Configure();
//			BasicConfigurator.Configure(
//				new ConsoleAppender
//				{
//					Layout = new SimpleLayout()
//				});
		}
	}
}