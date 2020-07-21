using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Reflection;
using System.Resources;
using System.ComponentModel;
using UnityEngine;

[System.AttributeUsage(AttributeTargets.Class)]
public class DataObjectAttribute : Attribute
{
	public string NodeName;
	public bool AutoSerialize;
	/** Version used during serialization */
	public float Version;

	public DataObjectAttribute(string nodeName, float version = 1.0f, bool autoSerialize = false)
	{
		this.NodeName = nodeName;
		this.Version = version;
		this.AutoSerialize = autoSerialize;
	}

	public DataObjectAttribute(string nodeName, bool autoSerialize)
		: this(nodeName, 1.0f, autoSerialize)
	{
	}

	public static DataObjectAttribute Default {
		get { return new DataObjectAttribute("Node", 1.0f, false); }
	}
}


namespace Data
{
	/** Stores per object properites for a DataObject */
	public class DataObjectProperties
	{
		public Dictionary<string,string> _properties;

		public DataObjectProperties()
		{
			_properties = new Dictionary<string, string>();
		}

		public void SetProperty(string name, object value)
		{
			_properties[name] = value.ToString();
		}

		/** Returns a list of all the property names stored in this property list */
		public List<string> GetPropertyNames()
		{
			return new List<string>(_properties.Keys);
		}

		/** Returns property of given name or null if no property matches that name */
		public string GetProperty(string name)
		{
			return _properties.ContainsKey(name) ? _properties[name] : null;
		}

		/** Returns property of given name or default if no property matches that name */
		public int GetPropertyInt(string name, int defaultValue = 0)
		{
			var result = GetProperty(name);
			return String.IsNullOrEmpty(result) ? defaultValue : int.Parse(result);
		}

		/** Indexer to properties by name */
		public string this [string name] {
			get { return GetProperty(name); }
			set { SetProperty(name, value); }
		}

	}



	/** A named data object with an ID */
	public class NamedDataObject : DataObject
	{
		/** Dictionary to lookup the global library for a given type of NamedDataObject */
		public static Dictionary<Type, IDataLibrary> GlobalLibrary = new Dictionary<Type, IDataLibrary>();

		/** Unique ID for this object */
		public int ID;
		
		/** The name of this object */
		public string Name;

		/** Used for generic per object properties */
		[FieldAttr(true)]
		public DataObjectProperties Property;

		public NamedDataObject()
		{
			ID = -1;
			Property = new DataObjectProperties();
		}

		public NamedDataObject(int id, string name = "")
			: this()
		{
			ID = id;
			Name = name;
		}

		#region Serialization implementation

		/** Writes object to given XML node */
		override public void WriteNode(XElement node)
		{
			if (ID != -1)
				node.SetAttributeValue("ID", ID);
			if (!String.IsNullOrEmpty(Name))
				node.SetAttributeValue("Name", Name);
		
			// write all properties as attributes
			foreach (var key in Property.GetPropertyNames()) {
				node.SetAttributeValue(key, Property[key]);
			}
				
			base.WriteNode(node);
		}

		override public void ReadNode(XElement node)
		{
			base.ReadNode(node);

			ID = ReadAttributeInt(node, "ID", -1);
			if (node.Attribute("Name") != null)
				Name = ReadAttribute(node, "Name");

			// load all attributes (excluding id, name and version) as properties
			foreach (var attribute in node.Attributes()) {
				if ((attribute.Name.LocalName == "ID") || (attribute.Name.LocalName == "Name") || (attribute.Name.LocalName == "Version"))
					continue;
				Property[attribute.Name.LocalName] = attribute.Value;
			}

		}

		#endregion

		/** Returns the global library associated with the given type. */ 
		public static DataLibrary<T> GetLibraryForType<T>() where T : NamedDataObject
		{
			return (DataLibrary<T>)GetLibraryForType(typeof(T));
		}

		/** Returns the global library associated with the given type. */ 
		public static IDataLibrary GetLibraryForType(Type type)
		{
			return GlobalLibrary.ContainsKey(type) ? GlobalLibrary[type] : null;
		}

		/** Returns the global library (if any) associated with this type of NamedNamedObject */
		public DataLibrary<T> GetGlobalLibrary<T>() where T: NamedDataObject
		{
			return GetLibraryForType<T>();
		}

