
using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

using UI;
using UI.State;
using UI.State.Town;
using UI.State.Menu;

using Mordor.Importers;

using Mordor;
using Data;
using UnityEngine.Analytics;
using Engines;
using Culler;
using System.Collections;

public delegate void GameTrigger();

/** Manages the display of the gui layer, and holds some shared resources. */
public class CoM : Engine
{
	/** Called when a new message is posted */
	public static event MessagePostedEvent OnMessagePosted;

	public delegate void MessagePostedEvent(object source,EventArgs e);

	// ---------------------------------------------------------
	// static variables
	
	/** The singleton instance of this class (cast as a mordorGame) */
	new public static CoM Instance { get { return (Engine.Instance as CoM); } }

	public static List<MessageEntry> MessageLog = new List<MessageEntry>();

	/** The games dungeon */
	public static MDRDungeon Dungeon;
	

	/** Used to temporarily disable analytics. */
	public static bool DisableAnalytics = false;

	// -----------------------------------------------------
	// Static data
	// -----------------------------------------------------


	/** The games monster classes */
	public static MDRMonsterClassLibrary MonsterClasses;

	/** The games monster types */
	public static MDRMonsterTypeLibrary MonsterTypes;

	/** The games monsters */
	public static MDRMonsterLibrary Monsters;

	/** The games damage types */
	public static MDRDamageTypeLibrary DamageTypes;

	/** The games skills */
	public static MDRSkillLibrary Skills;

	/** The games guilds */
	public static MDRGuildLibrary Guilds;

	/** The games races */
	public static MDRRaceLibrary Races;

	/** The games spells classes */
	public static MDRSpellClassLibrary SpellClasses;

	/** The games spells */
	public static MDRSpellLibrary Spells;

	/** The games items */
	public static MDRItemLibrary Items;

	/** The games item subtypes */
	public static MDRItemTypeLibrary ItemTypes;
		
	/** The games item subtypes */
	public static MDRItemClassLibrary ItemClasses;

	/** Stores data specific to this user. */
	public static UserProfile Profile;

	// -----------------------------------------------------
	// Dynamic data
	// -----------------------------------------------------

	public static GameStateManager State;

	/** The currently selected party */
	public static MDRParty Party;

	public static MDRCharacter SelectedCharacter { 
		get {
			return (Party == null) ? null : Party.Selected;
		}
	}

	/** True once the game data base been loaded.  This means the dungeon, monsters, items, etc have all been loaded */
	public static bool GameDataLoaded { get { return _gameDataLoaded; } }

	/** True once the game data base been loaded.  This means the dungeon, monsters, items, etc have all been loaded */
	public static bool SaveFileLoaded { get { return State.Loaded; } }

	/** True once the game data base been loaded.  This means the dungeon, monsters, items, etc have all been loaded */
	public static bool AllDataLoaded { get { return GameDataLoaded && SaveFileLoaded; } }

	/** True once the game graphics has been read in. */
	public static bool GraphicsLoaded { get { return _graphicsLoaded; } }

	/** True once the all the data and graphics have been loaded */
	public static bool Loaded { get { return GraphicsLoaded && AllDataLoaded; } }

	/** If true game will be saved just before the application closes */
	public static bool SaveOnExit = true;

	/** The number of messages to record until we start removing them */
	private const int MAX_MESSAGES = 32;

	/** Used to remove objects that can not be seen */
	public static AStarCuller Culler;

	/** Next time town is loaded we will open the temple. */
	public static bool AutoGotoTemple;

	// ---------------------------------------------------------
	// Stock styles

	private static GUIStyle _subtextStyle;

	/** Small yellow text */
	public static GUIStyle SubtextStyle {
		get { 
			if (_subtextStyle == null) {
				_subtextStyle = new GUIStyle(GUIStyle.none);
			}

			_subtextStyle.fontSize = 12;
			_subtextStyle.alignment = TextAnchor.UpperLeft;
			_subtextStyle.normal.textColor = Color.yellow;

			return _subtextStyle; 
		}
	}
	// ---------------------------------------------------------
	// Links to gamestate, because of system where CoM was used for most things.

	public static GameRecords GameStats { get { return State.GameStats; } }

	public static MDRCharacterLibrary CharacterList { get { return State.CharacterList; } }

