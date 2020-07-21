using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mordor;
using System;

public enum DungeonBuildMethod
{
	/** All maps are generated when game loads.  All maps will be stored in memory, which could be around 100 megs for 15 levels. */
	BuildOnStart,
	/** Only a single map is every loaded at a time.  Map will be generated as SelectedMap is set. */
	BuildOnDemand
}

[ExecuteInEditMode]
public class DungeonBuilder : MonoBehaviour
{
	public int Seed = 0;

	public List<GameObject> FloorTiles;
	public List<GameObject> DirtTiles;
	public GameObject CeilingTile;
	public GameObject GrassTile;

	public GameObject CrossBeam;

	public GameObject WallObject;
	public GameObject WaterObject;
	public GameObject WaterEdge;
	public GameObject WaterEdgeHalf;
	public GameObject WaterEdgeCorner;
	public GameObject ColumnObject;

	public GameObject SidewalkWall;

	public GameObject DoorFrameObject;
	public GameObject DoorObject;
	public GameObject IronBarGateObject;
	public GameObject SecretDoorObject;

	public GameObject CeilingLight;
	public GameObject StairsUp;
	public GameObject StairsDown;

	public bool GenerateFloor = true;
	public bool GenerateWalls = true;
	public bool GenerateCeiling = true;

	/** Adds random cosmetic details to the dungeon such as gratings. */
	public bool GenerateDetails = true;

	public DungeonBuildMethod BuildMethod { get { return _buildMethod; } set { setBuildMethod(value); } }

	[HideInInspector]
	[SerializeField]
	private DungeonBuildMethod _buildMethod = DungeonBuildMethod.BuildOnDemand;

	/** Records if each map is built or not. */
	private bool[] isBuilt;

	/** Dictionary of all the objects we created by name. */
	private Dictionary<string, GameObject> createdObjectsDictionary;

	/** Our instance. */
	private static DungeonBuilder instance;

	private static string[] directionNames = { "North", "East", "South", "West" };
	private static string[] cornerNames = { "NorthWest", "NorthEast", "SouthEast", "SouthWest" };

	/** Details to be placed in dungeon. */
	public FurnishingList Furnishings;

	[Range(1, 30)]
	/** Defines the maxiumum size of the map to load.  Defaults to 0 which means any size.  Setting this to a lower numebr can be helpful during testing as only a few map cells will be generated. */
	public int MaxSize = 0;

	/** Changes the currently active map. */
	private int _selectedMap = -1;

	/** Tell the generator that all maps will need to be regenrated. */
	public void MarkAllMapsAsDirty()
	{
		for (int lp = 0; lp < isBuilt.Length; lp++)
			isBuilt[lp] = false;	
	}

	public int SelectedMap {
		get {
			return _selectedMap;
		}
		set {		

			bool isNewMapBuilt = isValidMapIndex(value) ? isBuilt[value] : true;

			if (_selectedMap == value && isNewMapBuilt)
				return;
			_selectedMap = value;

			UpdateRequiredMaps();

			if (IsInitialized)
				updateVisibleMap();
		}
	}

	/** Sets the dungeon, triggering a rebuild of maps. */
	public MDRDungeon Dungeon {
		get {
			return _dungeon;
		} 
		set {
			if (_dungeon == value)
				return;
			_dungeon = value;

			MarkAllMapsAsDirty();
			
			UpdateRequiredMaps();
		}
	}

	private MDRDungeon _dungeon;

	/** Returns if the currently selected map is built or not, or returns false if the current map is out of bounds. */
	private bool isSelectedMapBuilt {
		get {
			if (_selectedMap < 0 || _selectedMap >= Maps)
				return false;
			return isBuilt[_selectedMap];
		}
	}

	private int objectsCreated = 0;

	public int Maps {
		get {
			if (Dungeon == null)
				return 0;
			else
				return Dungeon.Floors;
		}
	}

	public bool IsInitialized {
		get { return (Dungeon != null); }
	}

	/** The folder to put generated objects in. */
	private Transform getFabNode(int mapIndex)
	{
		return getMapNode(mapIndex).FindOrCreateChild("Fab"); 
	}

	/** The folder to put generated deteail objects in. */
	private Transform getDetailNode(int mapIndex)
	{
		return getMapNode(mapIndex).FindOrCreateChild("Details"); 
	}