		/** 
		 * Adds a reference linking given library to given NamedDataObject type.  This use used to link up objects
		 * during deserialization. 
		 */
		public static void AddGlobalLibrary<T>(DataLibrary<T> library) where T:NamedDataObject
		{
			GlobalLibrary[typeof(T)] = library;
		}


		/** Converts object to text */
		public override string ToString()
		{
			return Name;
		}

	}

	/** Interface for reading / writing data objects */
	interface iSerializable
	{
		/** Writes object to given XML node */
		void WriteNode(XElement node);

		/** Reads object from given XML node */
		void ReadNode(XElement node);
	}

	/** Ancestor for objects that can read and write to disk, descendants must implement "WriteNode" and "ReadNode" */
	abstract public class DataObject : System.Object, iSerializable
	{
		/** The version of the data in this object.  Will only be set if there is a 'version' attribute when reading
		 * in the node.  Which isn't saved by default.  Therefore will often default to 0.0f; */
		[FieldAttr(true)]
		public float Version;

		protected DataObject()
		{

		}

		#region Auto Serialize

		/**
		 * Writes all public memebers to node 
		 */
		protected void AutoWriteSerialize(XElement node)
		{
			FieldInfo[] fields = this.GetType().GetFields();
			List<String> ignoredFields = GetIgnoredFields();
			foreach (var field in fields) {
				if (field.IsStatic)
					continue;
				try {
					var attr = field.ExtendedAttributes();
					if (attr.Ignore)
						continue;
					if (ignoredFields.Contains(field.Name))
						continue;

					WriteValue(node, attr.DisplayName ?? field.Name, field.GetValue(this), attr.ReferenceType);
				} catch (Exception e) {
					Trace.Log("Could not write field " + field.Name + ": " + e.Message);
				}
			}
		}

		/**
		 * Reads all public memebers from node 
		 * forceNulls: If true null values will overwrite existing data.  The default behaviour is that null fields leave data as exists.
		 */
		protected void AutoReadSerialize(XElement node, bool forceNulls = false)
		{
			FieldInfo[] fields = this.GetType().GetFields();
			List<String> ignoredFields = GetIgnoredFields();

			foreach (var field in fields) {
				if (field.IsStatic)
					continue;
				try {
					var attr = field.ExtendedAttributes();
					if (attr.Ignore)
						continue;
					if (ignoredFields.Contains(field.Name))
						continue;

					object value = null;

					switch (attr.ReferenceType) {
						case FieldReferenceType.Full: 
							value = ReadObject(node, attr.DisplayName ?? field.Name, field.FieldType);
							break;
						case FieldReferenceType.ID: 
						case FieldReferenceType.Name:
							if (!field.FieldType.IsSubclassOf(typeof(NamedDataObject)))
								throw new Exception("Referenced types must be NamedDataObjects");

							var library = NamedDataObject.GetLibraryForType(field.FieldType);

							if (library == null)
								throw new Exception("No global library found for referenced type [" + field.FieldType + "]");

							value = ReadObject(node, attr.DisplayName ?? field.Name, field.FieldType, attr.ReferenceType, library);
							break;
					}
					if ((value != null) || forceNulls)
						field.SetValue(this, value);
				} catch (Exception e) {
					Trace.LogWarning("Error reading object [" + field.Name + "] " + e.Message + "\n" + node);
				}
			}
		}

		#endregion


		#region Read

		/** 
		 * Gets child node by name, 
		 * If child node not found logs warning and returns null
		 */
		protected XElement GetNode(XElement node, string name)
		{
			if (node.Element(name) != null)
				return node.Element(name);
			else {
				Trace.LogWarning("Node not found '" + name + "'");
				return null;
			}
		}

		/** Reads a string from the nodes attributes, returns defaultValue if not found */
		protected string ReadAttribute(XElement node, string name, string defaultValue = "")
		{
			return (node.Attribute(name) != null) ? node.Attribute(name).Value : defaultValue;
		}

		/** Reads a bool from the nodes attributes, returns defaultValue if not found */
		protected bool ReadAttributeBool(XElement node, string name, bool defaultValue = false)
		{
			string value = ReadAttribute(node, name);
			return (value == "") ? defaultValue : bool.Parse(value);
		}

		/** Reads an int from the nodes attributes, returns defaultValue if not found */
		protected int ReadAttributeInt(XElement node, string name, int defaultValue = 0)
		{
			string value = ReadAttribute(node, name);
			return (value == "") ? defaultValue : int.Parse(value);
		}

