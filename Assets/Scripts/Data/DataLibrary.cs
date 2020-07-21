
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Collections;

namespace Data
{

	public interface IDataLibrary
	{
		NamedDataObject _byID(int id);

		NamedDataObject _byName(string name);
	}

	/** A library of objects accessable by ID or name */
	[DataObject("Library")]
	public class DataLibrary<T> : DataObject, IDataLibrary, IEnumerable where T : NamedDataObject
	{
		/** List of all our objects */
		[FieldAttr(true)]
		protected List<T> DataList;

		[FieldAttr(true)]
		public int CurrentID = 0;

		/** If true items will be merged when loading rather than replaced */
		[FieldAttr(true)]
		public bool MergeRead = false;

		/** Causes IDs to be automatically assigned when adding entries to data library */
		[FieldAttr(true)]
		public bool AutoID = false;

		/** Objects by ID */
		// note: using Dictionary<int,T> (which would be much better) will cause a runtime exception on iOS due to
		// not having JIT.  So I just have to cast the dictionary and not use generics.
		private Dictionary<int,NamedDataObject> byID;
		private Dictionary<string,NamedDataObject> byName;

		public DataLibrary()
		{
			DataList = new List<T>();
			byID = new Dictionary<int, NamedDataObject>();
			byName = new Dictionary<string, NamedDataObject>(StringComparer.OrdinalIgnoreCase);
		}

		/** Maximum number of records this library can contain. */
		public virtual int MaxRecords
		{ get { return int.MaxValue; } }

		/** The default item from the default global library, or null if none set. */
		public static T GlobalDefault { 
			get {
				var globalLibrary = NamedDataObject.GetLibraryForType<T>();
				return globalLibrary != null ? globalLibrary.Default : null;
			}
		}

		/** Returns the default item in this library, this will just the item at index [0] unless there is an 
		entry called "_default" */
		public T Default {
			get { 

				if (Count == 0)
					throw new Exception("No default for object [" + this + "], as there are no items.");

				// For some crazy reason the following line of code:
				// 		return this["_default"] ?? this[0];
				// Generates a runtime error when using the WebPlayer build:
				// 		Error verifying Data.DataLibrary`1:get_Default (): 
				//		Argument type Complex not valid for brtrue/brfalse at 0x002d”
	
				var defaultItem = this["_default"];
				if (defaultItem == null)
					defaultItem = this[0]; 
				return defaultItem;
			}
		}

		/** Returns if this library contains given item or not. */
		public bool Contains(T item)
		{
			return DataList.Contains(item);
		}

		/** Indexer to data by index */
		public T this [int index] { 
			get { 
				if ((index < 0) || (index >= Count))
					throw new Exception(this.ClassAttributes.NodeName + ": invalid index " + index);
				return (DataList[index]); 
			}
		}

		/** Indexer to data by string */
		public T this [string name] {
			get { return ByName(name); }
		}

		public void AddRange(IEnumerable<T> enumerator)
		{
			foreach (var item in enumerator)
				Add(item);
		}

		/** Adds given object to this list */
		public void Add(T obj)
		{
			if (this.Count >= MaxRecords)
				throw new Exception(String.Format("Too many record for library {0}, maximum is {1}", this, this.MaxRecords));
			if (obj != null) {
				if (AutoID)
					obj.ID = NextID();
				if (byID.ContainsKey(obj.ID)) {
					Trace.LogWarning("Data Error [Duplicate ID]: Can not add " + obj + ", its ID [" + obj.ID + "] is already taken by " + byID[obj.ID]);
					return;
				}
				DataList.Add(obj);
				byID[obj.ID] = obj;
				if (obj.Name != null)
					byName[obj.Name] = obj;
			}
		}

		/** Removes given object by index */
		public void Remove(int index)
		{
			if ((index < 0) || (index >= DataList.Count))
				throw new Exception("Can not remove object of index " + index + " out of bounds.");
			byID.Remove(this[index].ID);
			byName.Remove(this[index].Name);
			DataList.RemoveAt(index);
		}

		/** Returns the number of objects in this library */
		public int Count {
			get {
				return DataList.Count;
			}
		}

		/** Enumerator for data */
		public IEnumerator GetEnumerator()
		{
			return DataList.GetEnumerator();
		}

		/** Fetches object matching name.  returns default or null if not found */
		public T ByName(string name, T _default = null)
		{
			if (byName.ContainsKey(name))
				return (T)byName[name];
			return _default;
		}

		/** Fetches object by ID */
		public T ByID(int id)
		{
			if (byID.ContainsKey(id))
				return (T)byID[id];
			else
				return null;
		}

		/** Returns a list of the sprites contained within this library, with optional filtering. */
		public List<T> GetEntries(System.Predicate<T> match = null)
		{
			var filteredList = (match == null) ? DataList : DataList.FindAll(match);
			return new List<T>(filteredList);
		}

		// these routines are required to create a generic interface that all DataLibrary classes support.

		public NamedDataObject _byID(int id)
		{
			return ByID(id);
		}

		public NamedDataObject _byName(string name)
		{
			return ByName(name);
		}


		/** Returns the next avalaible ID to be used */
		public int NextID()
		{
			return CurrentID++;
		}

		/** Returns a copy of the data list */
		public List<T> GetListCopy()
		{
			return new List<T>(DataList);
		}

		/** Writes names of library entrys to log */
		public void ListToLog(bool logToScreen = false)
		{
			foreach (T obj in DataList) {
				Trace.Log(obj.ToString());
				if (logToScreen)
					CoM.PostMessage(obj.ToString());
			}
		}

		/** Writes contents of library to log */
		public void WriteToLog(bool logToScreen = false)
		{
			foreach (T obj in DataList) {
				Trace.Log(obj.LongDescription());
				if (logToScreen)
					CoM.PostMessage(obj.LongDescription());
			}
		}

		/** Removes all the libraries contents */
		public void Clear()
		{
			DataList.Clear();
		}

		#region Serialization implementation

		/** Write object and library contents to XML */
		override public void WriteNode(XElement node)
		{
			base.WriteNode(node);
			foreach (DataObject data in DataList) {
				node.Add(data.CreateNode());
			}
		}

		/** Read object and library contents from XML */
		override public void ReadNode(XElement node)
		{
			base.ReadNode(node);

			int maxID = 0;

			if (!MergeRead)
				Clear();

			foreach (XElement subNode in node.Elements()) {
			
				int objID = (subNode.Attribute("ID") == null) ? -1 : int.Parse(subNode.Attribute("ID").Value);

				if (MergeRead && (this[objID]) != null) {
					this[objID].ReadNode(subNode); 
				} else {
					T obj = (T)Activator.CreateInstance(typeof(T));
					obj.ReadNode(subNode); 
					this.Add(obj);

				}

				if (objID > maxID)
					maxID = objID;

			}
			CurrentID = maxID + 1;
		}

		#endregion

	}
}
