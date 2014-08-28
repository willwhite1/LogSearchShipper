using log4net.Config;
using NUnit.Framework;

namespace IntegrationTests
{
	[SetUpFixture]
	public class SetupFixture
	{
	  [SetUp]
		public void RunBeforeAnyTests()
		{
			XmlConfigurator.Configure();
		}
	}
}