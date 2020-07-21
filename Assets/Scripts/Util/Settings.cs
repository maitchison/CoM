
using UnityEngine;

using System;
using System.Xml.Linq;

using Data;

public enum SettingType
{
	Ignore,
	Normal,
	Important
}

[System.AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
	public string DisplayName;
	public Color Color;
	public bool Ignore;

	public SettingAttribute(string displayName, SettingType type = SettingType.Normal)
	{
		this.DisplayName = displayName;
		this.Color = (type == SettingType.Important) ? Color.red : Color.white;
		this.Ignore = (type == SettingType.Ignore);
	}

	public SettingAttribute(bool ignore)
	{
		this.Ignore = ignore;
	}

	public static SettingAttribute Default {
		get {
			return new SettingAttribute(null, SettingType.Normal);
		}
	}
}

[System.AttributeUsage(AttributeTargets.Property)]
public class SettingRange : Attribute
{
	public float Min;
	public float Max;

	public SettingRange(float minValue, float maxValue)
	{
		this.Min = minValue;
		this.Max = maxValue;
	}
}

[System.AttributeUsage(AttributeTargets.Property)]
public class SettingDivider : Attribute
{
	public string Name;

	public SettingDivider(string name = "")
	{
		this.Name = name;
	}
}

/**
 * Stores and allows access to various settings 
 */
public class SettingsGroup : SimpleMap
{
	public string Name;

	public SettingsGroup(string name)
		: base("Name", "Value", "Setting")
	{
		this.Name = name;
		OnValidateValue += ValidateSettings;
	}

	/** Assigned default values to properties.*/
	protected virtual void Default()
	{
	}

	/** Returns true if this settings is currently disabled. */
	virtual public bool isDisabled(string name)
	{
		return false;
	}

	virtual protected bool ValidateSettings(string name, string newValue)
	{
		return true;
	}

	/**
	 * Resets settings group to default settings
	 */
	public void Reset()
	{
		foreach (var property in this.GetType().GetProperties()) {
			if (!property.CanWrite)
				continue;
			if (property.Name == "Item")
				continue;

			if (property.PropertyType == typeof(Boolean))
				SetValue(property.Name, false);
			if (property.PropertyType == typeof(int))
				SetValue(property.Name, 0);
			if (property.PropertyType == typeof(float))
				SetValue(property.Name, 0f);
			if (property.PropertyType == typeof(string))
				SetValue(property.Name, "");
		}
		Default();
	}
}

public class InformationSettings : SettingsGroup
{
	public string PermanentPath { get { return Application.persistentDataPath; } }

	public string DataPath { get { return Application.dataPath; } }

	public string StreamingAssetsPath { get { return Application.streamingAssetsPath; } }

	public string TemporaryCachePath { get { return Application.temporaryCachePath; } }

	public string SaveFileLocation { get { return StateStorage.SaveDataLocation.ToString(); } }

	public string DeviceInfo { get { return string.Format("{1} {2}", SystemInfo.deviceName, SystemInfo.deviceModel, SystemInfo.deviceType); } }

	public string GraphicsInfo { get { return string.Format("{1} [SM{2}] {3} MB", SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType, SystemInfo.graphicsShaderLevel, SystemInfo.graphicsMemorySize); } }

	public string RenderingInfo { get { return string.Format("{0}", CoM.Instance.Camera.actualRenderingPath); } }

	public int SaveVersion { get { return SaveFile.SaveVersion; } }

	public DateTime SaveTimeStamp { get { return SaveFile.SaveTimeStamp; } }

	/** Total time played on this device in seconds. */
	public float TotalPlayTime { get { return LookupFloat("TotalPlayTime"); } set { SetValue("TotalPlayTime", value); } }

	public InformationSettings()
		: base("Information")
	{ 
	}

}

public class AdvancedSettings : SettingsGroup
{
	/** Enables various keys and buttons that do things like instantly level the player */
	public bool PowerMode { get { return LookupBool("PowerMode"); } set { SetValue("PowerMode", value); } }


