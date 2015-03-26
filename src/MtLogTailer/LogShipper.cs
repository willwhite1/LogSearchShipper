﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MtLogTailer
{
	class LogShipper
	{
		public LogShipper(string filePath)
		{
			_filePath = filePath;
		}

		public void Process()
		{
			while (!_terminate)
			{
				var newLastWriteTime = (new FileInfo(_filePath)).LastWriteTimeUtc;
				if (newLastWriteTime != _lastWriteTime)
				{
					using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
					{
						var newOffset = FindEndOffset(stream);

						stream.Seek(_offset, SeekOrigin.Begin);
						var pos = _offset;
						using (var bufStream = new BufferedStream(stream))
						{
							while (pos < newOffset)
							{
								var tmp = bufStream.ReadByte();
								if (tmp == -1)
									throw new ApplicationException();
								var ch = (char)tmp;
								Console.Write(ch);
								pos++;
							}
						}

						_offset = newOffset;
						_lastWriteTime = newLastWriteTime;
					}
				}

				Thread.Sleep(TimeSpan.FromSeconds(1));
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
	}
}
