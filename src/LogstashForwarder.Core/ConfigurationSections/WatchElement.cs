using System;
using System.Configuration;

namespace LogstashForwarder.Core.ConfigurationSections
{
    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("files", IsKey = true, IsRequired = true)]
        public String Files
        {
            get
            {
                return (String)this["files"];
            }
            set
            {
                this["files"] = value;
            }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public String Type
        {
            get
            {
                return (String)this["type"];
            }
            set
            {
                this["type"] = value;
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

    [ConfigurationCollection(typeof(WatchElement), AddItemName = "watch")]
    public class WatchCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new WatchElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WatchElement)(element)).Files;
        }

        /// <summary>
        /// Access the collection by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public WatchElement this[int index]
        {
            get { return (WatchElement)BaseGet(index); }
        }

        /// <summary>
        /// Access the collection by key name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new WatchElement this[string key]
        {
            get { return (WatchElement)BaseGet(key); }
        }
    }
}