	public static MDRPartyLibrary PartyList { get { return State.PartyList; } }

	public static MDRStore Store { get { return State.Store; } }

	public static MDRDungeon ExploredDungeon { get { return State.ExploredDungeon; } }


	// ---------------------------------------------------------
	// Private static
	
	private static bool _gameDataLoaded = false;
	private static bool _graphicsLoaded = false;

	private const string MiscAtlasPath = "Gfx/Misc";
	private const string MonsterAtlasPath = "Gfx/Monsters";
	private const string MapIconsAtlasPath = "Gfx/Map";
	private const string ItemIconsAtlasPath = "Gfx/Items";
	private const string SpellIconsAtlasPath = "Gfx/Spells";
	private const string IconsAtlasPath = "Icons";
	private const string PortraitsXMLPath = "Gfx/Portraits";

	/** The data files that are required to make the game run */
	public static string[] RequiredStaticData;

	// ---------------------------------------------------------
	// Public to editor
	
	public GameObject DungeonObject;

	public GameObject FloorMap;

	public Light FloodLightObject;

	public Transform MonsterAvatar;

	/** Number of second between autosaves, 0 to disable. */
	const int AutoSaveInterval = 60;

	/** Number of second between phone homes, 0 to disable. */
	const int PhoneHomeInterval = 600;

	/** Number of second between update ticks*/
	const int TickInterval = 10;

	// ---------------------------------------------------------
	// Graphics
	
	internal SpriteLibrary MiscSprites;
	internal SpriteLibrary MonsterSprites;
	internal SpriteLibrary MapIconSprites;
	internal SpriteLibrary ItemIconSprites;
	internal SpriteLibrary SpellIconSprites;
	internal SpriteLibrary IconSprites;
	internal SpriteLibrary Portraits;
	
	protected GuiButton OptionsButton;

	internal DungeonState DungeonState;

	public static bool BenchmarkModeEnabled = false;

	/** If true dungeon will be rebuilt on startup.  Useful having the dungeon build on startup rather than have it packaged (which can take up extra space) */
	public bool RebuildDungeonOnStart = true;

	//todo: dungeon state?
	/** The dungeon level we are currently showing */
	public int CurrentDungeonLevel = -1;

	public override void Initialize()
	{
		Trace.Log("Starting game");

		RequiredStaticData = new string[] {	
			//"Dungeon",
			"Guilds",
			"Races",
			"ItemClasses",
			"ItemTypes",
			"Items",
			"MonsterClasses",
			"MonsterTypes",
			"Monsters",
			"SpellClasses",
			"Spells",
			"Skills",
			"DamageTypes"
		};

		State = new GameStateManager();

		Settings.ReadSettings();

		ProcessParameters();

		base.Initialize();

		SetFloodLight(Settings.Advanced.FloodLight);

		PostMessage("Welcome to Endurance.");

		Culler = new AStarCuller();

		PushState(new LoadingState());

		if (AutoSaveInterval != 0)
			StartCoroutine(AutoSaveTimer());
		if (PhoneHomeInterval != 0)
			StartCoroutine(PhoneHomeTimer());
		StartCoroutine(UpdateTickTimer());

		ResourceManager.UpdateHighDPI();
	}
		
	#if UNITY_WEBPLAYER	|| UNITY_IPHONE
	
	private static void ProcessParameters()
	{
	}














#else
	
	private static void ProcessParameters()
	{
		foreach (string arg in System.Environment.GetCommandLineArgs()) {
			if (string.Compare(arg, "-bench", StringComparison.OrdinalIgnoreCase) == 0)
				BenchmarkModeEnabled = true;
		}
	}
	#endif

	/** Starts playing the game with given party.  Game will be initalized, and dungeon loaded as appropriate */
	public static void LoadParty(MDRParty newParty)
	{
		Util.Assert(AllDataLoaded, "Data must be loaded before a party can be run.");
		Util.Assert(newParty != null, "Can not start game with null party.");

		Trace.Log("Starting game with party " + newParty);

		Party = newParty;

		//2: work out which level we need to instantiate
		Instance.DungeonState = new DungeonState();
		PopAllStates();
		PushState(Instance.DungeonState);

		PartyController.Instance.Party = Party;
	}

