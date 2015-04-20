using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MtLogTailer
{
	static class FileUtil
	{
		// Simple detection of stream encoding
		// see also http://stackoverflow.com/questions/4520184/how-to-detect-the-character-encoding-of-a-text-file
		public static Encoding DetectEncoding(Stream stream)
		{
			var startPosition = stream.Position;

			var bom = new byte[4];
			var bytesRead = stream.Read(bom, 0, 4);
			if (bytesRead < bom.Length)
				return null;

			if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
			{
				stream.Position = stream.Position - 1;
				return Encoding.UTF7;
			}
			if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
			{
				stream.Position = stream.Position - 1;
				return Encoding.UTF8;
			}

			if (bom[0] == 0xff && bom[1] == 0xfe)
			{
				stream.Position = stream.Position - 2;
				return Encoding.Unicode;
			}
			if (bom[0] == 0xfe && bom[1] == 0xff)
			{
				stream.Position = stream.Position - 2;
				return Encoding.BigEndianUnicode;
			}

			if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
				return Encoding.UTF32;

			// UTF-8 without BOM can give false positives
			var encoding = Encoding.UTF8;
			var nonAsciiCharsFound = 0;
			try
			{
				var bufStream = new BufferedStream(stream);
				var reader = new BinaryReader(bufStream, encoding);
				while (true)
				{
					var ch = reader.Read();
					if (ch == -1)
						break;
					if (ch == (char)0xFFFD)
					{
						encoding = Encoding.Default;
						break;
					}
					if (ch > 0x80)
						nonAsciiCharsFound++;
					if (nonAsciiCharsFound > 16)
						break;
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				encoding = Encoding.Default;
			}
			catch (ArgumentException)
			{
				encoding = Encoding.Default;
			}
			catch (EndOfStreamException)
			{
				encoding = Encoding.Default;
			}

			stream.Position = startPosition;
			return encoding;
		}
	}
}
