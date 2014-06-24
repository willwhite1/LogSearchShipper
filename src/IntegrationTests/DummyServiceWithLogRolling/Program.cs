using System;
using System.Timers;
using log4net;
using log4net.Config;
using Topshelf;

namespace DummyServiceWithLogRolling
{
	internal class MainClass
	{
		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			HostFactory.Run(x =>
			{
				x.Service<DummyService>(s =>
				{
					s.ConstructUsing(name => new DummyService());
					s.WhenStarted(tc => { tc.Start(); });
					s.WhenStopped(tc => tc.Stop());
				});
				x.RunAsNetworkService();
				x.StartAutomatically();

				x.SetDescription("DummyServiceWithLogRolling");
				x.SetDisplayName("DummyServiceWithLogRolling");
				x.SetServiceName("DummyServiceWithLogRolling");

				x.EnableServiceRecovery(rc => { rc.RestartService(1); // restart the service after 1 minute
				});

				x.UseLog4Net();
			});
		}
	}

	public class DummyService
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (DummyService));
		private Timer _timer;

		public void Start()
		{
			_log.Info("starting");
			_timer = new Timer(TimeSpan.FromMilliseconds(100).TotalMilliseconds);
			_timer.Elapsed += doWork;
			_timer.Enabled = true;
		}

		private static void doWork(object source, ElapsedEventArgs e)
		{
			_log.Info("Doing work... (filling log file with *): " + new String('*', 500));
		}

		public void Stop()
		{
			_log.Info("stopping");
			_timer.Enabled = false;
		}
	}
}