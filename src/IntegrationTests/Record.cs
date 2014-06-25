using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationTests
{
	class Record
	{
		public DateTime Time;
		public string Name;
		public object Value;

		public override string ToString()
		{
			return string.Format("{0} {1}:{2}", Time, Name, Value);
		}
	}
}
