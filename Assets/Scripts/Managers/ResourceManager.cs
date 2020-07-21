using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/** Handles graphics resources, including swapping between normal and 2x versions */
[ExecuteInEditMode]
public class ResourceManager : MonoBehaviour
{
	public string ResourcePath;

	/** If true @x2 versions will be included in the build. */
	[HideInInspector]
	[SerializeField]
	private bool _include2x;

	public static bool Include2x {
		get { return Instance == null ? false : Instance._include2x; }
		set {			
			if (Instance == null)
				return;			
			Instance._include2x = value;
		}
	}

	/** List of the sprites we are managing. */
	public List<Sprite> SpriteList;

	private static bool _usingHighDPIGui;

	public static bool UsingHighDPIGui {
		get { return _usingHighDPIGui; }
		set {
			if (_usingHighDPIGui != value)
				setHighDPIGuiGraphics(value);
		}
	}

	public static void UpdateHighDPI()
	{
		setHighDPIGuiGraphics(UsingHighDPIGui);
	}


	/** Sprites by path. */
	[SerializeField]
	internal Dictionary<string,Sprite> SpriteByPath;

	public static ResourceManager Instance;

	void Start()
	{		
		_usingHighDPIGui = false;
		setup();
		// make sure resources are in a clean state (i.e. set to low dpi) at startup. 
		if (Engine.Instance != null)
			setHighDPIGuiGraphics(false);
	}

	void Update()
	{
		if (Instance != this)
			setup();		
	}

	private void setup()
	{
		Instance = this;
		updateDictionary(ResourcePath);

	}

	private static bool Initialized {
		get {
			return Instance != null;
		}
	}

	private static bool hasManagedSprite(string path)
	{
		if (!Initialized)
			return false;	
		return Instance.SpriteByPath.ContainsKey(path);
	}

	private static Sprite getManagedSprite(string path)
	{
		if (!Initialized || !hasManagedSprite(path))
			return null;		
		return Instance.SpriteByPath[path];
	}

