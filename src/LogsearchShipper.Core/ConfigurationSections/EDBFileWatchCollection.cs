using System;
using System.Configuration;

namespace LogsearchShipper.Core.ConfigurationSections
{
	public class EnvironmentWatchElement : ConfigurationElement
	{
		[ConfigurationProperty("dataFile", IsKey = true, IsRequired = true)]
		public String DataFile
		{
			get
			{
				return (String)this["dataFile"];
			}
			set
			{
				this["dataFile"] = value;
			}
		}

		[ConfigurationProperty("networkAreas", IsRequired = false)]
		public String NetworkAreas
		{
			get
			{
				if (this.Properties.Contains("networkAreas"))
				{
					return (String)this["networkAreas"];
				}
				return string.Empty;
			}
			set
			{
				this["networkAreas"] = value;
			}
		}

		[ConfigurationProperty("serverNames", IsRequired = false)]
		public String ServerNames
		{
			get
			{
				if (this.Properties.Contains("serverNames"))
				{
					return (String)this["serverNames"];
				}
				return string.Empty;
			}
			set
			{
				this["serverNames"] = value;
			}
		}

		[ConfigurationProperty("serviceNames", IsRequired = false)]
		public String ServiceNames
		{
			get
			{
				if (this.Properties.Contains("serviceNames"))
				{
					return (String)this["serviceNames"];
				}
				return string.Empty;
			}
			set
			{
				this["serviceNames"] = value;
			}
		}

		[ConfigurationProperty("", IsDefaultCollection = true)]
		public FieldCollection Fields
		{
			get
			{
				var fieldCollection = (FieldCollection)base[""];
				return fieldCollection;
			}

		}

	}

	[ConfigurationCollection(typeof(EnvironmentWatchElement), AddItemName = "watch")]
	public class EDBFileWatchCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new EnvironmentWatchElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((EnvironmentWatchElement)(element)).DataFile;
		}

		/// <summary>
		/// Access the collection by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public EnvironmentWatchElement this[int index]
		{
			get { return (EnvironmentWatchElement)BaseGet(index); }
		}

		/// <summary>
		/// Access the collection by key name
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public new EnvironmentWatchElement this[string key]
		{
			get { return (EnvironmentWatchElement)BaseGet(key); }
		}
	}

}
