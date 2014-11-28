using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using NUnit.Framework;

namespace LogSearchShipper.Core.Tests
{
	[TestFixture]
	static class LogJsonFormattingTests
	{
		[Test]
		public static void TestJsonFormatting()
		{
			var layout = new JSONFragmentPatternLayout();
			layout.ActivateOptions();
			var memoryAppender = new MemoryAppender
				{
					Layout = layout,
				};
			BasicConfigurator.Configure(memoryAppender);
			var log = LogManager.GetLogger(typeof(LogJsonFormattingTests));

			{
				log.Info("Test1");
				var localEvents = memoryAppender.GetEvents();
				Assert.AreEqual("Test1", localEvents.Last().RenderedMessage);
			}

			{
				log.Info(new { Test = "Test1" });
				var localEvents = memoryAppender.GetEvents();
				Assert.AreEqual("{ Test = Test1 }", localEvents.Last().RenderedMessage);
			}

			{
				log.Info(new { Test1 = "Test1", Test2 = new { Test = "Test2" } });
				var localEvents = memoryAppender.GetEvents();
				Assert.AreEqual("{ Test1 = Test1, Test2 = { Test = Test2 } }", localEvents.Last().RenderedMessage);
			}
		}
	}

	public class JSONFragmentPatternLayout : PatternLayout
	{
		public JSONFragmentPatternLayout()
		{
			this.AddConverter("logsite", typeof(JSONFragmentPatternConverter));
		}
	}
}