	private static List<Sprite> getManagedSprites(string path)
	{		
		if (!Initialized)
			return null;	

		var result = new List<Sprite>();

		foreach (KeyValuePair<string,Sprite> pair in Instance.SpriteByPath) {

			var spritePath = pair.Key;
			if (spritePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)) {
				result.Add(pair.Value);
			}

		}
		return result;
	}

	/**
	 * Loads all resources at given path and of given type and returns the list of resources. 
	 * @param path Path to resources i.e "/SFX"
	 * @param recuse If true sub folders will be included
	 */
	public static List<T> GetResourcesAtPath<T>(string path) where T: UnityEngine.Object
	{
		var unmanagedAssets = Resources.LoadAll<T>(path);
		var result = new List<T>();
		result.AddRange(unmanagedAssets);

		if (typeof(T) == typeof(Sprite)) {
			var managedAssets = getManagedSprites(path);
			if (managedAssets != null)
				result.AddRange(managedAssets as List<T>);
		}

		return result;
	}

	/**
	 * Loads first resource matching given path and returns it
	 */
	public static T GetResourceAtPath<T>(string path) where T: UnityEngine.Object
	{
		if (typeof(T) == typeof(Sprite)) {
			Sprite sprite = getManagedSprite(path);
			if (sprite != null)
				return sprite as T;
		}			
		return Resources.Load<T>(path);
	}

	/**
	 * Returns a single sprite from given path and name 
	 * 
	 * @param pathAndFilename The location to the sprite.  I.e. "Backgrounds/Town"
	 * @param throwEror If set will throw exception with given message if sprite is not found.
	 * 
	 * @returns The sprite, or null if not found 
	 */
	public static Sprite GetSprite(string path, bool throwError = false)
	{
		var result = GetResourceAtPath<Sprite>(path);

		if (result == null && throwError)
			throw new Exception("Sprite " + path + " not found.");
		
		return result;
	}

	/**
	 * Returns if a resource exists at given path 
	 */
	public static bool HasResource(string path)
	{
		if (hasManagedSprite(path))
			return true;
		return (Resources.Load<TextAsset>(path) != null);
	}

	private void updateDictionary(string path)
	{
		SpriteByPath = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

		foreach (var resource in SpriteList) {
			SpriteByPath[path + resource.name] = resource;
		}
	}

	/** Sets given resources as resource manager's resources. */
	public void SetResources(Sprite[] resources)
	{
		List<Sprite> list = new List<Sprite>();

		foreach (Sprite resource in resources) {

			bool is2x = resource.name.EndsWith("@x2");

			if (is2x && !_include2x)
				continue;

			list.Add(resource);
		}

		SpriteList = list;

		updateDictionary(ResourcePath);
	}


	/** 
	 * 
	 * Enabled or disables highres textures on the GameTemplates 'skin'.
	 * 
	 * The function will look through all the styles, and style states and find if there are any high-res textures avaiable.  If there are the will
	 * be swapped out.
	 * 
	 * High-res textures must be placed under resources\gui and they must have the exact name of the origional texture + " x2" at the end.
	 * 
	 * Texture swaps will be permenant in the editor (not sure about during normal playback) so you can run this function with enabled=false to
	 * undo the changes and swap back to standard textures.
	 * 
	 * Also this function will, as best as it can, try to update the styles of already created components.  However it's really best to run it once
	 * at initializaiton.
	 * 
	 */
	private static void setHighDPIGuiGraphics(bool enabled = true, bool applyToExistingComponents = true)
	{
		if (enabled)
			Trace.Log("Enabling High-DPI gui graphics");
		else
			Trace.Log("Disabling High-DPI gui graphics");

		Util.Assert(Engine.Instance != null, "Can not set HighDPI before engine is initialized.");
		Util.Assert(Engine.Instance.Skin != null, "Error setting HighDPI, no gui skin is assigned.");

		if (enabled == true && !Include2x) {
			Trace.LogWarning("Can not enabled High-DPI gui as it was not included with this build.");
			_usingHighDPIGui = false;
			return;
		}	
			
		var guiSprites = GetResourcesAtPath<Sprite>("Gui");

		var dictionary = new Dictionary<string,Sprite>();

		foreach (Sprite sprite in guiSprites) {
			dictionary[sprite.name] = sprite;
			// Checking the name for @x2 is too slow during a draw call so I tag as highDPI here.
			if (sprite.name.EndsWith("@x2"))
				sprite.SetHighDPI();
		}

		List<GUIStyle> stylesToUpdate = new List<GUIStyle>();

		foreach (GUIStyle style in Engine.Instance.Skin)
			stylesToUpdate.Add(style);

		// Add our controls so they get updated too, as their styles will be copies 
		if (applyToExistingComponents) {
			foreach (var state in Engine.StateList) {
				var components = state.AllChildren();
				foreach (var component in components)
					stylesToUpdate.Add(component.Style);
			}
		}

		foreach (GUIStyle style in stylesToUpdate) {			
			foreach (GUIStyleState styleState in style.GetStyleStates()) {
				Texture2D texture = styleState.background;

				if (texture == null)
					continue;

				if (texture.HighDPI() && !enabled) {
					// switch HighDPI back
					string lowDPIName = texture.name.Replace("@x2", "");
					if (dictionary.ContainsKey(lowDPIName)) {
						var lowDPISprite = dictionary[lowDPIName];
						styleState.background = lowDPISprite.texture;
					}
				}

				if (!texture.HighDPI() && enabled) {
					// switch HighDPI in
					string highDPIName = texture.name + "@x2";

					if (dictionary.ContainsKey(highDPIName)) {
						var highDPISprite = dictionary[highDPIName];
						styleState.background = highDPISprite.texture;
					}
				}

			}
		}
		_usingHighDPIGui = enabled;
	}
}