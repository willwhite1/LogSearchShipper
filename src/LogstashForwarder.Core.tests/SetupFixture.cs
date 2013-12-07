using NUnit.Framework;

namespace LogstashForwarder.Core.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [SetUp]
        public void RunBeforeAnyTests()
        {
            log4net.Config.BasicConfigurator.Configure(
                  new log4net.Appender.ConsoleAppender
                  {
                      Layout = new log4net.Layout.SimpleLayout()
                  });
        }
    }
}
