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

			Init();
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

		private void ShipLogData(Stream stream, long endOffset)
		{
			Validate(stream, endOffset);

			stream.Position = _offset;

			using (var reader = new BinaryReader(stream, Encoding))
			{
				var buf = new StringBuilder();

				while (stream.Position < endOffset)
				{
					if (Program.Terminate)
						throw new ThreadInterruptedException();

					buf.Clear();
					ReadLine(reader, buf, endOffset);

					Console.Write("{0}\t{1}", _filePath, buf);
				}
			}
		}

		private static void ReadLine(BinaryReader reader, StringBuilder buf, long endOffset)
		{
			while (reader.BaseStream.Position < endOffset)
			{
				var tmp = reader.Read();
				if (tmp == -1)
					throw new Exception();
				var ch = (char)tmp;
				buf.Append(ch);

				// end of line can be "\r\n", "\n\r", "\n" or "\r" in files produced by different platforms
				if (ch == '\r' || ch == '\n')
				{
					var chNext = reader.PeekChar();
					if (chNext != -1)
					{
						if (ch == '\r' && chNext == '\n' || ch == '\n' && chNext == '\r')
							buf.Append(reader.ReadChar());
					}

					return;
				}
			}
		}

		// returns position of the first zero after the meaningful data
		long FindEndOffset(Stream stream)
		{
			var fileSize = stream.Length;

			for (var i = fileSize - 1; i >= _startOffset; i--)
			{
				stream.Position = i;
				if (stream.ReadByte() != 0)
					return i + 1;
			}

			return _startOffset;
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

		private void Init()
		{
			using (var stream = OpenStream())
			{
				_encoding = FileUtil.DetectEncoding(stream);
				if (Equals(_encoding, Encoding.Default))
					_encoding = null;
				_startOffset = stream.Position;
			}
		}

		private static void Validate(Stream stream, long maxOffset)
		{
			if (maxOffset > 0)
			{
				stream.Position = maxOffset - 1;
				if (stream.ReadByte() == 0)
					throw new Exception();
			}

			stream.Position = maxOffset;
			var next = stream.ReadByte();
			if (next != 0 && next != -1)
				throw new Exception();
		}

		Stream OpenStream()
		{
			var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			return new BufferedStream(stream, 1024 * 128);
		}

		readonly string _filePath;
		long _startOffset;
		long _offset;
		DateTime _lastWriteTime;

		public Encoding Encoding
		{
			get { return _encoding ?? Encoding.GetEncoding(_defaultEncoding); }
		}

		private Encoding _encoding;
		private readonly int _defaultEncoding;
	}
}