	private Transform getMapNode(int mapIndex)
	{
		return dungeonNode.FindOrCreateChild(getMapNodeName(mapIndex)); 
	}

	private MDRMap Map(int mapIndex)
	{
		if (!isValidMapIndex(mapIndex))
			return null;
		else
			return Dungeon.Floor[mapIndex];
	}

	/** The folder to put all generated objects in. */
	private Transform dungeonNode {
		get { return transform; }
	}

	/** Makes sure all the maps that need to be are built, and frees any that aren't needed */
	private void UpdateRequiredMaps()
	{
		for (int lp = 1; lp <= Maps; lp++) {
			bool needsToBeBuilt = (BuildMethod == DungeonBuildMethod.BuildOnStart) || (lp == SelectedMap);
			if (needsToBeBuilt && !isBuilt[lp])
				rebuildMap(lp);
			if (!needsToBeBuilt && isBuilt[lp])
				clearMap(lp);
		}
	}

	/** If an instance is set, initializes and rebuilds that dungeon instance. */
	public static void InitializeAndRebuildDungeon()
	{
		if (instance == null) {
			return;
		}
		instance.Initialize();
		instance.RebuildDungeon();
	}

	/** Sets the build method, potentially generating, or realsing maps in the process. */
	private void setBuildMethod(DungeonBuildMethod value)
	{		
		if (value == _buildMethod)
			return;
		
		_buildMethod = value;

		UpdateRequiredMaps();
	}

	public DungeonBuilder()
	{
		instance = this;

		createdObjectsDictionary = new Dictionary<string, GameObject>();

		isBuilt = new bool[MDRDungeon.MAX_FLOORS];

		MarkAllMapsAsDirty();
	}

	/** Returns the node name for given map number */
	private string getMapNodeName(int mapIndex)
	{
		return "Map" + mapIndex;
	}

	/** Makes the currently selected map visibile and all others hidden */
	private void updateVisibleMap()
	{		
		if (!IsInitialized)
			throw new Exception("Dungeon not allocated.");
		
		for (int lp = 1; lp <= Maps; lp++) {
			var node = dungeonNode.FindOrCreateChild(getMapNodeName(lp));
			node.gameObject.SetActive(lp == SelectedMap);			
		}			
	}

	/** Checks if the right child nodes exist, if they don't creates them. */
	private void setupNodes()
	{
		if (!IsInitialized)
			return;

		for (int lp = 1; lp <= Maps; lp++) {
			var node = dungeonNode.FindOrCreateChild(getMapNodeName(lp));
			node.FindOrCreateChild("Fab");
			node.FindOrCreateChild("Details");
			node.FindOrCreateChild("Custom");
		}
	}

	/** Permanently deletes all child objects in given node. */
	private void cleanNode(Transform node)
	{
		if (node == null)
			return;

		var children = new List<GameObject>();
		foreach (Transform child in node)
			children.Add(child.gameObject);
		children.ForEach(DestroyImmediate);
	}

		

	/** Removes all fabricated objects from given map. */
	private void clearMap(int mapIndex)
	{	
		cleanNode(getFabNode(mapIndex));
		cleanNode(getDetailNode(mapIndex));

		createdObjectsDictionary.Clear();

		instance.isBuilt[mapIndex] = false;
	}

	public static string NameFromProperites(int xpos, int ypos, string direction, string name)
	{
		return string.Format("{0:00}{1:00}-{2}-{3}", xpos, ypos, direction, name);
	}

	private void markReference(GameObject obj, int gridX, int gridY, Direction direction)
	{
		// Add a reference if the script is there.
		GridReference gridReference = obj.GetComponent<GridReference>();
		if (gridReference != null) {
			gridReference.GridX = gridX;
			gridReference.GridY = gridY;
			gridReference.Direction = direction;
		}

		if (gameObject.transform.childCount > 0) {
			foreach (Transform child in obj.transform) {
				markReference(child.gameObject, gridX, gridY, direction);
			}
		}
	}

