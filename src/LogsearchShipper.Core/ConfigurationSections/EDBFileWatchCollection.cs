using System;
using System.Configuration;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class EnvironmentWatchElement : ConfigurationElement
	{
		[ConfigurationProperty("dataFile", IsKey = true, IsRequired = true)]
		public String DataFile
		{
			get { return (String) this["dataFile"]; }
			set { this["dataFile"] = value; }
		}

		[ConfigurationProperty("logEnvironmentDiagramDataEveryMinutes", IsRequired = false)]
		public int LogEnvironmentDiagramDataEveryMinutes
		{
			get
			{
				if (Properties.Contains("logEnvironmentDiagramDataEveryMinutes"))
				{
					return (int) this["logEnvironmentDiagramDataEveryMinutes"];
				}
				return 60;
			}
			set { this["logEnvironmentDiagramDataEveryMinutes"] = value; }
		}

		[ConfigurationProperty("networkAreas", IsRequired = false)]
		public String NetworkAreas
		{
			get
			{
				if (Properties.Contains("networkAreas"))
				{
					return (String) this["networkAreas"];
				}
				return string.Empty;
			}
			set { this["networkAreas"] = value; }
		}

		[ConfigurationProperty("serverNames", IsRequired = false)]
		public String ServerNames
		{
			get
			{
				if (Properties.Contains("serverNames"))
				{
					return (String) this["serverNames"];
				}
				return string.Empty;
			}
			set { this["serverNames"] = value; }
		}

		[ConfigurationProperty("serviceNames", IsRequired = false)]
		public String ServiceNames
		{
			get
			{
				if (Properties.Contains("serviceNames"))
				{
					return (String) this["serviceNames"];
				}
				return string.Empty;
			}
			set { this["serviceNames"] = value; }
		}

		[ConfigurationProperty("", IsDefaultCollection = true)]
		public FieldCollection Fields
		{
			get
			{
				var fieldCollection = (FieldCollection) base[""];
				return fieldCollection;
			}
		}
	}

	[ConfigurationCollection(typeof (EnvironmentWatchElement), AddItemName = "watch")]
	public class EDBFileWatchCollection : ConfigurationElementCollection
	{
		/// <summary>
		///  Access the collection by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public EnvironmentWatchElement this[int index]
		{
			get { return (EnvironmentWatchElement) BaseGet(index); }
		}

		/// <summary>
		///  Access the collection by key name
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public new EnvironmentWatchElement this[string key]
		{
			get { return (EnvironmentWatchElement) BaseGet(key); }
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new EnvironmentWatchElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((EnvironmentWatchElement) (element)).DataFile;
		}
	}
}