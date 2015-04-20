using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using MtLogTailer;

namespace LogSearchShipper.Core.Tests
{
	[TestFixture]
	public class MtLogTailerTests
	{
		[Test]
		public void TestFindEndOffset()
		{
			var stream = new MemoryStream();
			for (var i = 0; i <= 300 * 1000; i++)
			{
				stream.WriteByte((byte)(i % 256));
			}

			var buffer = new byte[1024 * 1024];
			var minOffset = 2;

			{
				var res = LogShipper.FindOffset(stream, buffer, minOffset,
					(val, offset) =>
					{
						stream.Position = offset;
						var streamVal = stream.ReadByte();
						if (val != streamVal)
							throw new ApplicationException("Validation error");
						return (offset == 5 && streamVal == 5);
					});
				Assert.AreEqual(5, res);
			}

			{
				var res = LogShipper.FindOffset(stream, buffer, minOffset,
					(val, offset) =>
					{
						stream.Position = offset;
						if (val != stream.ReadByte())
							throw new ApplicationException("Validation error");
						return false;
					});
				Assert.AreEqual(minOffset, res);
			}
		}
	}
}
