using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class WinEventWatchElement : ConfigurationElement, IWatchElement
	{
		[ConfigurationProperty("path", IsKey = true, IsRequired = true)]
		public string Path
		{
			get { return (string)this["path"]; }
			set { this["path"] = value; }
		}

		[ConfigurationProperty("query", IsKey = true, IsRequired = true)]
		public string Query
		{
			get { return (string)this["query"]; }
			set { this["query"] = value; }
		}

		[ConfigurationProperty("readFromLast", IsRequired = false, DefaultValue = true)]
		public bool ReadFromLast
		{
			get { return (bool)this["readFromLast"]; }
			set { this["readFromLast"] = value; }
		}

		public string Key { get { return Path + "_" + Query; } }

		private FieldCollection _fieldCollection;

		[ConfigurationProperty("", IsDefaultCollection = true)]
		public FieldCollection Fields
		{
			get
			{
				if (_fieldCollection == null)
				{
					_fieldCollection = (FieldCollection)base[""];
				}
				return _fieldCollection;
			}
			set { _fieldCollection = value; }
		}

		public override string ToString()
		{
			return string.Format("{0}", Key);
		}
	}
}
