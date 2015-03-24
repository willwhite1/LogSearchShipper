using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class OverrideConfig : ConfigurationElement
	{
		[ConfigurationProperty("forServiceNames", IsRequired = true, IsKey = true)]
		public string ForServiceNames
		{
			get { return (string)this["forServiceNames"]; }
			set { this["forServiceNames"] = value; }
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
	}

	[ConfigurationCollection(typeof(OverrideConfig), AddItemName = "overrideConfig")]
	public class OverrideConfigCollection : ConfigurationElementCollection, IEnumerable<OverrideConfig>
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new OverrideConfig();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((OverrideConfig)element).ForServiceNames;
		}

		public new IEnumerator<OverrideConfig> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return BaseGet(i) as OverrideConfig;
			}
		}
	}
}
