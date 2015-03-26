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
					throw new ApplicationException("Invalid args");
				var filePath = args[0];

				var shipper = new LogShipper(filePath);

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
