
using UnityEngine;

using System.Collections.Generic;
using System.Xml.Linq;

using Data;

[DataObject("SpriteLibrary")]
public class SpriteLibrary : DataLibrary<SpriteEntry>
{
	public string DefaultSpriteName = "";

	public string ResourcePath;

	/** Creates a new sprite library */
	public SpriteLibrary()
	{
		MergeRead = true;
		AutoID = true;
	}

	//------------------------------------------------
	// Creators
	//------------------------------------------------

	/**
 	 * Creates sprite library.  DefaultSprite is returned when indexer fails to find a matching sprite
 	 */
	public static SpriteLibrary FromResourcePath(string path, string defaultSprite = "")
	{
		var library = new SpriteLibrary();
		library.DefaultSpriteName = defaultSprite;
		library.AddSpritesFromResourcePath(path);
		return library;
	}

	/**
 	 * Creates sprite library from the given XML file.  
 	 */
	public static SpriteLibrary FromXML(string path)
	{
		return FromXML(Util.GetXMLResource(path));
	}

	public static SpriteLibrary FromXML(XElement node)
	{
		var library = new SpriteLibrary();
		library.ReadNode(node);
		return library;
	}

	//------------------------------------------------
	// Indexers
	//------------------------------------------------

	/** Indexer to sprite by index */
	new public Sprite this [int index] { 
		get { 
			return base[index].Sprite; 
		}
	}

	/** Indexer to sprite by string */
	new public Sprite this [string name] {
		get { return base[name].Sprite; }
	}
		
	//------------------------------------------------
	// Public
	//------------------------------------------------

	/** Adds all sprites found in given resource folder */
	private void AddSpritesFromResourcePath(string path)
	{
		foreach (var sprite in ResourceManager.GetResourcesAtPath<Sprite>(path))
			Add(new SpriteEntry(sprite));
	}

	public override void ReadNode(XElement node)
	{
		// Read from resource folder.
		ResourcePath = ReadAttribute(node, "ResourcePath");
		if (ResourcePath != "")
			AddSpritesFromResourcePath(ResourcePath);

		// Read sprite entries.
		base.ReadNode(node);
	}

	/** Returns list of sprites contained within this library, with optional filtering */
	public List<Sprite> GetSprites(System.Predicate<SpriteEntry> match = null)
	{
		var list = new List<Sprite>();
		foreach (var entry in GetEntries(match))
			list.Add(entry.Sprite);
		return list;
	}

	//------------------------------------------------
	//------------------------------------------------

}

[DataObject("Sprite", true)]
public class SpriteEntry : NamedDataObject, IDrawableSprite
{
	public Sprite Sprite { get { return _sprite; } set { setSprite(value); } }

	private Sprite _sprite;

	public SpriteEntry()
	{
	}

	public SpriteEntry(Sprite sprite)
	{
		Sprite = sprite;
	}

	private void setSprite(Sprite value)
	{
		_sprite = value;
		Name = Sprite.name;
	}
}