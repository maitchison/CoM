using System;
using Mordor;
using Data;
using UnityEngine;
using System.Collections.Generic;

/** A class to hold and manage all our gamestate data such as characters, monster instances etc. */
public class GameStateManager
{
	protected const int SAVE_VERSION = 2;

	public SpawnManager SpawnManager;

	/** Holds game statistics such as monsters killed etc. */
	public GameRecords GameStats;

	/** The town shop */
	public MDRStore Store;

	/** A copy of the dungeon with only the explored parts defined */
	public MDRDungeon ExploredDungeon;

	/** List of the games characters */
	public MDRCharacterLibrary CharacterList;

	/** List of the games parties */
	public MDRPartyLibrary PartyList;

	/** True when game state is loaded and ready to be used. */
	public bool Loaded { get { return _loaded; } }

	private bool _loaded = false;

	private int currentAreaUpdateIndex = 0;

	public GameStateManager()
	{
		GameStats = new GameRecords();
		SpawnManager = new SpawnManager();
		CoM.Profile = new UserProfile();
		_loaded = false;
	}

	/** Updates all the active objects in the game. */
	public void Update()
	{
		updateMonsters();
		updateMonsterAvatars();
		updateAreas();
	}

	/** Saves just the characters. */
	public void SaveCharacters()
	{
		StateStorage.SaveData("Characters", CharacterList);
	}

	/** Saves all game state data to permant storage. */
	public void Save()
	{
		if (!Loaded) {
			Trace.LogWarning("Tried to save game before game was initialized.");
			return;
		}

		DateTime startTime = DateTime.Now;

		CoM.Profile.SaveVersion = SAVE_VERSION;
		CoM.Profile.SaveTimeStamp = DateTime.Now;

		StateStorage.SaveData("Characters", CharacterList);
		StateStorage.SaveData("Parties", PartyList);
		StateStorage.SaveData("ExploredDungeon", ExploredDungeon);
		StateStorage.SaveData("Store", Store);
		StateStorage.SaveData("GameStats", GameStats);
		StateStorage.SaveData("SpawnData", SpawnManager);
		StateStorage.SaveData("Profile", CoM.Profile);

		TimeSpan delta = (DateTime.Now - startTime);
		Trace.Log("Game saved. [" + delta.TotalMilliseconds.ToString("0.0") + "ms]");	
	}

	/** 
	 * Adds a new monster to the game.
	 * @param instance the monster instance to add
	 * @param area, the area this monster is associated with.
	 * @param location, the location this monster should be spawned at.
	 */
	public void AddMonster(MDRMonsterInstance instance, MDRArea area = null, MDRLocation? location = null)
	{
		if (instance == null)
			return;

		if (SpawnManager.Monsters.Contains(instance)) {
			Trace.LogWarning("Can not add monster instance {0} to table as it already has been added.", instance);
			return;
		}

		// associate monster with area.
		if (area != null) {
			CoM.State.SpawnManager.TrackSpawnedMonster(area, instance);
		}

		if (location != null) {
			instance.SpawnLocation = location.Value;
			instance.X = location.Value.X;
			instance.Y = location.Value.Y;
			//todo: floor instance.Floor = location.Value.Floor
		}

		SpawnManager.Monsters.Add(instance);
	}

	/** Restores game from save file. */
	public void Load()
	{
		Util.Assert(CoM.GameDataLoaded, "Data must be loaded before loading a save file.");

		DateTime startTime = DateTime.Now;

		GameStats = loadFromStoreDefault<GameRecords>("GameStats");
		Store = loadFromStoreDefault<MDRStore>("Store");
		ExploredDungeon = loadFromStoreDefault<MDRDungeon>("ExploredDungeon");
		CharacterList = loadFromStoreDefault<MDRCharacterLibrary>("Characters");
		PartyList = loadFromStoreDefault<MDRPartyLibrary>("Parties");
		SpawnManager = loadFromStoreDefault<SpawnManager>("SpawnData");

		Trace.Log("Save file loading completed in " + (DateTime.Now - startTime).TotalMilliseconds.ToString("0.0") + "ms.");

		_loaded = true;

		UpdateCharacterRecords(true);
	}

