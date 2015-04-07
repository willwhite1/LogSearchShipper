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
		public LogShipper(string filePath, int defaultEncoding)
		{
			_filePath = filePath;
			_defaultEncoding = defaultEncoding;
		}

		public void Process()
		{
			var newLastWriteTime = GetLastWriteTime();
			if (newLastWriteTime > _lastWriteTime)
			{
				using (var stream = OpenStream())
				{
					var newOffset = FindEndOffset(stream);

					ShipLogData(stream, newOffset);

					_offset = newOffset;
					_lastWriteTime = newLastWriteTime;
				}
			}
		}

		private void ShipLogData(Stream stream, long maxOffset)
		{
			Validate(stream, maxOffset);

			stream.Seek(_offset, SeekOrigin.Begin);
			var pos = _offset;

			using (var reader = new StreamReader(stream, Encoding.GetEncoding(_defaultEncoding)))
			{
				var buf = new StringBuilder();

				while (pos < maxOffset)
				{
					if (Program.Terminate)
						throw new ThreadInterruptedException();

					pos = ReadLine(reader, buf, pos, maxOffset);

					Console.Write("{0}\t{1}", _filePath, buf);
					buf.Clear();
				}
			}
		}

		private static long ReadLine(TextReader reader, StringBuilder buf, long pos, long maxOffset)
		{
			while (pos < maxOffset)
			{
				var tmp = reader.Read();
				if (tmp == -1)
					throw new ApplicationException();
				var ch = (char)tmp;
				buf.Append(ch);
				pos++;

				// end of line can be "\r\n", "\n\r", "\n" or "\r" in files produced by different platforms
				if (ch == '\r' || ch == '\n')
				{
					var chNext = reader.Peek();
					if (chNext != -1)
					{
						if (ch == '\r' && chNext == '\n' || ch == '\n' && chNext == '\r')
						{
							reader.Read();
							buf.Append((char)chNext);
						}
					}

					break;
				}
			}
			return pos;
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

		public void Update()
		{
			_lastWriteTime = GetLastWriteTime();
			using (var stream = OpenStream())
			{
				_offset = FindEndOffset(stream);
			}
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

		Stream OpenStream()
		{
			var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			return new BufferedStream(stream, 1024 * 128);
		}

		readonly string _filePath;
		long _offset;
		DateTime _lastWriteTime;

		private readonly int _defaultEncoding;
	}
}
