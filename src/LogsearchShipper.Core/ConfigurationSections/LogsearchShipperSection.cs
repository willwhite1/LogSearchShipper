using System;
using System.Configuration;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class LogSearchShipperSection : ConfigurationSection
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

		[ConfigurationProperty("data_folder", IsRequired = true)]
		public String DataFolder
		{
			get { return (String) this["data_folder"]; }
			set { this["data_folder"] = value; }
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

		[ConfigurationProperty("shipper_service_username", IsRequired = false)]
		public String ShipperServiceUsername
		{
			get { return (String)this["shipper_service_username"]; }
			set { this["shipper_service_username"] = value; }
		}

		[ConfigurationProperty("shipper_service_password", IsRequired = false)]
		public String ShipperServicePassword
		{
			get { return (String)this["shipper_service_password"]; }
			set { this["shipper_service_password"] = value; }
		}
	}
}