	/** 
	 * Loads the specific dungeon level and displays it.  If level 0 is loaded the town state will be shown. 
	 * todo: should this be dungeon states job?
	 */
	public static void LoadDungeonLevel(int newLevel)
	{
		Util.Assert(AllDataLoaded, "Data must be loaded before dungeon level.");
		Util.Assert(GraphicsLoaded, "Graphics must be loaded before dungeon level.");

		if ((newLevel < 0) || (newLevel >= CoM.Dungeon.Floor.Length))
			throw new Exception("Invalid level index to load " + newLevel);

		if (newLevel == 0) {
			GetDungeonBuilder().SelectedMap = 0;
			EnableCamera = false;
			PushState(new TownState());

			if (AutoGotoTemple) {
				PushState(new TempleState());
				AutoGotoTemple = false;
			}

		} else {			
			Trace.Log("Loading dungeon level " + newLevel + "/" + CoM.Dungeon.Floors);
			GetDungeonBuilder().SelectedMap = newLevel;
			EnableCamera = true;
			Culler.Initialize(Party, newLevel);
		}

		if (Instance.DungeonState != null)
			Instance.DungeonState.AutoMap.Map = Party.ExploredMap;
		Instance.CurrentDungeonLevel = newLevel;

		//stub: reset monsters
		State.ForceRespawn();

		FPSCounter.Reset();
	}

	public static DungeonBuilder GetDungeonBuilder()
	{
		return Instance.DungeonObject.GetComponent<DungeonBuilder>();
	}

	/** 
	 * Loads a file from storage and returns it.  
	 * Provides validiation and logging.
	 * If the type is a library the library will be set as the global library for that type.
	 * If min version is set an exception will be thrown if data doesn't meet minimum version.
	 */
	private static T LoadData<T>(string source, float minVersion = 0.0f, bool silent = false) where T: DataObject
	{
		if (!StateStorage.HasData(source))
			throw new Exception("Source not found [" + source + "]");
		
		T result = StateStorage.LoadData<T>(source);

		if (result.Version < minVersion)
			throw new Exception(String.Format("File {0} has an old version, expecting {1} but found {2}", source, minVersion, result.Version));

		if (!silent)
			Trace.Log(" - Loaded [" + source + "] (v" + result.Version.ToString("0.0") + ")");

		return result;
	}

	/** 
	 * Loads a library and returns it.
	 * Provides validiation and logging.
	 * If SetAsGlobal is true it will be set as the global library for that type.
	 * If no items are loaded an exception will be generated.
	 */
	private static T LoadLibrary<T, U>(string source, float minVersion = 0.0f, bool setAsGlobal = true, bool ignoreEmptyLibrarys = false)
		where T:DataLibrary<U>
		where U:NamedDataObject
	{
		T library;

		try {
			library = LoadData<T>(source, minVersion, true);
		} catch (Exception e) {
			throw new Exception(string.Format("Could not load library [{0}]\n", source) + e.Message + "\n\n" + e.StackTrace, e.InnerException);			
		}

		if (library == null)
			return null;

		Trace.Log(" - Loaded " + library.Count + " from [" + source + "] (v" + library.Version.ToString("0.0") + ")");

		if (setAsGlobal)
			NamedDataObject.AddGlobalLibrary(library);

		if ((library.Count == 0) && (!ignoreEmptyLibrarys))
			throw new Exception("Error loading library: [" + library + "], no records found.");

		return library;
	}