	[SettingAttribute("Target Frame Rate")]
	[SettingRange(0, 120)]
	public int	 	TargetFrameRate { get { return LookupInt("TargetFrameRate", -1); } set { SetValue("TargetFrameRate", value); } }

	public bool WhiteWizard { get { return LookupBool("WhiteWizard"); } set { SetValue("WhiteWizard", value); } }

	public bool ShowFPSDetails { get { return LookupBool("ShowFPSDetails"); } set { SetValue("ShowFPSDetails", value); } }

	public bool ShowAreaDebug { get { return LookupBool("ShowAreaDebug"); } set { SetValue("ShowAreaDebug", value); } }

	public bool ShowCacheRenders { get { return LookupBool("ShowCacheRenders"); } set { SetValue("ShowCacheRenders", value); } }

	/** Increases the number of drops from monsters by x percent */
	public int DropRateMod { get { return LookupInt("DropRateMod"); } set { SetValue("DropRateMod", value); } }

	/** Allows characters to be created with unlimited points */
	public bool UnlimitedCharacterPoints { get { return LookupBool("UnlimitedCharacterPoints"); } set { SetValue("UnlimitedCharacterPoints", value); } }

	/** If true controls will show their frames when drawing */
	public bool ShowGuiBounds { get { return LookupBool("ShowGuiBounds"); } set { SetValue("ShowGuiBounds", value); } }

	/** If true shows which material each gui component is using to draw */
	public bool ShowGuiMaterial { get { return LookupBool("ShowGuiMaterial"); } set { SetValue("ShowGuiMaterial", value); } }

	/** Enables the logging of comment statements, which often spam the log window but can be helpful when tracking down specific problems */
	public bool LogComments { get { return LookupBool("LogComments"); } set { SetValue("LogComments", value); } }

	/** If true detailed combat information will be logged */
	public bool LogCombat { get { return LookupBool("LogCombat"); } set { SetValue("LogCombat", value); } }


	/** If true detailed combat information will be logged */
	public bool GuiCaching { get { return LookupBool("GuiCaching"); } set { SetValue("GuiCaching", value); } }

	/** If true logs will be displayed in a loggin window */
	[SettingAttribute("Show Logs")]
	public bool ShowLogs { get { return LookupBool("ShowLogs"); } set { SetValue("ShowLogs", value); } }

	public bool RunAutoQualityTest { get { return LookupBool("RunAutoQualityTest", true); } set { SetValue("RunAutoQualityTest", value); } }

	/** Global directional light */
	public bool FloodLight { get { return LookupBool("FloodLight"); } set { SetValue("FloodLight", value); } }

	/** Shows party from birds eye */
	public bool BirdsEyeView { get { return LookupBool("BirdsEyeView"); } set { SetValue("BirdsEyeView", value); } }

	/** Displays the automap over the floor */
	public bool FloorMap { get { return LookupBool("FloorMap"); } set { SetValue("FloorMap", value); } }

	public float 	GuiScale { get { return LookupFloat("GuiScale"); } set { SetValue("GuiScale", value); } }

	public bool		UseHighDPIGui { get { return LookupBool("UseHighDPIGui"); } set { SetValue("UseHighDPIGui", value); } }

	/** Enables compression on save files.  Typically reducing the size to a tenth.  In general this isn't needed and makes reading the saves by hand impossiable */
	public bool		SaveFileCompression { get { return LookupBool("SaveFileCompression"); } set { SetValue("SaveFileCompression", value); } }

	/** Enables compression on data files.  Typically reducing the size to a tenth.  This is required for storing game data in player prefs on the webplayer.  */
	public bool		DataFileCompression { get { return (Application.isWebPlayer ? true : LookupBool("DataFileCompression")); } set { SetValue("DataFileCompression", value); } }

	public bool 	AutoFight { get { return LookupBool("AutoFight"); } set { SetValue("AutoFight", value); } }

	public float	DoubleClickDelay { get { return LookupFloat("DoubleClickDelay", 0.4f); } set { SetValue("DoubleClickDelay", value); } }

	public float	DoubleClickMovementThreshold { get { return LookupFloat("DoubleClickMovementThreshold", 2); } set { SetValue("DoubleClickMovementThreshold", value); } }

