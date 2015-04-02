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
		public LogShipper(string filePath, int encoding)
		{
			_filePath = filePath;
			_encoding = encoding;
		}

		public void Process()
		{
			var newLastWriteTime = GetLastWriteTime();
			if (newLastWriteTime > _lastWriteTime)
			{
				using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					var newOffset = FindEndOffset(stream);

					ShipLogData(stream, newOffset);

					_offset = newOffset;
					_lastWriteTime = newLastWriteTime;
				}
			}
		}

		private void ShipLogData(FileStream stream, long maxOffset)
		{
			Validate(stream, maxOffset);

			stream.Seek(_offset, SeekOrigin.Begin);
			var pos = _offset;
			using (var bufStream = new BufferedStream(stream))
			{
				using (var reader = new StreamReader(bufStream, Encoding.GetEncoding(_encoding)))
				{
					while (pos < maxOffset)
					{
						if (Program.Terminate)
							throw new ThreadInterruptedException();

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

		private DateTime GetLastWriteTime()
		{
			var newLastWriteTime = (new FileInfo(_filePath)).LastWriteTimeUtc;
			return newLastWriteTime;
		}

		public void UpdateLastWriteTime()
		{
			_lastWriteTime = GetLastWriteTime();
		}

		private static void Validate(Stream stream, long maxOffset)
		{
			stream.Seek(maxOffset - 1, SeekOrigin.Begin);
			if (stream.ReadByte() == 0)
				throw new ApplicationException();

			stream.Seek(maxOffset, SeekOrigin.Begin);
			if (stream.ReadByte() != 0)
				throw new ApplicationException();
		}

		readonly string _filePath;
		long _offset;
		DateTime _lastWriteTime;

		private readonly int _encoding;
	}
}
