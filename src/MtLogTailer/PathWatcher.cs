using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MtLogTailer
{
	class PathWatcher
	{
		public PathWatcher(string path, int encoding)
		{
			_path = path;
			_encoding = encoding;
		}

		public void Process()
		{
			lock (_sync)
			{
				_mainThread = Thread.CurrentThread;
			}

			var dirPath = Directory.GetParent(_path).FullName;
			var fileName = Path.GetFileName(_path);
			var isFirstRead = true;

			while (!Program.Terminate)
			{
				foreach (var file in Directory.GetFiles(dirPath, fileName, SearchOption.AllDirectories))
				{
					LogShipper shipper;
					if (!_shippers.TryGetValue(file, out shipper))
					{
						shipper = new LogShipper(file, _encoding);
						_shippers.Add(file, shipper);
						if (isFirstRead)
							shipper.UpdateLastWriteTime();
					}

					shipper.Process();
				}

				isFirstRead = false;
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}

		public void Stop()
		{
			lock (_sync)
			{
				_mainThread.Interrupt();
			}
		}

		private readonly Dictionary<string, LogShipper> _shippers = new Dictionary<string, LogShipper>();

		private readonly object _sync = new object();
		private Thread _mainThread;

		private readonly string _path;
		private readonly int _encoding;
	}
}
