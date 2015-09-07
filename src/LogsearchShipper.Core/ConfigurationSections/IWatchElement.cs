using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public interface IWatchElement
	{
		string Key { get; }
		FieldCollection Fields { get; set; }
	}
}
