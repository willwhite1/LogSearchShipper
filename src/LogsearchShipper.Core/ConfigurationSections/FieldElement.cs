using System;
using System.Configuration;

namespace LogSearchShipper.Core.ConfigurationSections
{
	public class FieldElement : ConfigurationElement
	{
		[ConfigurationProperty("key", IsRequired = true)]
		public String Key
		{
			get { return (String) this["key"]; }
			set { this["key"] = value; }
		}

		[ConfigurationProperty("value", IsRequired = true)]
		public String Value
		{
			get { return (String) this["value"]; }
			set { this["value"] = value; }
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", Key, Value);
		}
	}

	public class FieldCollection : ConfigurationElementCollection
	{
		public FieldCollection()
		{
			var field = (FieldElement) CreateNewElement();
			if (field.Key != "")
			{
				Add(field);
			}
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get { return ConfigurationElementCollectionType.BasicMap; }
		}

		public FieldElement this[int index]
		{
			get { return (FieldElement) BaseGet(index); }
			set
			{
				if (BaseGet(index) != null)
				{
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}

		public new FieldElement this[string name]
		{
			get { return (FieldElement) BaseGet(name); }
		}

		protected override string ElementName
		{
			get { return "field"; }
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new FieldElement();
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((FieldElement) element).Key;
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
	}
}