		/** Reads an int from the nodes attributes, returns defaultValue if not found */
		protected float ReadAttributeFloat(XElement node, string name, float defaultValue = 0f)
		{
			string value = ReadAttribute(node, name);
			return (value == "") ? defaultValue : float.Parse(value);
		}

		/** Reads an int from the nodes attributes, returns defaultValue if not found */
		protected T ReadAttributeEnum<T>(XElement node, string name, T defaultValue = default(T))
		{
			string value = ReadAttribute(node, name);
			return (value == "") ? defaultValue : (T)Enum.Parse(typeof(T), value);

		}

		/** 
		 * Reads a value from this nodes children, returns defaultValue if not found.  An empty name string reads
		 * from given node, otherwise reads from node.[name] 
		 */
		protected string ReadValue(XElement node, string name = "", string defaultValue = "")
		{
			if (String.IsNullOrEmpty(name))
				return node.Value;
			if (node.Element(name) == null)
				return defaultValue;
			string value = node.Element(name).Value;
			return value ?? "";
		}

		/** Reads an bool from this nodes children, returns defaultValue if not found */
		protected bool ReadBool(XElement node, string name, bool defaultValue = false)
		{
			string value = ReadValue(node, name, defaultValue.ToString());
			return (value == "") ? defaultValue : bool.Parse(value);
		}

		/** Reads an int from this nodes children, returns defaultValue if not found */
		protected int ReadInt(XElement node, string name, int defaultValue = 0)
		{
			string value = ReadValue(node, name);
			value = value.Replace(",", "");
			return (value == "") ? defaultValue : int.Parse(value);
		}

		/** Reads a float from this nodes children, returns defaultValue if not found */
		protected float ReadFloat(XElement node, string name, float defaultValue = 0)
		{
			string value = ReadValue(node, name);
			value = value.Replace(",", "");
			return (value == "") ? defaultValue : float.Parse(value);
		}

		/** Reads a color from this nodes children, returns defaultValue if not found */
		protected Color ReadColor(XElement node, string name, Color defaultValue = default(Color))
		{
			string value = ReadValue(node, name);
			return (value == "") ? defaultValue : Util.ParseColor(value);
		}

		/** Reads a datetime from this nodes children, returns default value if not found */
		protected DateTime ReadDateTime(XElement node, string name, DateTime defaultValue = default(DateTime))
		{
			string value = ReadValue(node, name);
			return (value == "") ? defaultValue : DateTime.Parse(value);
		}

		protected T ReadEnum<T>(XElement node, string name, T defaultValue = default(T))
		{
			return (T)ReadEnum(node, typeof(T), name, defaultValue.ToString());
		}

		/** Reads enum type from this nodes children.  This is the non generic version */
		public object ReadEnum(XElement node, Type enumType, string name, string defaultValue = null)
		{
			return Enum.Parse(enumType, ReadValue(node, name, defaultValue));
		}

		/** Reads a bit array from this nodes children */
		public BitArray ReadBitArray(XElement node, string name)
		{
			string value = ReadValue(node, name);
			BitArray result = new BitArray(value.Length);
			for (int lp = 0; lp < result.Length; lp++) {
				result.Set(lp, value[lp] == '1');
			}
			return result;
		}

		/** 
		 * Reads an array ulongfrom this node, or this nodes child of given name. 
		 * returns defaultValue if child node not found. 
		 */
		protected ulong[] ReadArrayULong(XElement node, string name = "", ulong[] defaultValue = null)
		{
			// Special case for ulong because iOS has problems with a uint64 converter.

			if (defaultValue == null)
				defaultValue = new ulong[0];

			string value = ReadValue(node, name, "");

			if (value == "")
				return defaultValue;

			string[] values = value.Split(',');

			ulong[] result = new ulong[values.Length];

			for (int lp = 0; lp < result.Length; lp++)
				result[lp] = ulong.Parse(values[lp]);
		
			return result;

		}

		/** Reads an array primative or string types from this node, or this nodes child of given name. 
		 * returns defaultValue if child node not found. */
		protected T[] ReadArray<T>(XElement node, string name = "", T[] defaultValue = null) where T : struct
		{
			// Note: I used to use (T)Convert.ChangeType(stringValue, typeof(T)) for the conversion, however
			// that requires JIT and therefore failed on the iPhone.

			if (defaultValue == null)
				defaultValue = new T[0];
				
			string value = ReadValue(node, name, "");

			if (value == "")
				return defaultValue;

			string[] values = value.Split(',');

			// new method to create array, uses less reflection and might work better on IOS (with no JIT)
			T[] result = new T[values.Length];

			var parseMethod = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });

