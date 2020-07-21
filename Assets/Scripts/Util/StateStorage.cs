/**
 * Todo: this needs a tidy up!  Should probably change some of those string readers over to strings 
 */


using UnityEngine;

using System;
using System.Xml.Linq;
using System.IO;
using Data;

public enum StorageLocation
{
	None,
	/** Data files are stored in player prefs, limited to 1meg, but works on web player */
	PlayerPrefs,
	/** Data files are written to XML under the permanent data folder.*/
	PersistentFolder,
	/** Static files compiled with game, such as monsters etc. */
	Resource,
	/** Looks first in Player prefs, then in persistent folder, and finally in resource */
	Auto
}

public class StateStorage
{
	/** Tag to identify compressed data nodes */
	private const string COMPRESSED_TAG = "Compressed";

	public static StorageLocation SaveDataLocation {
		get {
			#if UNITY_WEBPLAYER	|| UNITY_WEBGL
			return StorageLocation.PlayerPrefs;
			#else
			return StorageLocation.PersistentFolder; 
			#endif
		}
	}

	/** Returns true if state currently stores data of given key */
	public static bool HasData(string key, StorageLocation location = StorageLocation.Auto)
	{
		switch (location) {
			case StorageLocation.Auto:
				return (findDataLocation(key) != StorageLocation.None);
			case StorageLocation.PlayerPrefs:
				return (PlayerPrefs.HasKey(key));
			case StorageLocation.PersistentFolder:
				return File.Exists(getPercistentURLFromKey(key));			                
			case StorageLocation.Resource:
				return ResourceManager.HasResource(getResourceNameFromKey(key));			                
		}
		return false;
	}

	/** Returns the version number if the data stored under this key. 0 is returned is no data is found. */
	public static float DataVersion(string key, StorageLocation location = StorageLocation.Auto)
	{
		if (!HasData(key, location))
			return 0;
		var xml = LoadXML(key, location);

		var attribute = xml.Attribute("Version");
		return (attribute != null) ? float.Parse(attribute.Value) : 0f;
	}

	/** Deletes data with given key.  Has no effect if key was not found */
	public static void DeleteData(string key)
	{
		switch (SaveDataLocation) {
			case StorageLocation.PlayerPrefs:
				PlayerPrefs.DeleteKey(key);
				break;
			case StorageLocation.PersistentFolder: 
				if (HasData(key))
					File.Delete(getPercistentURLFromKey(key));			              
				break;
			case StorageLocation.Resource:
				throw new Exception("Can not write data to resource location, read only.");
			case StorageLocation.None:
				throw new Exception("Can not write data to resource location, no destination specified.");		
		}
	}

	/** built in serialization for data object */
	private static T CreateDataObject<T>(StringReader sr)
	{
		XElement node = XElement.Load(sr);
		T result = (T)Activator.CreateInstance(typeof(T));
		(result as DataObject).ReadNode(node);
		return result;		
	}

	/** Returns if this data is tagged as compressed or not */
	private static bool isCompressed(string data)
	{
		string tag = "<" + COMPRESSED_TAG + ">";
		return data.StartsWith(tag);
	}

	/** looks for data under each storage location, returns the first one found */
	private static StorageLocation findDataLocation(string key)
	{
		if (HasData(key, StorageLocation.Resource))
			return StorageLocation.Resource;
		if (HasData(key, SaveDataLocation))
			return SaveDataLocation;				
		return StorageLocation.None;
	}

