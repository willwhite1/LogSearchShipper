using System;
using System.Configuration;

namespace LogsearchShipper.Core.ConfigurationSections
{
	public class FileWatchElement : ConfigurationElement
	{
		[ConfigurationProperty("files", IsKey = true, IsRequired = true)]
		public String Files
		{
			get
			{
				return (String)this["files"];
			}
			set
			{
				this["files"] = value;
			}
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public String Type
		{
			get
			{
				return (String)this["type"];
			}
			set
			{
				this["type"] = value;
			}
		}

		private FieldCollection _fieldCollection;
		[ConfigurationProperty("", IsDefaultCollection = true)]
		public FieldCollection Fields
		{
			get
			{
				if (_fieldCollection == null) {
					_fieldCollection = (FieldCollection)base[""];
				}
				return _fieldCollection;
			}
			set 
			{
				_fieldCollection = value;
			}
		}

	}

	[ConfigurationCollection(typeof(FileWatchElement), AddItemName = "watch")]
	public class FileWatchCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new FileWatchElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((FileWatchElement)(element)).Files;
		}

		/// <summary>
		/// Access the collection by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public FileWatchElement this[int index]
		{
			get { return (FileWatchElement)BaseGet(index); }
		}

		/// <summary>
		/// Access the collection by key name
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public new FileWatchElement this[string key]
		{
			get { return (FileWatchElement)BaseGet(key); }
		}
	}
}