	/** Rotates object given number of degrees about it's up axis, then translates it. */
	private void rotateAndTranslate(GameObject obj, float angle, Vector3 translation)
	{
		var newRotation = obj.transform.localRotation.eulerAngles;
		newRotation.y += angle;
		obj.transform.localRotation = Quaternion.Euler(newRotation);
		//obj.transform.Rotate(obj.transform.up, angle);
		obj.transform.localPosition = Quaternion.Euler(0, angle, 0) * obj.transform.localPosition;
		obj.transform.localPosition += translation;
	}

	/** 
	 * Instantiates a game object representation of a map tile field.
	 * location will adjust the rotation and position of an object so that it lines up with the given wall.
	 * Offset, offsets the object before rotation.
	 */
	private GameObject InstantiateObject(Transform parentNode, GameObject obj, float xpos, float ypos, string location = "Center", string niceName = "", Vector3 positionOffset = new Vector3())
	{
		if (obj == null)
			return null;

		if (parentNode == null)
			throw new Exception("No parent node has been set.");

		if (niceName == "")
			niceName = obj.name;

		int gridX = (int)xpos;
		int gridY = (int)ypos;

		string objectIdentifier = NameFromProperites(gridX, gridY, location, niceName);

		if (createdObjectsDictionary.ContainsKey(objectIdentifier)) {
			// duplicate object.
			return null;
		}

		GameObject newObject = Instantiate<GameObject>(obj);

		createdObjectsDictionary.Add(objectIdentifier, newObject);

		newObject.transform.localPosition += positionOffset;


		newObject.name = objectIdentifier;

		// perform location transformation
		if (location == "North")
			rotateAndTranslate(newObject, 0, new Vector3(0.0f, 0.0f, 0.5f));

		if (location == "East")
			rotateAndTranslate(newObject, 90, new Vector3(0.5f, 0.0f, 0.0f));

		if (location == "South")
			rotateAndTranslate(newObject, 180, new Vector3(0.0f, 0.0f, -0.5f));
					
		if (location == "West")
			rotateAndTranslate(newObject, -90, new Vector3(-0.5f, 0.0f, 0.0f));

		// corners
		if (location == "NorthWest")
			rotateAndTranslate(newObject, 0, new Vector3(-0.5f, 0.0f, 0.5f));

		if (location == "NorthEast")
			rotateAndTranslate(newObject, 90, new Vector3(0.5f, 0.0f, 0.5f));

		if (location == "SouthEast")
			rotateAndTranslate(newObject, 180, new Vector3(0.5f, 0.0f, -0.5f));
		
		if (location == "SouthWest")
			rotateAndTranslate(newObject, -90, new Vector3(-0.5f, 0.0f, -0.5f));

		newObject.transform.localPosition += new Vector3(xpos, 0.0f, ypos);

		newObject.transform.SetParent(parentNode, false);

		markReference(newObject, gridX, gridY, Direction.FromString(location));

		objectsCreated++;

		return newObject;
	}

	/** Deletes all constructed dungeon elements for all maps.*/
	public static void ClearAll()
	{
		if (instance == null)
			return;

		for (int lp = 1; lp <= instance.Maps; lp++) {			
			instance.clearMap(lp);
		}
	}

	/** 
	 * Deletes then recreates all objects for all maps in dungeon. 
	 * Custom added objects will remain. 
	 */
	public void RebuildDungeon()
	{
		if (!IsInitialized)
			return;

		for (int lp = 1; lp <= Maps; lp++) {			
			rebuildMap(lp);
		}
	}

	/** Finds the best rotation for given object so that it either faces away from a wall, or towards a non wall. */
	private float AutoRotation(FieldRecord field)
	{
		int wallCount = 0;
		for (int lp = 0; lp < 4; lp++)
			if (field.getWallRecord(lp))
				wallCount++;

		// 1 wall face away from wall, 3 walls face towards open space.  Otherwise default to north.
		for (int lp = 0; lp < 4; lp++) {
			if (field.getWallRecord(lp).IsEmpty && wallCount == 3)
				return lp * 90;
			if (!field.getWallRecord(lp).IsEmpty && wallCount == 1)
				return lp * 90 + 180;
		}
		return 0;
	}

	private bool isValidMapIndex(int mapIndex)
	{
		return (mapIndex >= 1 && mapIndex <= Maps);
	}

	public void RebuildSelectedMap()
	{
		rebuildMap(SelectedMap);
	}

