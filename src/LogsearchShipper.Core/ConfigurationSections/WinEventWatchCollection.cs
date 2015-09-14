using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Core.ConfigurationSections
{
	[ConfigurationCollection(typeof(WinEventWatchElement), AddItemName = "watch")]
	public class WinEventWatchCollection : ConfigurationElementCollection
	{
		public WinEventWatchElement this[int index]
		{
			get { return (WinEventWatchElement)BaseGet(index); }
		}

		public new FileWatchElement this[string key]
		{
			get { return (FileWatchElement)BaseGet(key); }
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new WinEventWatchElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((WinEventWatchElement)(element)).Key;
		}
	}
}