	/** 
	 * Deletes current gamestate (characters, explored map etc) and creates default data.  
	 * This will causes all characters, maps, etc to be lost 
	 */
	public void Reset()
	{
		if (!CoM.GameDataLoaded)
			throw new Exception("Can not reset save file until game data has been loaded.");

		SpawnManager = new SpawnManager();

		// Store.
		Store = new MDRStore();
		Store.SetDefault();

		GameStats.AddDefaultStats();

		// Explored dungeon.
		ExploredDungeon = new MDRDungeon();
		ExploredDungeon.Initialize(CoM.Dungeon.Width, CoM.Dungeon.Height, CoM.Dungeon.Floors);

		// characters and party
		CharacterList = loadFromStore<MDRCharacterLibrary>("DefaultCharacters");
		PartyList = loadFromStore<MDRPartyLibrary>("DefaultParty");

		Trace.Log("Save file reset.");

		_loaded = true;
	}

	/** 
	 * Checks characters to see if any of them set a new record.
	 * @param silent if true notifications will not be displayed when a record is set.
	 */
	public void UpdateCharacterRecords(Boolean silent = false)
	{
		if (silent)
			GameStats.PostNotifications = false;

		foreach (MDRCharacter character in CharacterList)
			GameStats.CheckCharacterRecords(character);

		if (silent)
			GameStats.PostNotifications = true;
	}


	/** Removes all monster instances and forces new respawns. */
	public void ForceRespawn()
	{
		SpawnManager.Monsters.Clear();
		SpawnManager.SpawnInfo.Clear();
		var currentMap = CoM.Party.Map;
		for (int lp = 0; lp < currentMap.Area.Count; lp++) {
			currentMap.Area[lp].RespawnTime = -1;
			currentMap.Area[lp].Update();
		}
	}
		
	//--------------------------------------------------------------------------------------------------------
	// Private
	//--------------------------------------------------------------------------------------------------------

	private void updateMonsters()
	{
		for (int lp = 0; lp < SpawnManager.Monsters.Count; lp++) {
			SpawnManager.Monsters[lp].Update();
		}
	}

	/** Makes sure that all the monster instances that need avatars have them. */
	private void updateMonsterAvatars()
	{
		if (CoM.Party.Map == null)
			return;			
		for (int lp = 0; lp < SpawnManager.Monsters.Count; lp++) {
			var monster = SpawnManager.Monsters[lp];
			if (monster.Controller == null) {
				monster.Controller = createMonsterAvatar(monster);
			}
		}
	}

	/** Updates spawning of new monsters by area. */
	private void updateAreas()
	{
		int AREAS_TO_UPDATE_PER_FRAME = 1;

		var currentMap = CoM.Party.Map;

		if (currentMap.Area.Count != 0)
			for (int lp = 0; lp < AREAS_TO_UPDATE_PER_FRAME; lp++) {
				currentAreaUpdateIndex++;
				if (currentAreaUpdateIndex >= currentMap.Area.Count)
					currentAreaUpdateIndex = 0;				
				currentMap.Area[currentAreaUpdateIndex].Update();
			}								
	}

	/** Creates an avatar for given monster instance. */
	private MonsterController createMonsterAvatar(MDRMonsterInstance monster)
	{
		if (monster == null)
			throw new ArgumentNullException("monster");

		Trace.LogDebug("Creating avatar for {0}", monster);
		Util.Assert(CoM.Instance.MonsterAvatar != null, "Monster avatar not assigned.");

		var avatar = GameObject.Instantiate(CoM.Instance.MonsterAvatar);

		var pixelMesh = avatar.Find("Pixel Mesh");

		Util.Assert(pixelMesh != null, "Monster avatar has no Pixel Mesh child.");

		// Create unique mesh for us.
		pixelMesh.GetComponent<MeshFilter>().mesh = new Mesh();		

		var script = avatar.GetComponent<MonsterController>();

		Util.Assert(script != null, "Monster avatar has no MonsterController script.");

		script.Sprite = monster.MonsterType.Portrait;
		script.X = monster.X;
		script.Y = monster.Y;
		script.Resync();


		return script;
	}

	/** Loads item from store, throws error if not found. */
	private T loadFromStore<T>(string key) where T: DataObject
	{
		if (StateStorage.HasData(key))
			return StateStorage.LoadData<T>(key);
		else
			throw new Exception(String.Format("Error reading from save file, expecting file {0} but it wasn't found.", key));		
	}

	/** Loads item from store, or creates a new default instance if not found. */
	private T loadFromStoreDefault<T>(string key) where T: DataObject, new()
	{
		if (StateStorage.HasData(key))
			return StateStorage.LoadData<T>(key);
		else {
			return new T();
		}
	}
}