	/** Adds details to given tile. */
	private void generateFurnishings(Transform detailsNode, FieldRecord tile)
	{		
		if (Furnishings == null || Furnishings.Count == 0)
			return;

		if (tile.Empty || tile.Rock)
			return;

		// Groups used for this tile.
		var groupsUsed = new List<int>();

		// process walls...
		for (int wallIndex = 0; wallIndex < 4; wallIndex++) {
			
			if (!tile.getWallRecord(wallIndex).Wall)
				continue;
			
			for (int lp = 0; lp < Furnishings.Count; lp++) {
				
				var item = Furnishings[lp];
				if (!item.Enabled || item.Source == null || item.Chance == 0 || item.Placement != DetailPlacementMethod.Wall)
					continue;

				var groupID = (item.GroupID % 65536) + ((1 + wallIndex) * 65536);

				if (groupsUsed.Contains(groupID))
					continue;
				
				float diceRoll = Util.SeededRandom(tile.X + tile.Y + "wall_detail" + lp + wallIndex) * 100f;

				if (diceRoll < item.Chance) {					

					var offsetVector = new Vector3(0, 0, 0);
					if (item.PositionVariation)
						offsetVector.x = Util.SeededRandom(tile.X + tile.Y + "offset" + lp + wallIndex) * 0.6f - 0.3f;

					InstantiateObject(detailsNode, item.Source, tile.X, tile.Y, directionNames[wallIndex], item.Source.name, offsetVector);
					groupsUsed.Add(groupID);
				}
			}
		}			

		// process corners...
		for (int wallIndex = 0; wallIndex < 4; wallIndex++) {

			if (!(tile.getWallRecord(wallIndex).Wall && tile.getWallRecord((wallIndex + 3) % 4).Wall))
				continue;

			for (int lp = 0; lp < Furnishings.Count; lp++) {

				string seedBase = tile.X + " " + tile.Y + " " + wallIndex + " " + lp + ":";

				var item = Furnishings[lp];
				if (!item.Enabled || item.Source == null || item.Chance == 0 || item.Placement != DetailPlacementMethod.Corner)
					continue;

				var groupID = (item.GroupID % 65536) + ((5 + wallIndex) * 65536);

				if (groupsUsed.Contains(groupID))
					continue;

				float diceRoll = Util.SeededRandom(seedBase, "corner_detail", wallIndex) * 100f;

				if (diceRoll < item.Chance) {					

					var offsetVector = new Vector3(0, 0, 0);
					if (item.PositionVariation) {
						offsetVector.x = Util.SeededRandom(seedBase, "offsetx") * 0.3f - 0.15f;					
						offsetVector.z = Util.SeededRandom(seedBase, "offsetz") * 0.3f - 0.15f;					
					}

					var rotationVector = new Vector3(0, 0, 0);
					if (item.RotationVariation) {
						rotationVector.y = Util.SeededRandom(seedBase, "rotation") * 360f;	
					}

					var createdObject = InstantiateObject(detailsNode, item.Source, tile.X, tile.Y, cornerNames[wallIndex], item.Source.name, offsetVector);

					createdObject.transform.localRotation = Quaternion.Euler(createdObject.transform.localRotation.eulerAngles + rotationVector);

					groupsUsed.Add(groupID);
				}
			}
		}			

		// process floor...

		if (!(tile.Water || tile.Dirt || tile.Grass)) {
			for (int lp = 0; lp < Furnishings.Count; lp++) {
				var item = Furnishings[lp];
				if (!item.Enabled || item.Source == null || item.Chance == 0 || item.Placement != DetailPlacementMethod.Floor)
					continue;

				var groupID = (item.GroupID % 65536) + ((0) * 65536);

				if (groupsUsed.Contains(groupID))
					continue;

				float diceRoll = (Util.SeededRandom(tile.X, tile.Y, "floor_detail" + lp) * 100f);

				if (diceRoll < item.Chance) {					

					var offsetVector = new Vector3(0, 0, 0);
					if (item.PositionVariation) {
						offsetVector.x = Util.SeededRandom(tile.X, tile.Y, "offsetx" + lp) * 0.8f - 0.4f;					
						offsetVector.z = Util.SeededRandom(tile.X, tile.Y, "offsetz" + lp) * 0.8f - 0.4f;					
					}

					var rotationVector = new Vector3(0, 0, 0);
					if (item.RotationVariation) {
						rotationVector.y = Util.SeededRandom(tile.X, tile.Y, "rotation" + lp) * 360f;	
					}

					var createdObject = InstantiateObject(detailsNode, item.Source, tile.X, tile.Y, "Center", item.Source.name, offsetVector);

					createdObject.transform.localRotation = Quaternion.Euler(createdObject.transform.localRotation.eulerAngles + rotationVector);

					groupsUsed.Add(groupID);
				}
			}
		}
	}