	/** Loads all the static data, this includes things like monsters, items, etc.  These values do not change as the game progresses */
	public static void LoadGameData()
	{		
		DateTime startTime = DateTime.Now;
	
		Trace.Log("Loading game data:");

		DamageTypes = LoadLibrary<MDRDamageTypeLibrary,MDRDamageType>("DamageTypes", 0, true);
		Skills = LoadLibrary<MDRSkillLibrary,MDRSkill>("Skills", 0, true);

		SpellClasses = LoadLibrary<MDRSpellClassLibrary,MDRSpellClass>("SpellClasses");
		Spells = LoadLibrary<MDRSpellLibrary,MDRSpell>("Spells");

		Races = LoadLibrary<MDRRaceLibrary,MDRRace>("Races");
		Guilds = LoadLibrary<MDRGuildLibrary,MDRGuild>("Guilds");

		ItemClasses = LoadLibrary<MDRItemClassLibrary,MDRItemClass>("ItemClasses");
		ItemTypes = LoadLibrary<MDRItemTypeLibrary,MDRItemType>("ItemTypes");
		Items = LoadLibrary<MDRItemLibrary,MDRItem>("Items");

		MonsterClasses = LoadLibrary<MDRMonsterClassLibrary,MDRMonsterClass>("MonsterClasses");
		MonsterTypes = LoadLibrary<MDRMonsterTypeLibrary,MDRMonsterType>("MonsterTypes");
		Monsters = LoadLibrary<MDRMonsterLibrary,MDRMonster>("Monsters");

		// stub: import dungeon
		var importer = new MapImporter();
		Dungeon = importer.LoadDungeon(Util.ResourceToStream("Data/MDATA11"));
		//Dungeon = LoadData<MDRDungeon>("Dungeon", 2.0f);

		Trace.Log("Game data loading completed in " + (DateTime.Now - startTime).TotalMilliseconds.ToString("0.0") + "ms.");

		_gameDataLoaded = true;
	}

	/** Checks characters to see if any of them set a new record.
	 * @param silent if true notifications will not be displayed when a record is set.
	 */
	public static void UpdateCharacteRecords(Boolean silent = false)
	{
		if (silent)
			GameStats.PostNotifications = false;
		
		foreach (MDRCharacter character in CharacterList)
			GameStats.CheckCharacterRecords(character);
		
		if (silent)
			GameStats.PostNotifications = true;
	}

	public static void DeleteSaveFile()
	{
		Trace.Log("Deleting save file");
		StateStorage.DeleteData("Characters");
		StateStorage.DeleteData("Parties");
		StateStorage.DeleteData("Store");
		StateStorage.DeleteData("ExploredDungeon");
		StateStorage.DeleteData("Monsters");
	}

	public static void DeleteGameStats()
	{
		StateStorage.DeleteData("GameStats");
	}

	/** 
	 * Deletes all the games files, reseting the game back to when it was first run 
	 */
	public static void FullReset()
	{
		Trace.Log("Full Reset");
		DeleteSaveFile();
		DeleteGameStats();
		Settings.DeleteSettings();
		PlayerPrefs.DeleteAll();
	}

	/** Returns true only if all the requied static game files are present.  */
	public static bool HasAllStaticData()
	{
		foreach (var file in RequiredStaticData)
			if (!StateStorage.HasData(file)) {
				Trace.LogDebug("Missing file {0}", file);
				return false;
			}
		return true;
	}

	/** Writes all static game data to storage with optional compression */
	public static void WriteStaticDataToStorage(bool compression = false)
	{
		StateStorage.SaveData("Dungeon", Dungeon, compression);
		StateStorage.SaveData("Guilds", Guilds, compression);
		StateStorage.SaveData("Races", Races, compression);
		StateStorage.SaveData("ItemClasses", ItemClasses, compression);
		StateStorage.SaveData("ItemTypes", ItemTypes, compression);
		StateStorage.SaveData("Items", Items, compression);
		StateStorage.SaveData("MonsterClasses", MonsterClasses, compression);
		StateStorage.SaveData("MonsterTypes", MonsterTypes, compression);
		StateStorage.SaveData("Monsters", Monsters, compression);
		StateStorage.SaveData("SpellClasses", SpellClasses, compression);
		StateStorage.SaveData("Spells", Spells, compression);
	}

	/** Create global controls */
	private void CreateControls()
	{
		OptionsButton = new GuiButton("Options");
		OptionsButton.OnMouseClicked += delegate {
			if (!(CurrentState is SettingsMenuState))
				PushState(new SettingsMenuState());
		};	

	}

	/** Draw overlayed ui.  todo: maybe itemInfo should be part of an always present overlay state */
	public override void DoDraw()
	{
		base.DoDraw();

		// check if the game is loadeding and don't draw if we are...
		if (CurrentState is LoadingState) {
			return;
		}
			
		if (Settings.Advanced.PowerMode && (OptionsButton != null) && (!HideGUI)) {
			CurrentState.PositionComponent(OptionsButton, 10, -10);
			OptionsButton.Update();
			OptionsButton.Draw();
		}		 
	}

	/**
	 * Takes a screenshot, saving with the current filename.  If no filename is given the date and time will be used */
	public static void TakeScreenshot(string filename = "")
	{
		string path = Application.persistentDataPath + "/Screenshots";

		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);

