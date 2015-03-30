using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MtLogTailer
{
	class LogShipper
	{
		public LogShipper(string filePath, int encoding)
		{
			_filePath = filePath;
			_encoding = encoding;
		}

		public void Process()
		{
			while (!_terminate)
			{
				var newLastWriteTime = (new FileInfo(_filePath)).LastWriteTimeUtc;
				if (newLastWriteTime != _lastWriteTime)
				{
					using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					{
						var newOffset = FindEndOffset(stream);

						if (_lastWriteTime != DateTime.MinValue)
							ShipLogData(stream, newOffset);

						_offset = newOffset;
						_lastWriteTime = newLastWriteTime;
					}
				}

				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}

		private void ShipLogData(FileStream stream, long newOffset)
		{
			stream.Seek(_offset, SeekOrigin.Begin);
			var pos = _offset;
			using (var bufStream = new BufferedStream(stream))
			{
				using (var reader = new StreamReader(bufStream, Encoding.GetEncoding(_encoding)))
				{
					while (pos < newOffset)
					{
						var tmp = reader.Read();
						if (tmp == -1)
							throw new ApplicationException();
						var ch = (char)tmp;
						Console.Write(ch);
						pos++;
					}
				}
			}
		}

		public void Stop()
		{
			_terminate = true;
		}

		static long FindEndOffset(Stream stream)
		{
			var buf = new byte[1024 * 128];
			var fileSize = stream.Length;
			var offset = fileSize - buf.Length;
			if (offset < 0)
				offset = 0;

			while (true)
			{
				stream.Seek(offset, SeekOrigin.Begin);
				var blockLength = stream.Read(buf, 0, buf.Length);
				if (blockLength == 0)
					return 0;

				var i = blockLength - 1;
				for (; i > 0; i--)
				{
					if (buf[i] != '\0')
						return offset + i + 1;
				}

				offset -= blockLength;
			}
		}

		readonly string _filePath;
		long _offset;
		DateTime _lastWriteTime;

		private volatile bool _terminate;
		private int _encoding;
	}
}
