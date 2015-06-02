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

		[ConfigurationProperty("sessionId", IsRequired = false)]
		public string SessionId
		{
			get { return (string)this["sessionId"]; }
			set { this["sessionId"] = value; }
		}

		[ConfigurationProperty("processor_usage_reporting_interval_seconds", IsRequired = false, DefaultValue = 60.0d)]
		public double ProcessorUsageReportingIntervalSeconds
		{
			get { return (double)this["processor_usage_reporting_interval_seconds"]; }
			set { this["processor_usage_reporting_interval_seconds"] = value; }
		}

		[ConfigurationProperty("file_poll_interval_seconds", IsRequired = false, DefaultValue = 5.0d)]
		public double FilePollIntervalSeconds
		{
			get { return (double)this["file_poll_interval_seconds"]; }
			set { this["file_poll_interval_seconds"] = value; }
		}

		[ConfigurationProperty("output_file", IsRequired = false)]
		public string OutputFile
		{
			get { return (string)this["output_file"]; }
			set { this["output_file"] = value; }
		}

		[ConfigurationProperty("resolve_unc_paths", IsRequired = false, DefaultValue = false)]
		public bool ResolveUncPaths
		{
			get { return (bool)this["resolve_unc_paths"]; }
			set { this["resolve_unc_paths"] = value; }
		}
	}
}