		if (filename == "")
			filename = "Endurance " + DateTime.Now.ToString("yyyy-MM-dd-hh-m-s");

		string fullPath = path + "/" + filename + ".png";

		GUI.matrix = Matrix4x4.identity;

		ScreenCapture.CaptureScreenshot(fullPath);
		PostMessage("Screenshot taken {0}", Util.Colorise(filename, Color.green));
	}

	/** Enables or disabled a map wide flood light. */
	public void SetFloodLight(bool value)
	{
		if (FloodLightObject != null) {
			FloodLightObject.GetComponent<Light>().enabled = value;
		}
	}

	/** Enables or disabled the floor map. */
	public void SetFloorMap(bool value)
	{
		if (FloorMap != null) {
			FloorMap.GetComponent<Renderer>().enabled = value;
		}
	}

	override protected void processKeys(bool ctrl, bool alt, bool shift)
	{
		base.processKeys(ctrl, alt, shift);

		if (Input.GetKeyUp(KeyCode.Print))
			TakeScreenshot();

		if (Input.GetKeyUp(KeyCode.F10)) {
			if (CurrentState is DungeonState || CurrentState is TownState)
				PushState(new InGameMenuState());
		}

		if (Settings.Advanced.PowerMode) {
			if (Input.GetKey(KeyCode.RightShift))
				Time.timeScale = 0.1f;
			else
				Time.timeScale = 1.0f;			

			if (shift) {
				if (Input.GetKeyDown(KeyCode.F1))
					ApplyQualityLevel(0);					
				if (Input.GetKeyDown(KeyCode.F2))
					ApplyQualityLevel(1);
				if (Input.GetKeyDown(KeyCode.F3))
					ApplyQualityLevel(2);
			} else {
				
				if (Input.GetKeyUp(KeyCode.F1))
					Settings.Advanced.ShowLogs = !Settings.Advanced.ShowLogs;

				if (Input.GetKeyUp(KeyCode.F2))
					Settings.Advanced.ShowGuiBounds = !Settings.Advanced.ShowGuiBounds;

				if (Input.GetKeyUp(KeyCode.F3))
					Settings.Advanced.ShowGuiMaterial = !Settings.Advanced.ShowGuiMaterial;
			

				if (Input.GetKeyUp(KeyCode.F4))
					Settings.Advanced.UseHighDPIGui = !Settings.Advanced.UseHighDPIGui;

				if (Input.GetKeyUp(KeyCode.F5))
					HideGUI = !HideGUI;

				if (Input.GetKeyUp(KeyCode.F6))
					DungeonObject.SetActive(!DungeonObject.activeSelf);
				
				if (Input.GetKeyUp(KeyCode.F7))
					CoM.Party.Selected.GiveItem(MDRItemInstance.Create(CoM.Items[0]));

				if (Input.GetKeyUp(KeyCode.F8))
					Settings.Advanced.GuiCaching = !Settings.Advanced.GuiCaching;

				if (Input.GetKeyUp(KeyCode.F9))
					Settings.Advanced.ShowLogs = !Settings.Advanced.ShowLogs;


				if (Input.GetKeyUp(KeyCode.F11))
					Settings.General.FXStyle = (Settings.General.FXStyle.Next());
				
			}
		}
	}

	/** Coroutine to handle autosaving at regular intervals. */
	private IEnumerator AutoSaveTimer()
	{
		while (true) {
			SaveGame();
			yield return new WaitForSeconds(AutoSaveInterval);
		}
	}

	/** Coroutine to handle phone home at regular intervals. */
	private IEnumerator PhoneHomeTimer()
	{
		while (true) {
			WebServer.PostCheckIn();
			yield return new WaitForSeconds(PhoneHomeInterval);
		}
	}

	/** Coroutine to update low priority things such as the time played. */
	private IEnumerator UpdateTickTimer()
	{		
		while (true) {

			if (!Loaded) {
				yield return new WaitForSeconds(TickInterval);
				continue;
			}

			if (!Engine.Idle) {
				Profile.TotalPlayTime += TickInterval;
				if (Party != null)
					foreach (MDRCharacter character in Party) {
						if (character != null)
							character.PlayTime += TickInterval;
					}
			}				

			yield return new WaitForSeconds(TickInterval);
		}
	}

	/** Updates the game */
	public override void DoUpdate()
	{
		// check if the game has loaded and move us to the main menu 
		if (CurrentState is LoadingState) {
			if (Loaded) {
				CreateControls();
				
				PopState();
				PushState(new MenuDungeonBackground());
				PushState(new MainMenuState());
			} else {
				base.DoUpdate();
				return;
			}
		}

		// update culling
		if (Culler != null)
			Culler.Update();

		// update settings
		//error: object not set to an instance of an object (when a few minutes into game in menu...)
		SetFloodLight(Settings.Advanced.FloodLight);
		SetFloorMap(Settings.Advanced.FloorMap);

		base.DoUpdate();
	}

	/** 
	 * Loads graphical resources at given path, returns a SpriteLibrary contianing the sprites.
	 * Will throw execptions if the path is not defined, or if there are no sprites at that path.
	 * 
	 * @param path The path to use when loading the graphical resource (relative to Resources folder)
	 * @param resourceName The name to display use for the resource in the case of an error
	 * 
	 * @returns A list of sprites found at that location.
	 */
	private static SpriteLibrary LoadGraphicResources(string path, string defaultSpriteName = "", string resourceName = "")
	{
		if (path == "")
			throw new Exception("No path defined for " + resourceName + " sprites.");
	
		var result = SpriteLibrary.FromResourcePath(path, defaultSpriteName);

		if (result.Count == 0)
			throw new Exception("No " + (resourceName == "" ? "" : "[" + resourceName + "]") + " sprites found at " + path);

		return result;
	}

	/** Finds and organises all the sprites required to display the gui. */
	public static void LoadGraphics()
	{
		DateTime startTime = DateTime.Now;

		Trace.Log("Loading graphic resources:");

		Instance.MiscSprites = LoadGraphicResources(MiscAtlasPath);
		Instance.MonsterSprites = LoadGraphicResources(MonsterAtlasPath);
		Instance.MapIconSprites = LoadGraphicResources(MapIconsAtlasPath);
		Instance.ItemIconSprites = LoadGraphicResources(ItemIconsAtlasPath);
		Instance.SpellIconSprites = LoadGraphicResources(SpellIconsAtlasPath);
		Instance.IconSprites = LoadGraphicResources(IconsAtlasPath, "NotFound");
		Instance.Portraits = SpriteLibrary.FromXML(PortraitsXMLPath);

		Util.Assert(TextureMagic.IsReadable(Instance.MapIconSprites[0].texture), "Map sprites atlas needs to be set to readable under import settings (advanced).");

		Trace.Log("Graphic resource loading completed in " + (DateTime.Now - startTime).TotalMilliseconds.ToString("0.0") + "ms.");

		_graphicsLoaded = true;		
	}

	/** Causes game to autosave (assuming everything is loaded up) */
	public static void SaveGame()
	{		
		Settings.SaveSettings();
		if (SaveFileLoaded)
			State.Save();
		PlayerPrefs.Save();
	}

	/** Save on pause, iOS calls this for soft shutdown.*/
	void OnApplicationPause(bool pauseStatus)
	{
		if (SaveOnExit && pauseStatus) {			
			SaveGame();
		}
	}

	/** Shuts the game down. */
	public void ShutDown()
	{
		WebServer.PostEvent("Shutdown");
		WebServer.PostCheckIn();
		if (SaveOnExit) {
			SaveGame();
		}
	}

	/** Save on exit */
	void OnApplicationQuit()
	{
		ShutDown();
	}

	/** Redraws the given map tile.  X and Y are 1 based */
	public void RefreshMapTile(int x, int y)
	{
		if (DungeonState == null)
			return;

		DungeonState.AutoMap.RenderTile(x, y, TileRenderMode.Full, true);
		DungeonState.AutoMap.MapCanvas.Apply();
	}

	/** Processes a list of objects, finding any that can be formated, and replacing them with their string formatted
	 * representation.  I.e. if the list contains a spell the spell will be replaced with the colorised name of the spell */
	public static object[] FormatParameters(object[] args)
	{
		var list = new List<object>();
		foreach (var obj in args) {
			object formattedObject = null;
			if (obj is MDRCharacter)
				formattedObject = CoM.Format(obj as MDRCharacter);
			if (obj is MDRSpell)
				formattedObject = CoM.Format(obj as MDRSpell);
			if (obj is MDRItemInstance)
				formattedObject = CoM.Format(obj as MDRItemInstance);
			list.Add(formattedObject ?? obj);
		}
		return list.ToArray();
	}

	/** Pops all states, returns to main menu. */
	public static void ReturnToMainMenu(object source, EventArgs e)
	{
		ReturnToMainMenu();
	}

	/** Pops all states, returns to main menu. */
	public static void ReturnToMainMenu()
	{
		PopAllStates();
		PartyController.Instance.Party = null;
		PushState(new MenuDungeonBackground());
		PushState(new MainMenuState());
	}

	/** 
	 * Converts coins into an amount of gold, silver, and copper. 
	 * @param value Number of coins.  I.e. 104 = 1 silver, 4 copper.
	 * @param full If true gold, silver, and copper will always be mentioned even if they are zero.
	 * @param colorize.  Values will be colorised.
	 */
	public static string CoinsAmount(int value, bool full = false)
	{
		int gold = (int)(value / 10000); 
		int silver = (int)(value / 100) % 100; 
		int copper = value % 100; 

		string goldAmount = Util.Colorise(gold, Color.yellow) + " gold ";
		string silverAmount = Util.Colorise(silver, Color.yellow) + " silver ";
		string copperAmount = Util.Colorise(copper, Color.yellow) + " copper ";

		bool showGold = gold != 0 || full;
		bool showSilver = silver != 0 || full;
		bool showCopper = copper != 0 || full || (value == 0);

		string result = (showGold ? goldAmount : "") + (showSilver ? silverAmount : "") + (showCopper ? copperAmount : "");
		return result.TrimEnd(' ');
	}

	/** Displays a new message to the player */
	public static void PostMessage(string message, params object[] args)
	{
		MessageLog.Add(new MessageEntry(String.Format(message, FormatParameters(args))));
		if (OnMessagePosted != null)
			OnMessagePosted(null, new EventArgs());
	}

	/** 
	 * Returns all the characters that are currently located in the same area as the player.
	 * @param includePartyMembers if true current party memebers will be included in the list 
	 * STUB: for the moment we include characters anywhere in the game, also need to implement area not location matching
	 */
	static public List<MDRCharacter> GetCharactersInCurrentArea(bool includePartyMembers = false)
	{
		List<MDRCharacter> result = new List<MDRCharacter>();
		foreach (MDRCharacter character in CharacterList) {
			bool inParty = Party.Contains(character);
			//bool sameLocation = ((character.LocationX == Party.LocationX) && (character.LocationY == Party.LocationY) && (character.Depth == Party.Depth));
			if ((!inParty) || includePartyMembers)
				result.Add(character);
		}
		return result;
	}

	// ----------------------------------------------
	// Formating
	// ----------------------------------------------

	/** Formats spell name, including coloring */
	public static string Format(MDRSpell spell)
	{
		return Util.Colorise(spell.Name, Colors.SPELL_COLOR);
	}

	/** Formats item instance including coloring */
	public static string Format(MDRItemInstance item)
	{
		return Util.Colorise(item.Name, item.Item.QualityColor);
	}


	/** Formats characters name, including coloring */
	public static string Format(MDRCharacter character)
	{
		return Util.Colorise(character.Name, Colors.CHARACTER_COLOR);
	}

	/** Formats monster name, including coloring.  Count is used for detecting plurals. */
	public static string Format(MDRMonster monster, int count = 1)
	{
		return Util.Colorise(monster.Name + ((count > 1) ? "s" : ""), Colors.MONSTER_COLOR);
	}

	/** Formats monster name, including coloring.  Count is used for detecting plurals. */
	public static string Format(MDRMonsterInstance monster, int count = 1)
	{
		return Format(monster.MonsterType, count);
	}

	public static string Format(int value)
	{
		return Util.Colorise(value.ToString(), Colors.VALUES_COLOR);
	}

	/** Removes parties with 0 characters. */
	public static void CleanParties()
	{
		for (int lp = PartyList.Count - 1; lp >= 0; lp--) {
			if (PartyList[lp].MemberCount == 0)
				PartyList.Remove(lp);
		}
	}
		
}