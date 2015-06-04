using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MtLogTailer
{
	public class LogShipper
	{
		public LogShipper(string filePath, int defaultEncoding, bool readFromLast)
		{
			Program.Log(LogLevel.Info, "LogShipper(). Path: {0}", filePath);

			_filePath = filePath;
			_defaultEncoding = defaultEncoding;
			_readFromLast = readFromLast;
		}

		public void Process()
		{
			EnsureInitDone();

			if (_readFromLast && _isFirstRead)
			{
				UpdateCurOffset();
				_isFirstRead = false;
			}

			var newLastWriteTime = GetLastWriteTime();
			if (newLastWriteTime > _lastWriteTime)
			{
				Program.Log(LogLevel.Info, "File '{0}' has changed. Processing...", _filePath);

				using (var stream = OpenStream())
				{
					stream.Position = _offset;

					ShipLogData(stream);

					_offset = stream.Position;
					_lastWriteTime = newLastWriteTime;
				}
			}
		}

		private void ShipLogData(Stream stream)
		{
			var reader = new BinaryReader(stream, Encoding);
			var buf = new StringBuilder();

			// read the rest of an incomplete line at the start, if any
			if (_isFirstLine)
			{
				if (stream.Length <= _startOffset)
					return;

				if (stream.Position > _startOffset)
				{
					stream.Position = stream.Position - 1;
					// if the previous line ends with a linefeed char, this will read that linefeed char only
					// otherwise, it will read until the next line
					if (!ReadLine(reader, buf))
						return;
					buf.Clear();
				}

				_isFirstLine = false;
			}

			while (true)
			{
				if (Program.Terminate)
					throw new ThreadInterruptedException();

				buf.Clear();
				if (!ReadLine(reader, buf))
					break;

				Console.Write("{0}\t{1}", _filePath, buf);
			}
		}

		private bool ReadLine(BinaryReader reader, StringBuilder buf)
		{
			var stream = reader.BaseStream;
			var startPosition = stream.Position;

			while (true)
			{
				int tmp;
				try
				{
					tmp = reader.Read();
				}
				catch (ArgumentException exc)
				{
					// check if this is a UTF-8 "The output char buffer is too small" exception
					// (happening occasionally when only a part of UTF-8 char was flushed to the file)
					if (exc.ParamName == "chars" && Equals(_encoding, Encoding.UTF8))
					{
						Program.Log(LogLevel.Warn, "File '{0}': incomplete UTF8 char in the log ending is detected. Re-try later.", _filePath);
						break;
					}
					else
					{
						var message = FormatEncodingMessage(stream, reader, startPosition);
						throw new ApplicationException(message);
					}
				}
				if (tmp == -1)
					break;

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

					return true;
				}
			}

			buf.Clear();
			stream.Position = startPosition;
			return false;
		}

		private string FormatEncodingMessage(Stream stream, BinaryReader reader, long startPosition)
		{
			var curPosition = stream.Position;
			stream.Position = startPosition;
			var bytes = reader.ReadBytes(16);
			var message = string.Format(
				"Error when reading a char. File '{0}', encoding '{1}', started at {2}, ended at {3}, " +
				"total length {4}, bytes {5}.",
				_filePath, _encoding.WebName, startPosition, curPosition, stream.Length,
				Convert.ToBase64String(bytes));
			return message;
		}

		// returns position of the first zero after the meaningful data
		long FindEndOffset(Stream stream)
		{
			var res = FindOffset(stream, _buffer, _startOffset, (val, offset) => val != '\0');
			return res + 1;
		}

		public static long FindOffset(Stream stream, byte[] buffer, long minOffset, Func<byte, long, bool> condition)
		{
			var fileSize = stream.Length;
			var prevBlockStart = fileSize;
			var blockStart = prevBlockStart - buffer.Length;

			while (true)
			{
				if (blockStart < minOffset)
					blockStart = minOffset;

				stream.Seek(blockStart, SeekOrigin.Begin);
				var blockSize = (int)Math.Min(prevBlockStart - blockStart, buffer.Length);
				var bytesRead = stream.Read(buffer, 0, blockSize);
				if (bytesRead == 0)
					break;

				for (var i = bytesRead - 1; i > 0; i--)
				{
					var globalOffset = blockStart + i;
					if (condition(buffer[i], globalOffset))
						return globalOffset;
				}

				prevBlockStart = blockStart;
				blockStart -= bytesRead;
			}

			return minOffset;
		}

		private DateTime GetLastWriteTime()
		{
			var newLastWriteTime = (new FileInfo(_filePath)).LastWriteTimeUtc;
			return newLastWriteTime;
		}

		void UpdateCurOffset()
		{
			_lastWriteTime = GetLastWriteTime();
			using (var stream = OpenStream())
			{
				_offset = FindEndOffset(stream);
			}

			Program.Log(LogLevel.Info, "LogShipper.UpdateCurOffset() done. File '{0}', offset: {1}", _filePath, _offset);
		}

		private void EnsureInitDone()
		{
			if (_initDone)
				return;

			using (var stream = OpenStream())
			{
				_encoding = FileUtil.DetectEncoding(stream);
				if (_encoding == null)
					return;
				if (Equals(_encoding, Encoding.Default))
					_encoding = null;
				_startOffset = stream.Position;
				_offset = _startOffset;
			}

			Program.Log(LogLevel.Info, "LogShipper.Init() done. File '{0}', encoding: {1}, start offset: {2}", _filePath, Encoding.WebName, _startOffset);

			_initDone = true;
		}

		private bool _initDone;

		Stream OpenStream()
		{
			var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufSize);
			return stream;
		}

		readonly string _filePath;
		long _startOffset;
		long _offset;
		DateTime _lastWriteTime;
		private bool _isFirstLine = true;

		public Encoding Encoding
		{
			get { return _encoding ?? Encoding.GetEncoding(_defaultEncoding); }
		}

		private Encoding _encoding;
		private readonly int _defaultEncoding;

		private const int BufSize = 1024 * 1024;
		readonly byte[] _buffer = new byte[BufSize];

		private readonly bool _readFromLast;
		bool _isFirstRead = true;
	}
}
