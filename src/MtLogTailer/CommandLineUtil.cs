using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MtLogTailer
{
	public static class CommandLineUtil
	{
		public static Dictionary<string, string> ParseArgs(IEnumerable<string> args)
		{
			var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var argRaw in args)
			{
				var arg = argRaw;
				if (arg.StartsWith("-") || arg.StartsWith("/"))
				{
					arg = arg.Substring(1);

					if (!arg.Contains(":"))
					{
						res.Add("", arg);
					}
					else
					{
						var parts = arg.Split(':');
						var value = parts[1];
						if (parts.Count() > 2)
						{
							value = string.Join(":", parts.Skip(1));
						}

						res.Add(parts[0], value);
					}
				}
				else
				{
					res.Add("", arg);
				}
			}

			return res;
		}
	}
}