	/** 
	 * Deletes then recreates all objects for given map.
	 * Custom added objects will remain. 
	 */
	private void rebuildMap(int mapIndex)
	{		
		if (!IsInitialized)
			throw new Exception("Can not rebuild map, dungeon not initialized.");

		if (!isValidMapIndex(mapIndex))
			return;

		Trace.Log("Rebuilding map {0} with Ceiling: {1}", mapIndex, GenerateCeiling);

		UnityEngine.Random.seed = Seed + (mapIndex);

		var startTime = DateTime.Now;

		clearMap(mapIndex);

		objectsCreated = 0;
		var lightsCreated = 0;

		var fabNode = getFabNode(mapIndex);
		var detailsNode = getDetailNode(mapIndex);

		//create map objects 
		int mapHeight = (MaxSize == 0) ? Map(mapIndex).Height : Util.ClampInt(Map(mapIndex).Height, 1, MaxSize);
		int mapWidth = (MaxSize == 0) ? Map(mapIndex).Width : Util.ClampInt(Map(mapIndex).Width, 1, MaxSize);
		for (int ylp = 0; ylp <= mapHeight; ylp++) {
			for (int xlp = 0; xlp <= mapWidth; xlp++) {
				// floor
				FieldRecord field = Map(mapIndex).GetField(xlp, ylp);
				 
				if (field == null)
					continue;

				if (GenerateCeiling) {

					int roll = (int)(Util.SeededRandom("ceiling", xlp, ylp) * 16);

					var ceilingTile = CeilingTile;
					var ceilingObject = InstantiateObject(fabNode, ceilingTile, xlp, ylp);
					ceilingObject.transform.Rotate(ceilingObject.transform.up, 90 * (int)(roll / 4));

					// generate the cross beams
					if (CrossBeam != null) {
						InstantiateObject(fabNode, CrossBeam, xlp, ylp, "North");
						InstantiateObject(fabNode, CrossBeam, xlp, ylp, "East");
					}
				}

				if (GenerateFloor) {

					// Floor:
					if (field.Water) {
						InstantiateObject(fabNode, WaterObject, xlp, ylp);
					} else if (field.Grass) {						
						InstantiateObject(fabNode, GrassTile, xlp, ylp);
					} else if (field.Dirt) {
						var floorObject = DirtTiles[Util.Roll(DirtTiles.Count) - 1];
						InstantiateObject(fabNode, floorObject, xlp, ylp);
					} else if (!field.Rock) {
						var floorObject = FloorTiles[Util.Roll(FloorTiles.Count) - 1];
						InstantiateObject(fabNode, floorObject, xlp, ylp);
					}

					// Tile Edges:
					bool stepNorth = (field.EdgeHeight < field.North.EdgeHeight);
					bool stepSouth = (field.EdgeHeight < field.South.EdgeHeight);
					bool stepEast = (field.EdgeHeight < field.East.EdgeHeight);
					bool stepWest = (field.EdgeHeight < field.West.EdgeHeight);

					if (stepNorth)
						InstantiateObject(fabNode, field.NorthWall ? WaterEdgeHalf : WaterEdge, xlp, ylp, "North");
					if (stepSouth)
						InstantiateObject(fabNode, field.SouthWall ? WaterEdgeHalf : WaterEdge, xlp, ylp, "South");
					if (stepEast)
						InstantiateObject(fabNode, field.EastWall ? WaterEdgeHalf : WaterEdge, xlp, ylp, "East");
					if (stepWest)
						InstantiateObject(fabNode, field.WestWall ? WaterEdgeHalf : WaterEdge, xlp, ylp, "West");

					if (stepNorth || stepEast)
						InstantiateObject(fabNode, WaterEdgeCorner, xlp + 0.5f, ylp + 0.5f);
					if (stepNorth || stepWest)
						InstantiateObject(fabNode, WaterEdgeCorner, xlp - 0.5f, ylp + 0.5f);
					if (stepSouth || stepEast)
						InstantiateObject(fabNode, WaterEdgeCorner, xlp + 0.5f, ylp - 0.5f);
					if (stepSouth || stepWest)
						InstantiateObject(fabNode, WaterEdgeCorner, xlp - 0.5f, ylp - 0.5f); 

					// stairs
					GameObject stairs = null;
					if (field.StairsUp)
						stairs = InstantiateObject(fabNode, StairsUp, xlp, ylp);
					if (field.StairsDown)
						stairs = InstantiateObject(fabNode, StairsDown, xlp, ylp);
					if (stairs != null) {
						float stairDirection = AutoRotation(field);
						stairs.transform.RotateAround(new Vector3(xlp, 0, ylp), new Vector3(0, 1f, 0), stairDirection);
						markReference(stairs, xlp, ylp, new Direction(stairDirection));
					}
				}

				if (GenerateWalls) {

					// Wall:
					if (field.NorthWall.Wall)
						InstantiateObject(fabNode, WallObject, xlp, ylp, "North", "Wall");	
					if (field.NorthWall.Secret)
						InstantiateObject(fabNode, SecretDoorObject, xlp, ylp, "North", "Secret");	

					if (field.EastWall.Wall)
						InstantiateObject(fabNode, WallObject, xlp, ylp, "East", "Wall");	
					if (field.EastWall.Secret)
						InstantiateObject(fabNode, SecretDoorObject, xlp, ylp, "East", "Secret");	

					// Columns:
					if (!field.NorthWall.IsEmpty || !field.EastWall.IsEmpty)
						InstantiateObject(fabNode, ColumnObject, xlp + 0.5f, ylp + 0.5f);	
					if (!field.NorthWall.IsEmpty)
						InstantiateObject(fabNode, ColumnObject, xlp - 0.5f, ylp + 0.5f);	
					if (!field.EastWall.IsEmpty)
						InstantiateObject(fabNode, ColumnObject, xlp + 0.5f, ylp - 0.5f);	


					// sidewalks 
					if (!field.Rock && !field.Water && !field.Dirt && !field.Grass) {
						for (int lp = 0; lp < 4; lp++) {
							if (field.getWallRecord(lp).Wall || field.getWallRecord(lp).Secret)
								InstantiateObject(fabNode, SidewalkWall, xlp, ylp, directionNames[lp]);							
						}
					}
						
					// doors
					if (field.NorthWall.Door) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "North");
						InstantiateObject(fabNode, DoorObject, xlp, ylp, "North", "Door");
					}

					if (field.EastWall.Door) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "East");
						InstantiateObject(fabNode, DoorObject, xlp, ylp, "East", "Door");
					}