	public float	DragDelay { get { return LookupFloat("DragDelay", 0.4f); } set { SetValue("DragDelay", value); } }

	public float	DragMovementThreshold { get { return LookupFloat("DragMovementThreshold", 4); } set { SetValue("DragMovementThreshold", value); } }

	public bool		LinearLight { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }

	/** Set to true the first time the game is run. */
	public bool 	FirstRun  { get { return LookupBool("FirstRun", true); } set { SetValue("FirstRun", value); } }

	/** Used to detect version changes. */
	public string	LastVersion { get { return LookupString("Version"); } set { SetValue("Version", value); } }

	public AdvancedSettings()
		: base("Advanced")
	{ 
	}

	protected override void Default()
	{
		PowerMode = false;
		DataFileCompression = false;
		GuiScale = 1.0f;
		UseHighDPIGui = (Screen.dpi > 200);
		GuiCaching = true;
		DoubleClickDelay = 0.4f;
		DoubleClickMovementThreshold = Engine.TouchDevice ? 4 : 2;
		DragDelay = 0.4f;
		DragMovementThreshold = 4;
		RunAutoQualityTest = true;
		FirstRun = true;

		TargetFrameRate = -1;
	}
		
}

public class GeneralSettings : SettingsGroup
{
	[SettingAttribute(true)]
	public bool 	MusicEnabled { get { return MusicVolume > 0; } }

	[SettingDivider("Sound")]
	[SettingAttribute("Music")]
	[SettingRange(0, 100)]
	public float	MusicVolume { get { return LookupFloat("MusicVolume", 50f); } set { SetValue("MusicVolume", value); } }

	[SettingAttribute("SFX")]
	[SettingRange(0, 100)]
	public float	SoundFXVolume { get { return LookupFloat("SoundFXVolume", 50f); } set { SetValue("SoundFXVolume", value); } }


	[SettingDivider("Graphics")]

	[SettingAttribute("Vsync")]
	public bool 	EnablevSnyc { get { return LookupBool("EnableVysnc"); } set { SetValue("EnableVysnc", value); } }

	[SettingDivider()]

	[SettingAttribute("Anti Aliasing")]
	public bool 	EnableAntiAliasing { get { return LookupBool("EnableAA"); } set { SetValue("EnableAA", value); } }

	[SettingAttribute("Anisotropic Filtering")]
	public bool 	EnableAnisotropicFiltering { get { return LookupBool("EnableAnisotropicFiltering"); } set { SetValue("EnableAnisotropicFiltering", value); } }

	[SettingAttribute("High Quality Lighting")]
	public bool 	HighQualityLighting { get { return LookupBool("HighQualityLighting"); } set { SetValue("HighQualityLighting", value); } }

	[SettingAttribute("SSAO")]
	public bool 	EnableSSAO { get { return LookupBool("EnableSSAO", true); } set { SetValue("EnableSSAO", value); } }

	[SettingDivider()]
	[SettingAttribute("FX Mode")]
	public RenderFXStyle FXStyle { get { return (RenderFXStyle)LookupInt("FXStyle"); } set { SetValue("FXStyle", (int)value); } }

	[SettingAttribute(true)]
	public int UnityQualityLevel { get { return HighQualityLighting ? 2 : 1; } }

	[SettingAttribute(true)]
	public int vSyncCount { get { return EnablevSnyc ? 1 : 0; } }

	[SettingAttribute(true)]
	public AnisotropicFiltering AnisotropicFiltering { get { return EnableAnisotropicFiltering ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable; } }

	[SettingAttribute(true)]
	public int 		RawAALevel {
		get {
			return EnableAntiAliasing ? 4 : 0;
		}
	}

	/** Logarithimc version of music volume */
	[SettingAttribute(true)]
	public float MusicVolumeDB
	{ get { return (float)Math.Pow(MusicVolume / 100f, 2f); } }

	/** Logarithimc version of sfx volume */
	[SettingAttribute(true)]
	public float SFXVolumeDB
	{ get { return (float)Math.Pow(SoundFXVolume / 100f, 2f); } }

