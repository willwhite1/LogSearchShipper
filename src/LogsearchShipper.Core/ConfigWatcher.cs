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
			Watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
			Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
		}

		public DateTime LastWriteTime;
		public readonly FileSystemWatcher Watcher;
		public event Action Changed;

		public void SubscribeConfigFileChanges(Action actionsToRun)
		{
			Watcher.Changed += (s, e) =>
			{
				try
				{
					_log.InfoFormat("Detected change in file: {0}", e.FullPath);
					actionsToRun();
				}
				catch (Exception exc)
				{
					_log.Error(exc);
				}
			};
			Watcher.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			Watcher.EnableRaisingEvents = false;
			Watcher.Dispose();
		}

		private readonly ILog _log;
	}
}
