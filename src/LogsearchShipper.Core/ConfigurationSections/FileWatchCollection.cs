using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class FileWatchElement : ConfigurationElement
	{
		private FieldCollection _fieldCollection;

		[ConfigurationProperty("files", IsKey = true, IsRequired = true)]
		public String Files
		{
			get { return (String) this["files"]; }
			set { this["files"] = value; }
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public String Type
		{
			get { return (String) this["type"]; }
			set { this["type"] = value; }
		}

		[ConfigurationProperty("readFromLast", IsRequired = false, DefaultValue = true)]
		public bool ReadFromLast
		{
			get { return (bool)this["readFromLast"]; }
			set { this["readFromLast"] = value; }
		}

		[ConfigurationProperty("customNxLogConfig")]
		public CustomNxlogConfig CustomNxlogConfig
		{
			get { return (CustomNxlogConfig)this["customNxLogConfig"]; }
			set { this["customNxLogConfig"] = value; }
		}

		[ConfigurationProperty("closeWhenIdle", IsRequired = false, DefaultValue = true)]
		public bool CloseWhenIdle
		{
			get { return (bool)this["closeWhenIdle"]; }
			set { this["closeWhenIdle"] = value; }
		}

		[ConfigurationProperty("", IsDefaultCollection = true)]
		public FieldCollection Fields
		{
			get
			{
				if (_fieldCollection == null)
				{
					_fieldCollection = (FieldCollection) base[""];
				}
				return _fieldCollection;
			}
			set { _fieldCollection = value; }
		}

		public override string ToString()
		{
			return string.Format("{0}", Files);
		}
	}

	public class CustomNxlogConfig : ConfigurationElement
	{
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value.Trim(); }
		}

		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			Value = (string)reader.ReadElementContentAs(typeof(string), null);
		}
	}

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