	[SettingDivider()]
	public bool ShowFPS { get { return LookupBool("ShowFPS"); } set { SetValue("ShowFPS", value); } }

	[SettingAttribute(true)]
	public string Resolution { get { return Screen.width + "x" + Screen.height + ((Screen.currentResolution.refreshRate != 0) ? " (" + Screen.currentResolution.refreshRate + " fps)" : ""); } }

	[SettingAttribute(true)]
	public float DPI { get { return Screen.dpi; } }

	public GeneralSettings()
		: base("General")
	{ 
	}

	/** Make sure settigns are valid. */
	override protected bool ValidateSettings(string name, string newValue)
	{
		if (!base.ValidateSettings(name, newValue))
			return false;

		// Disable some controls when we're running on SM20
		if (SystemInfo.graphicsShaderLevel <= 20) {
			if (name == "HighQualityLighting" && (bool.Parse(newValue)))
				return false;
			if (name == "EnableAA" && (bool.Parse(newValue)))
				return false;
			if (name == "EnableSSAO" && (bool.Parse(newValue)))
				return false;
		}

		if (Application.platform == RuntimePlatform.WebGLPlayer) {
			if (name == "EnableAntiAliasing" && (bool.Parse(newValue)))
				return false;			
		}

		return true;
	}

	/** Disable the high quality lighting if shader model doesn't support it. */
	public override bool isDisabled(string name)
	{
		if (SystemInfo.graphicsShaderLevel <= 20) {
			if (name == "HighQualityLighting")
				return true;
			if (name == "EnableSSAO")
				return true;	
			if (name == "EnableAA")
				return true;	
			if (name == "FXStyle")
				return true;
		}

		if (Application.platform == RuntimePlatform.WebGLPlayer) {
			if (name == "EnableAntiAliasing")
				return true;	
		}
			
		return base.isDisabled(name);
	}

	protected override void Default()
	{
		MusicVolume = 50f;
		SoundFXVolume = 50f;
		EnablevSnyc = true;
		HighQualityLighting = true;
		EnableAnisotropicFiltering = true;
		EnableSSAO = true;
		ShowFPS = false;
	}
}

/** Static class for game constants */
public class Settings
{
	public static bool IsLoaded = false;

	public static GeneralSettings General;
	public static AdvancedSettings Advanced;
	public static InformationSettings Information;

	/** Reads all settings from XML file */
	public static void ReadSettings()
	{
		bool hasSettingsFile = StateStorage.HasData("Settings");

		General = new GeneralSettings();
		Advanced = new AdvancedSettings();
		Information = new InformationSettings();

		try {
			if (hasSettingsFile) {
				XElement rootNode = StateStorage.LoadXML("Settings");
				if (rootNode == null) {
					Trace.LogWarning("No settings in file, resetting to default");
					ResetSettings();
				} else {
					Trace.Log("Reading settings");
					General.Load(rootNode.Element("General"));
					Advanced.Load(rootNode.Element("Advanced"));
				}
			} else {
				Trace.Log("No settings file present, setting to default");
				ResetSettings();
			}

		} catch (Exception e) {
			Trace.LogWarning("Error while reading settings file:" + e.Message);
			ResetSettings();
		}

		IsLoaded = true;
	}

	/** Resets all settings to default. */
	public static void ResetSettings()
	{
		Trace.Log("Resetting settings to default.");
		General.Reset();
		Advanced.Reset();
	}

	/** Deletes settings file. */
	public static void DeleteSettings()
	{
		StateStorage.DeleteData("Settings");
	}

	public static void SaveSettings()
	{
		if (!IsLoaded) {
			Trace.LogWarning("Tried saving settings, but settings are not yet loaded.");
			return;
		}

		XElement node = new XElement("Settings");
		XElement subNode;

		subNode = new XElement("General");
		General.WriteNode(subNode);
		node.Add(subNode);

		subNode = new XElement("Advanced");
		Advanced.WriteNode(subNode);
		node.Add(subNode);

		StateStorage.SaveData("Settings", node);

		PlayerPrefs.Save();
	}

}
	