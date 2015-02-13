using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace LogSearchShipper.Core
{
	class ConfigWatcher : IDisposable
	{
		public ConfigWatcher(string fullPath, ILog log)
		{
			_log = log;
			Watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath))
			{
				NotifyFilter = NotifyFilters.FileName | NotifyFilters .LastWrite | NotifyFilters.Size,
			};
		}

		public readonly FileSystemWatcher Watcher;

		public void SubscribeConfigFileChanges(Action actionsToRun)
		{
			Watcher.Changed += (s, e) => OnChanged(actionsToRun, e);
			Watcher.EnableRaisingEvents = true;
		}

		private void OnChanged(Action actionsToRun, FileSystemEventArgs e)
		{
			try
			{
				var lastWriteTime = File.GetLastWriteTimeUtc(e.FullPath);
				lock (_sync)
				{
					if (_lastWriteTime == lastWriteTime)
					{
						_log.DebugFormat("Ignoring change in file: {0} since last write time hasn't changed", e.FullPath);
						return;
					}
				}

				_log.InfoFormat("Detected change in file: {0}", e.FullPath);
				actionsToRun();

				lock (_sync)
				{
					_lastWriteTime = lastWriteTime;
				}
			}
			catch (Exception exc)
			{
				_log.Error(exc);
			}
		}

		public void Dispose()
		{
			Watcher.EnableRaisingEvents = false;
			Watcher.Dispose();
		}

		private DateTime _lastWriteTime;
		private readonly ILog _log;
		readonly object _sync = new object();
	}
}
