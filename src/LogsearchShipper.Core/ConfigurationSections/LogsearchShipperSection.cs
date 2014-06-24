using System;
using System.Configuration;

namespace LogsearchShipper.Core.ConfigurationSections
{
	public class LogsearchShipperSection : ConfigurationSection
	{
		[ConfigurationProperty("servers", IsRequired = true)]
		public String Servers
		{
			get { return (String) this["servers"]; }
			set { this["servers"] = value; }
		}

		[ConfigurationProperty("ssl_ca", IsRequired = true)]
		public String SSL_CA
		{
			get { return (String) this["ssl_ca"]; }
			set { this["ssl_ca"] = value; }
		}

		[ConfigurationProperty("timeout", IsRequired = true)]
		public int Timeout
		{
			get { return Convert.ToInt32(this["timeout"]); }
			set { this["timeout"] = value; }
		}

		[ConfigurationProperty("fileWatchers", IsDefaultCollection = false)]
		public FileWatchCollection FileWatchers
		{
			get { return (FileWatchCollection) base["fileWatchers"]; }
		}

		[ConfigurationProperty("edbFileWatchers", IsDefaultCollection = false)]
		public EDBFileWatchCollection EDBFileWatchers
		{
			get { return (EDBFileWatchCollection) base["edbFileWatchers"]; }
		}
	}
}