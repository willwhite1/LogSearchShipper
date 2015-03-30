using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace LogSearchShipper.Core.ConfigurationSections
{
	[ConfigurationCollection(typeof (FileWatchElement), AddItemName = "watch")]
	public class FileWatchCollection : ConfigurationElementCollection
	{
		/// <summary>
		///  Access the collection by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public FileWatchElement this[int index]
		{
			get { return (FileWatchElement) BaseGet(index); }
		}

		/// <summary>
		///  Access the collection by key name
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public new FileWatchElement this[string key]
		{
			get { return (FileWatchElement) BaseGet(key); }
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new FileWatchElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((FileWatchElement) (element)).Files;
		}
	}
}