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
		public static Encoding DetectEncoding(Stream stream, Encoding defaultEncoding)
		{
			var startPosition = stream.Position;

			var bom = new byte[4];
			var bytesRead = stream.Read(bom, 0, 4);
			if (bytesRead < bom.Length)
			{
				stream.Position = startPosition;
				return null;
			}

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

			stream.Position = startPosition;

			return defaultEncoding;
		}
	}
}
