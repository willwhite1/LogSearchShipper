using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationTests
{
	class Record
	{
		public DateTime Time;

		public Dictionary<string, string> Fields = new Dictionary<string, string>();

		public override string ToString()
		{
			return string.Format("{0} {1}", Time, string.Join(", ", Fields));
		}
	}
}
