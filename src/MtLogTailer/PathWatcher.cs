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
		public PathWatcher(string path, int encoding, bool readFromLast)
		{
			_path = path;
			_encoding = encoding;
			_readFromLast = readFromLast;
		}

		public void Process()
		{
			lock (_sync)
			{
				_mainThread = Thread.CurrentThread;
			}

			var dirPath = Directory.GetParent(_path).FullName;
			var fileName = Path.GetFileName(_path);

			while (!Program.Terminate)
			{
				if (Directory.Exists(dirPath))
					ProcessFiles(dirPath, fileName);

				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}

		private void ProcessFiles(string dirPath, string fileMask)
		{
			try
			{
				foreach (var file in Directory.GetFiles(dirPath, fileMask, SearchOption.AllDirectories))
				{
					try
					{
						LogShipper shipper;
						if (!_shippers.TryGetValue(file, out shipper))
						{
							shipper = new LogShipper(file, _encoding, _readFromLast);
							_shippers.Add(file, shipper);
						}

						shipper.Process();
					}
					catch (Exception exc)
					{
						Program.LogError(exc.ToString());
					}
				}
			}
			catch (Exception exc)
			{
				Program.LogError(exc.ToString());
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
		private readonly bool _readFromLast;
	}
}