					// gates
					if (field.NorthWall.Gate) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "North");
						InstantiateObject(fabNode, IronBarGateObject, xlp, ylp, "North", "Gate");
					}

					if (field.EastWall.Gate) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "East");
						InstantiateObject(fabNode, IronBarGateObject, xlp, ylp, "East", "Gate");
					}


					// arches
					if (field.NorthWall.Arch) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "North");
					}

					if (field.EastWall.Arch) {
						InstantiateObject(fabNode, DoorFrameObject, xlp, ylp, "East");
					}

					// lightsfloorObject
					if (field.Light) {
						lightsCreated++;
						InstantiateObject(fabNode, CeilingLight, xlp, ylp);
					}						

					if (GenerateDetails)
						generateFurnishings(detailsNode, field);

				}
			}
		}		
			
		isBuilt[mapIndex] = true;

		Trace.Log("Rebuilt map {2}, created {0} objects {1} lights, took {3:0.00} seconds.", objectsCreated, lightsCreated, mapIndex, (DateTime.Now - startTime).TotalSeconds);
	}

	/** This is a temporary function to load a map file, later it will be configurable, but right now its hard coded to load via CoM. */
	public void Initialize()
	{
		Trace.Log("Initializing dungeon creator.");

		if (!CoM.GameDataLoaded)
			CoM.LoadGameData();
		
		Dungeon = CoM.Dungeon;

		setupNodes();
	}
}