using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MtLogTailer
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				if (args.Length != 1)
					throw new ApplicationException("Invalid args.  Should use MtLogTailer.exe <filePath> <encoding: optional>");
				var filePath = args[0];

				var encoding = 936;  //Default to gb2312-> ANSI/OEM Simplified Chinese (PRC, Singapore); Chinese Simplified (GB2312)
			  if (args.Length >= 2)
					encoding = Convert.ToInt32(args[1]);

				var shipper = new LogShipper(filePath, encoding);

				Console.CancelKeyPress +=
					(sender, eventArgs) =>
					{
						shipper.Stop();
						eventArgs.Cancel = true;
					};

				shipper.Process();
			}
			catch (ApplicationException exc)
			{
				Console.WriteLine(exc.Message);
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc);
			}
		}
	}
}
