using System;
using System.Configuration;

namespace LogstashForwarder.Core.ConfigurationSections
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

        [ConfigurationProperty("environmentNames", IsRequired = false)]
        public String EnvironmentNames
        {
            get
            {
                if (this.Properties.Contains("environmentNames"))
                {
                    return (String)this["environmentNames"];
                }
                return string.Empty;
            }
            set
            {
                this["environmentNames"] = value;
            }
        }

        [ConfigurationProperty("serverGroupNames", IsRequired = false)]
        public String ServerGroupNames
        {
            get
            {
                if (this.Properties.Contains("serverGroupNames"))
                {
                    return (String)this["serverGroupNames"];
                }
                return string.Empty;
            }
            set
            {
                this["serverGroupNames"] = value;
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

    [ConfigurationCollection(typeof(EnvironmentWatchElement), AddItemName = "environmentWatch")]
    public class EnvironmentWatchCollection : ConfigurationElementCollection
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
