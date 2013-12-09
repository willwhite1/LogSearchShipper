using System;
using System.Configuration;

namespace LogstashForwarder.Core
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

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public WatchCollection Watchs
        {
            get
            {
                 var watchCollection = (WatchCollection)base[""];
                return watchCollection;
            }
           
        }

    }

    public class WatchCollection : ConfigurationElementCollection
      {
        public WatchCollection()
          {
              var watch = (WatchElement)CreateNewElement();
              if (watch.Files != "")
              {
                  Add(watch);
              }
          }
  
          public override ConfigurationElementCollectionType CollectionType
          {
              get
              {
                  return ConfigurationElementCollectionType.BasicMap;
              }
          }
  
          protected override ConfigurationElement CreateNewElement()
          {
              return new WatchElement();
          }
  
          protected override Object GetElementKey(ConfigurationElement element)
          {
              return ((WatchElement)element).Files;
          }

          public WatchElement this[int index]
          {
              get
              {
                  return (WatchElement)BaseGet(index);
              }
              set
              {
                  if (BaseGet(index) != null)
                  {
                      BaseRemoveAt(index);
                  }
                  BaseAdd(index, value);
              }
          }

          new public WatchElement this[string name]
          {
              get
              {
                  return (WatchElement)BaseGet(name);
              }
          }

          public int IndexOf(WatchElement watch)
          {
              return BaseIndexOf(watch);
          }

          public void Add(WatchElement watch)
          {
              BaseAdd(watch);
          }
          protected override void BaseAdd(ConfigurationElement element)
          {
              BaseAdd(element, false);
          }

          public void Remove(WatchElement watch)
          {
              if (BaseIndexOf(watch) >= 0)
                  BaseRemove(watch.Files);
          }
  
          public void RemoveAt(int index)
          {
              BaseRemoveAt(index);
          }
  
          public void Remove(string name)
          {
              BaseRemove(name);
          }
  
          public void Clear()
          {
              BaseClear();
          }
  
          protected override string ElementName
          {
              get { return "watch"; }
          }
      }
 
    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("files", IsRequired = true)]
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

    public class FieldElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true)]
        public String Key
        {
            get
            {
                return (String)this["key"];
            }
            set
            {
                this["key"] = value;
            }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public String Value
        {
            get
            {
                return (String)this["value"];
            }
            set
            {
                this["value"] = value;
            }
        }

    }

    public class FieldCollection : ConfigurationElementCollection
    {
        public FieldCollection()
        {
            var field = (FieldElement)CreateNewElement();
            if (field.Key != "")
            {
                Add(field);
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new FieldElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((FieldElement)element).Key;
        }

        public FieldElement this[int index]
        {
            get
            {
                return (FieldElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public FieldElement this[string name]
        {
            get
            {
                return (FieldElement)BaseGet(name);
            }
        }

        public int IndexOf(FieldElement field)
        {
            return BaseIndexOf(field);
        }

        public void Add(FieldElement field)
        {
            BaseAdd(field);
        }
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        public void Remove(FieldElement field)
        {
            if (BaseIndexOf(field) >= 0)
                BaseRemove(field.Key);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override string ElementName
        {
            get { return "field"; }
        }
    }

   
}
