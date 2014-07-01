using System;
using System.Configuration;

namespace LogsearchShipper.Core.ConfigurationSections
{
	public class LogsearchShipperSection : ConfigurationSection
	{
		[ConfigurationProperty("ingestor_host", IsRequired = true)]
		public String IngestorHost
		{
			get { return (String) this["ingestor_host"]; }
			set { this["ingestor_host"] = value; }
		}

		[ConfigurationProperty("ingestor_port", IsRequired = true)]
		public int IngestorPort
		{
			get { return Convert.ToInt32(this["ingestor_port"]); }
			set { this["ingestor_port"] = value; }
		}

		[ConfigurationProperty("ssl_ca", IsRequired = true)]
		public String SSL_CA
		{
			get { return (String) this["ssl_ca"]; }
			set { this["ssl_ca"] = value; }
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