	/** Reads an object from state storage, if not found returns a default instance. */
	public static T LoadData<T>(string key, StorageLocation location = StorageLocation.Auto)
	{
		if (HasData(key, location)) {		
			string stringData;

			var dataLocation = location;
			if (dataLocation == StorageLocation.Auto)
				dataLocation = findDataLocation(key);

			switch (dataLocation) {
				case StorageLocation.PlayerPrefs:
					stringData = PlayerPrefs.GetString(key);
					break;
				case StorageLocation.PersistentFolder:
					stringData = ReadXML(getPercistentURLFromKey(key));
					break;
				case StorageLocation.Resource:
					stringData = Util.GetXMLResource(getResourceNameFromKey(key)).ToString();
					break;
				default:
					return default(T);
			}

			// handle decompression
			if (isCompressed(stringData)) {
				// strip of the compressed tag and decompress 
				stringData = stringData.Substring(COMPRESSED_TAG.Length + 2, stringData.Length - ((COMPRESSED_TAG.Length * 2) + 5));
				stringData = Compressor.Decompress(stringData);
			}
				
			if (typeof(T).IsSubclassOf(typeof(DataObject))) {				
				return CreateDataObject<T>(new StringReader(stringData));
			} else if (typeof(T) == typeof(XElement))
				return (T)(object)XElement.Load(new StringReader(stringData));
		}
	
		// not found
		Trace.Log("File {0} not found, creating default.", key);

		return (T)Activator.CreateInstance(typeof(T));
	}


	/** Reads XML file from state storage */
	public static XElement LoadXML(string key, StorageLocation location = StorageLocation.Auto)
	{
		return LoadData<XElement>(key, location);
	}

	/** 
	 * Saves an object to state storage.
	 * 
	 * @param compression If enabled object will be compressed before it is saved, and decompressed on read 
	 */
	public static void SaveData(string key, object source, bool compressed = false)
	{
		if (source == null) {
			Trace.LogWarning("Warning, tried to save null data to key: " + key + ".");
			return;
		}

		// check if this is an dataObject, if so use our built in serializer
		StringWriter sw;
		if (source is DataObject) {
			// use our own serializer
			DataObject data = (source as DataObject);
			sw = new StringWriter();
			var node = data.CreateNode();
			node.SetAttributeValue("Version", data.ClassAttributes.Version.ToString("0.0"));
			sw.Write(node);
		} else if (source is XElement) {
			/// write an XML file
			sw = new StringWriter();
			sw.Write((source as XElement).ToString());
		} else {
			throw new Exception("Can only save objects of type XML or DataObject");
		}

		// apply compression
		if (compressed) {
			var oldStringWriter = sw;
			sw = new StringWriter();
			sw.Write("<" + COMPRESSED_TAG + ">");
			sw.Write(Compressor.Compress(oldStringWriter.ToString()));
			sw.Write("</" + COMPRESSED_TAG + ">");
		}

		switch (SaveDataLocation) {
			case StorageLocation.PlayerPrefs:
				PlayerPrefs.SetString(key, sw.ToString());
				break;
			case StorageLocation.PersistentFolder:
				SaveXML(getPercistentURLFromKey(key), sw.ToString());
				break;
		}
	}

	public static void ClearData(string key)
	{
		PlayerPrefs.DeleteKey(key);
	}

	public static void CommitData()
	{
		PlayerPrefs.Save();	
	}
	
	#if UNITY_WEBPLAYER
	
	private static void SaveXML(string fileName, string xml)
	{
		throw new Exception("Writing XML to files is not supported in the webplayer version.");
	}

	private static string ReadXML(string fileName)
	{
		throw new Exception("Read XML to files is not supported in the webplayer version.");
	}







	
	





#else
	
	/** Saves an XML string to a file under the given path and filename */
	private static void SaveXML(string fileName, string xml)
	{
		
		StreamWriter writer;
		FileInfo file = new FileInfo(fileName);
		if (file.Exists)
			file.Delete();
		writer = file.CreateText();
		writer.Write(xml);
		writer.Close();
	}

	/** Reads XML string from a file under the persistent data path */
	private static string ReadXML(string fileName)
	{
		StreamReader reader;
		FileInfo file = new FileInfo(fileName);
		if (!file.Exists)
			return "";
		reader = file.OpenText();
		string result = reader.ReadToEnd();
		reader.Close();
		return result;
	}

	#endif

	/** Returns a filename to use for given key.  Includes path and extention */
	private static string getPercistentURLFromKey(string key)
	{
		return getDataPath() + Path.DirectorySeparatorChar + key + ".xml";
	}

	/** Returns path to resource for given key.  Includes path and extention */
	private static string getResourceNameFromKey(string key)
	{
		return "Data/" + key;
	}

	/** Returns the path used to read and write persistent files to */
	private static string getDataPath()
	{
		return Application.persistentDataPath;
	}

}