			if (parseMethod == null)
				throw new Exception("No converter for array of type " + typeof(T) + ".");
			
			for (int lp = 0; lp < result.Length; lp++)
				result[lp] = (T)parseMethod.Invoke(null, new object[] { values[lp] });

			return result;
		}

		/** Reads a list of dataobjects from this nodes child of given name, returns null if not found. */
		protected List<T> ReadDataObjectList<T>(XElement node, string name) where T: DataObject
		{
			node = node.Element(name);

			if (node == null)
				return null;

			var list = new List<T>();
			string subNodeName = GetClassAttributesForType(typeof(T)).NodeName;

			foreach (var subNode in node.Elements(subNodeName)) {
				T item = (T)Activator.CreateInstance(typeof(T));
				item.ReadNode(subNode);			
				list.Add(item);
			}

			return list;
		}

		/** Reads a list of dataobjects from this nodes child of given name, returns null if not found.  Can be referenced, or full. */
		protected List<T> ReadNamedDataObjectList<T>(XElement node, string name, FieldReferenceType referenceType, DataLibrary<T> library = null) where T: NamedDataObject
		{
			node = node.Element(name);

			if (node == null)
				return null;

			var list = new List<T>();
			string subNodeName = GetClassAttributesForType(typeof(T)).NodeName;

			library = library ?? NamedDataObject.GetLibraryForType<T>();

			if (library == null && referenceType != FieldReferenceType.Full) {
				throw new Exception("Can not load type " + typeof(T) + " as no default library is defined, and it is stored referenced.");
			}
							
			foreach (var subNode in node.Elements(subNodeName)) {

				T item = null;

				switch (referenceType) {
					case FieldReferenceType.Full:
						item = (T)Activator.CreateInstance(typeof(T));
						item.ReadNode(subNode);
						break;
					case FieldReferenceType.ID:
						var fieldId = int.Parse(subNode.Value);
						item = (T)library._byID(fieldId);							
						break;
					case FieldReferenceType.Name:
						var fieldName = subNode.Value;
						item = (T)library._byName(fieldName);				
						break;
				}

				list.Add(item);
			}

			return list;
		}

		/**
		 * Loads a given object from node. 
		 * If the node contains a reference (i.e. an id or name but not the full object) then specify the reference type,
		 * and library to search.
		 * 
		 * Supported types.
		 * 
		 * Primatives and strings
		 * Data objects (referenced and full)
		 * Arrays of primatives or strings
		 * 
		 * Not implement yet: Lists of primatives, Lists or arrays of dataObjects.
		 * 
		 */
		protected object ReadObject(XElement node, string name, Type type, FieldReferenceType referenceType = FieldReferenceType.Full, IDataLibrary library = null)
		{
			XElement sourceNode = (node.Element(name));

			if (sourceNode == null)
				return null;

			// Primatives

			if (type.IsEnum)
				return ReadEnum(node, type, name);

			if (type == typeof(Color))
				return ReadColor(node, name);			

			if (type == typeof(bool))
				return ReadBool(node, name);
			if (type.IsIntType())
				return ReadInt(node, name);
			if (type == typeof(float))
				return ReadFloat(node, name);
			if (type == typeof(string))
				return ReadValue(node, name);
			if (type == typeof(DateTime))
				return ReadDateTime(node, name);

			// Data object.

			if (type.IsSubclassOf(typeof(DataObject))) {

				switch (referenceType) {

					case FieldReferenceType.ID:
					case FieldReferenceType.Name:

						if (!type.IsSubclassOf(typeof(NamedDataObject)))
							throw new Exception("Type must be subclass of NamedDataObject");

						if (referenceType == FieldReferenceType.ID)
							return library._byID(ReadInt(node, name));
						else
							return library._byName(ReadValue(node, name));				

					case FieldReferenceType.Full:

						var result = Activator.CreateInstance(type);
						(result as DataObject).ReadNode(sourceNode);
						return result;		
				}
			}

			// Serializable
			if (typeof(iSerializable).IsAssignableFrom(type)) {
				var result = Activator.CreateInstance(type);
				(result as iSerializable).ReadNode(sourceNode);
				return result;		
			}

			// Arrays.

			if (type.IsArray) {
				switch (Type.GetTypeCode(type.GetElementType())) {
					case TypeCode.Boolean:
						return ReadArray<bool>(node, name);
					case TypeCode.Byte:
						return ReadArray<byte>(node, name);
					case TypeCode.Char:
						return ReadArray<char>(node, name);
					case TypeCode.DateTime:
						return ReadArray<DateTime>(node, name);
					case TypeCode.Double:
						return ReadArray<double>(node, name);
					case TypeCode.Single:
						return ReadArray<Single>(node, name);
					case TypeCode.Int16:
						return ReadArray<Int16>(node, name);
					case TypeCode.Int32:
						return ReadArray<Int32>(node, name);
					case TypeCode.Int64:
						return ReadArray<Int64>(node, name);
					case TypeCode.UInt16:
						return ReadArray<UInt16>(node, name);
					case TypeCode.UInt32:
						return ReadArray<UInt32>(node, name);
					case TypeCode.UInt64:
						return ReadArrayULong(node, name);
				}
			}

			// Try other types.

			if (type == typeof(BitArray))
				return ReadBitArray(node, name);

			if (type.IsGenericList())
				throw new Exception("Sorry, auto loading for generic lists are not yet supported for [" + type + "].");
				
			throw new Exception("Can not read object of type " + type + " as it is not a known type.");
		}

		/** Reads an object of given type from node */
		protected T ReadDataObject<T>(XElement node, string name, T defaultValue) where T:DataObject
		{
			T obj = ReadDataObject<T>(node, name);
			return obj ?? defaultValue;
		}

		/** Reads a value from XML, returns the newly created object, or default if it doesn't exist */
		protected T ReadDataObject<T>(XElement node, string name) where T:DataObject
		{
			XElement sourceNode = (node.Element(name));

			if (sourceNode == null)
				return default(T);

			T result = (T)Activator.CreateInstance(typeof(T));
			result.ReadNode(sourceNode);
			return result;		
		}

		#endregion

		#region Write

		/** Writes attribute to the writer */
		protected void WriteAttribute(XElement node, string name, string value)
		{
			node.SetAttributeValue(name, value);
		}

		/** Writes attribute to the writer */
		protected void WriteAttribute(XElement node, string name, int value)
		{
			node.SetAttributeValue(name, value.ToString());
		}

		/** Writes attribute to the writer */
		protected void WriteAttribute(XElement node, string name, float value)
		{
			node.SetAttributeValue(name, value.ToString());
		}

		/** Writes attribute to the writer */
		protected void WriteAttribute(XElement node, string name, bool value)
		{
			node.SetAttributeValue(name, value.ToString());
		}

		/** 
		 * Writes a value to the writer.  
		 * Object can be primative, string, or DataObject, or enum
		 * Or an array or list of either primative, string or DataObject.
		 * 
		 * If name is null or empty value will be written to node directly.
		 * 
		 * If byReference is true any NamedDataObjects will be referenced by name instead of writing them out in full.
		 */
		protected void WriteValue(XElement node, string name, object value, FieldReferenceType referenceType = FieldReferenceType.Full)
		{
			if (value == null)
				return;

			if (string.IsNullOrEmpty(name)) {
				WriteDirect(node, value, referenceType);
				return;
			}

			var subNode = new XElement(name);
			WriteDirect(subNode, value, referenceType);
			node.Add(subNode);
		}

		/**
		 * Writes a value direct to a node. 
		 */
		protected void WriteDirect(XElement node, object value, FieldReferenceType referenceType = FieldReferenceType.Full)
		{
			if (value == null)
				return;

			// Todo: use stringbuilder for performance.

			var valueType = value.GetType();

			// -----------------------------------------
			// single items

			if (valueType.IsPrimitive || (value is string)) {
				node.Value = value.ToString();
				return;
			}

			if (value is DateTime) {
				node.Value = ((DateTime)value).ToString("u");
				return;
			}

			// use custom dataobject serialization 
			if (value is DataObject) {
				if (value is NamedDataObject) {
					switch (referenceType) {
						case FieldReferenceType.Name:
							node.Value = (value as NamedDataObject).Name;
							return;
						case FieldReferenceType.ID:
							node.Value = (value as NamedDataObject).ID.ToString();
							return;
					}
				}
				(value as DataObject).WriteNode(node);
				return;
			}

			if (valueType.IsEnum) {
				node.Value = (value as Enum).ToString();
				return;
			}

			// check for ReadWriteable interface
			if (value is iSerializable) {
				(value as iSerializable).WriteNode(node);
				return;
			}

			// -----------------------------------------
			// arrays and lists

			// simple arrays
			if (valueType.IsArray && valueType.HasElementType && (valueType.GetElementType().IsPrimitive || valueType.GetElementType() == typeof(string))) {

				var array = (Array)value;
				var sb = new StringBuilder();

				foreach (var element in array)
					sb.Append(element + ",");

				if (sb.Length >= 1)
					sb.Remove(sb.Length - 1, 1);

				node.Value = sb.ToString();
				return;
			}				

			// array or list of data objects
			if (value is IList) {

				var list = (IList)(value);

				if (list.Count == 0)
					return;

				Type entryType = null;
				if (valueType.GetGenericArguments().Length >= 1)
					entryType = valueType.GetGenericArguments()[0];
				
				// If we know the generic type use this, otherwise just check each entry. 
				// Knowing the generic type allows nulls to be written with the correct type.
				if (entryType != null) {
					var classAttr = DataObject.GetClassAttributesForType(entryType);
					var nodeName = classAttr.NodeName;
					foreach (object entry in list)
						WriteValue(node, nodeName, entry, referenceType);					
				} else {
					foreach (object entry in list) {
						var classAttr = DataObject.GetClassAttributesForType(entry.GetType());
						var nodeName = classAttr.NodeName;
						WriteValue(node, nodeName, entry, referenceType);
					}	
				}
					
				return;
			}

			// -----------------------------------------
			// some random types

			if (value is BitArray) {
				string stringValue = "";
				foreach (bool bit in (value as BitArray))
					stringValue += bit ? "1" : "0";
				node.Value = stringValue;
				return;
			}
				
			if (value is Dictionary<string,string>) {
				foreach (KeyValuePair<string,string> entry in (value as Dictionary<String,String>)) {
					node.Add(new XElement("entry", entry.Key + ": " + entry.Value));
				}
				return;
			}

			if (value is Dictionary<string,int>) {
				foreach (KeyValuePair<string,int> entry in (value as Dictionary<String,int>)) {
					node.Add(new XElement("entry", entry.Key + ": " + entry.Value));
				}
				return;
			}

			// just convert to a string as a fall back
			Trace.LogWarning("Unknown datatype [" + valueType + "] while trying to serialize a [" + this.GetType() + "] object.");
			node.Value = value.ToString();
		}

		#endregion

		#region Serialization implementation

		/** 
		 * Creates an XML node containing this object.
		 * @param name the name of the node to create, or standard nodename for blank
		 */
		public XElement CreateNode(string name = null)
		{
			XElement node = new XElement(String.IsNullOrEmpty(name) ? ClassAttributes.NodeName : name);
			WriteNode(node);
			return node;
		}

		/** XML string for this object */
		protected string XMLString()
		{
			var node = CreateNode();
			return node.ToString();
		}

		/** Writes object to given XML node */
		virtual public void WriteNode(XElement node)
		{
			if (ClassAttributes.AutoSerialize)
				AutoWriteSerialize(node);
		}

		/** Reads object from given XML node */
		virtual public void ReadNode(XElement node)
		{
			Version = ReadAttributeFloat(node, "Version");
			if (ClassAttributes.AutoSerialize)
				AutoReadSerialize(node);
		}

		#endregion

		/** Returns a shallow memeberwise clone of given dataobject */
		new public DataObject MemberwiseClone()
		{
			return (DataObject)(base.MemberwiseClone());
		}

		/**
		 * Gives a long descrition of this object 
		 */
		virtual public string LongDescription()
		{
			return ToString();
		}


		/** Returns a list of fields that should not be included in auto serialization */
		virtual protected List<String> GetIgnoredFields()
		{ 
			var list = new List<String>();
			list.Add("ID"); 
			list.Add("Name");
			list.Add("Version");
			return list;
		}

		/** Returns the dataObject attributes for given type, or the default if there are none */
		public static DataObjectAttribute GetClassAttributesForType(Type type)
		{
			var attributes = type.GetCustomAttributes(typeof(DataObjectAttribute), true);
			if (attributes.Length == 0)
				return DataObjectAttribute.Default;
			else
				return (attributes[0] as DataObjectAttribute);
		}

		/** Returns the custom attributes for this class. */
		public DataObjectAttribute ClassAttributes {
			get { return GetClassAttributesForType(this.GetType()); }
		}

	}

}

