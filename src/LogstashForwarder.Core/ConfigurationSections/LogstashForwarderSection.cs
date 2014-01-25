using System;
using System.Configuration;

namespace LogstashForwarder.Core.ConfigurationSections
{
	public class LogstashForwarderSection : ConfigurationSection
    {
        [ConfigurationProperty("servers", IsRequired = true)]
        public String Servers
        {
            get
            {
                return (String)this["servers"];
            }
            set
            {
                this["servers"] = value;
            }
        }

        [ConfigurationProperty("ssl_ca", IsRequired = true)]
        public String SSL_CA
        {
            get
            {
                return (String)this["ssl_ca"];
            }
            set
            {
                this["ssl_ca"] = value;
            }
        }

        [ConfigurationProperty("timeout", IsRequired = true)]
        public int Timeout
        {
            get
            {
                return Convert.ToInt32(this["timeout"]);
            }
            set
            {
                this["timeout"] = value;
            }
        }

        [ConfigurationProperty("watches", IsDefaultCollection = false)]
            
        public WatchCollection Watchs
        {
            get
            {
                return (WatchCollection)base["watches"];
            }
        }

        [ConfigurationProperty("environmentWatches", IsDefaultCollection = false)]
        public EnvironmentWatchCollection EnvironmentWatches
        {
            get
            {
                return (EnvironmentWatchCollection)base["environmentWatches"];
            }
        }
    }


 
}
