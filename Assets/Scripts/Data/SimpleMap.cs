using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Data
{
	public delegate void ValueChangedEvent(string name,string value);
	public delegate bool CanAcceptValueEvent(string name,string newValue);

	/** A simple dictionary that maps from one string to another */
	[DataObjectAttribute("Dictionary", true)]
	public class SimpleMap : DataObject
	{
		/** Called whenever a setting is changed or added */
		public ValueChangedEvent OnValueChanged;
		
		/** Called before a setting is changed, can be used to cancel update. */
		public CanAcceptValueEvent OnValidateValue;
		
		protected Dictionary<string,string> Dictionary;

		protected string KeyFieldName = "key";
		protected string ValueFieldName = "value";
		protected string EntryName = "Entry";

		public SimpleMap()
		{
			Dictionary = new Dictionary<string, string>();
		}

		public SimpleMap(string keyFieldName, string valueFieldName, string entryName)
			: this()
		{
			KeyFieldName = keyFieldName;
			ValueFieldName = valueFieldName;
			EntryName = entryName;
		}

		/** Creates a new simple map, loads it, and returns the result */
		public static SimpleMap Create(string keyFieldName, string valueFieldName, string entryName, XElement node)
		{
			var map = new SimpleMap(keyFieldName, valueFieldName, entryName);
			map.Load(node);
			return map;
		}

		public void Load(XElement node)
		{
			ReadNode(node);
		}

		/** Indexer to values by key */
		public string this [string key] { 
			get { return Lookup(key); }
			set { SetValue(key, value); }
		}

		/**
		 * Looks up key in dictionary, returns value or an empty string if not found. 
		 */
		public string Lookup(string key)
		{
			if (Dictionary.ContainsKey(key))
				return (Dictionary[key]);
			return "";
		}

		/**
		 * Looks up key in dictionary, returns value or defaultValue if not found. 
		 */
		public int LookupInt(string key, int defaultValue = 0)
		{
			string value = Lookup(key);
			if (value == "")
				return defaultValue;

			int result;
			return int.TryParse(value, out result) ? result : defaultValue;
		}

		/**
		 * Looks up key in dictionary, returns value or defaultValue if not found. 
		 */
		public int LookupInt(int key, int defaultValue = 0)
		{
			return LookupInt(key.ToString(), defaultValue);
		}


		public float LookupFloat(string key, float defaultValue = 0.0f)
		{
			string value = Lookup(key);
			if (value == "")
				return defaultValue;
			else
				return float.Parse(value);
		}

		public string LookupString(string key, string defaultValue = "")
		{
			string value = Lookup(key);
			return value ?? defaultValue;
		}

		public bool LookupBool(string key, bool defaultValue = false)
		{
			string value = Lookup(key);
			if (value == "")
				return defaultValue;
			else
				return bool.Parse(value);
		}

		public DateTime LookupDateTime(string key, DateTime defaultValue = default(DateTime))
		{
			string value = Lookup(key);
			if (value == null)
				return defaultValue;
			return DateTime.Parse(value);
		}

		/**
		 * Adds a key / value pair to dictionary.
		 * If key already exists the value is just modified.
		 */
		virtual public void SetValue(string key, string value)
		{
			if (Dictionary.ContainsKey(key)) {
				if (Dictionary[key] == value)
					return;
				if ((OnValidateValue != null) && (!OnValidateValue(key, value)))
					return;
				
				Dictionary[key] = value;
				
				if (OnValueChanged != null)
					OnValueChanged(key, value);
			} else {
				Dictionary.Add(key, value);
			}
		}

		public void SetValue(string key, bool value)
		{
			SetValue(key, value.ToString());
		}

		public void SetValue(string key, int value)
		{
			SetValue(key, value.ToString());
		}

		public void SetValue(string key, float value)
		{
			SetValue(key, value.ToString());
		}

		public void SetValue(string key, DateTime value)
		{
			SetValue(key, value.ToString("o"));
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			foreach (KeyValuePair<string,string> entry in Dictionary) {
				XElement entryNode = new XElement(EntryName);
				WriteAttribute(entryNode, KeyFieldName, entry.Key);
				WriteAttribute(entryNode, ValueFieldName, entry.Value);
				node.Add(entryNode);
			}
		}

		/**
		 * Reads key value pairs from node 
		 */
		public override void ReadNode(XElement node)
		{			
			if (node == null)
				throw new Exception("node is null");

			foreach (XElement subNode in node.Elements(EntryName)) {
				string key = ReadAttribute(subNode, KeyFieldName);
				string value = ReadAttribute(subNode, ValueFieldName);

				SetValue(key, value);
			}
		}

		#endregion
	}
}
