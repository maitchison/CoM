using UnityEngine;

using System;
using System.Collections.Generic;

using Mordor;

enum GridSquareState
{
	Visible,
	Hidden,
	Unknown,
}


namespace Culler
{

	/** Represents a square on the grid we are going to apply culling to. */
	public class GridSquare
	{
		/** All objects, including lights and doors. */
		public List<GameObject> ObjectList;
		/** Just the doors in this square. */
		private List<GameObject> DoorList;
		/** Just the lights in this square. */
		private List<GameObject> LightList;

		private GridSquareState state = GridSquareState.Visible;

		/** Used to mark squares that will soon be visible / hidden without actually applying them. */
		internal bool marked;

		public GridSquare()
		{
			ObjectList = new List<GameObject>();
			DoorList = new List<GameObject>();
			LightList = new List<GameObject>();
			state = GridSquareState.Unknown;
		}

		protected void SetObject(GameObject obj, bool visible)
		{					
			bool isLight = LightList.Contains(obj);
			bool isDoor = DoorList.Contains(obj);

			// special case for doors, just hide them via renderer.
			if (isDoor) {
				var renderer = obj.GetComponent<MeshRenderer>();	
				if (renderer == null)
					return;
				renderer.enabled = visible;
				return;
			}

			if (isLight) {
				var lightScript = obj.GetComponent<FadeLightOn>();	
				if (visible && !obj.activeInHierarchy) {				
					obj.SetActive(true);
				}
				lightScript.EnableLight = visible;
			} else {
				obj.SetActive(visible);	
			}
		}

		/** Adds given object to this square */
		internal void AddObject(GameObject obj)
		{	
			if (!ObjectList.Contains(obj)) {
				
				ObjectList.Add(obj);
				state = GridSquareState.Unknown;

				bool isLight = obj.GetComponent<FadeLightOn>();	
				bool isDoor = obj.GetComponent<DoorScript>();	

				if (isLight)
					LightList.Add(obj);
				if (isDoor)
					DoorList.Add(obj);				
			}
		}

		internal void SetVisible(bool value, bool force = false)
		{
			if (value) {			
				if (!force && state == GridSquareState.Visible)
					return;
				SetAllObjects(true);
				state = GridSquareState.Visible;
			} else {
				if (!force && state == GridSquareState.Hidden)
					return;
				state = GridSquareState.Hidden;
				SetAllObjects(false);
			}
		}

		/** Removes all objects from this square */
		internal void Reset()
		{
			ObjectList.Clear();
			LightList.Clear();
			DoorList.Clear();
		}

		protected void SetAllObjects(bool visible)
		{
			for (int lp = 0; lp < ObjectList.Count; lp++)
				SetObject(ObjectList[lp], visible);
		}
	}


	/** 
 * Class to handle game culling.  Map is split into a grid and objects placed into each grid sector.
 * Objects that straddle grid secords should be placed in multiple sectors.
 * 
 * During culling an A* method is applied to the map starting at the partyies current location.  This traces
 * the map to find all sectors that the player could travel to without hitting a wall or a door, thus building
 * a potentialy visible set.
 * 
 */
	public class AStarCuller
	{
		/** Array for each sector, contains a list of objects in that sector */
		private GridSquare[,] ObjectGrid;

		/** Fast access to all objects by name */
		private Dictionary<string,GameObject> AllObjects;

		/** Map identifying locations we can see through. */
		private bool[,,] TransitMap;

		/** Trace map used in A* culling */
		private int[,] TraceMap;

		/** Previous location information, so we don't have to recull if not needed */
		private string previousHash;

		/** Just used for debuging */
		private int cullTests = 0;

		/** The map we will trace */
		public MDRMap Map { get { return _map; } set { setMap(value); } }

		private MDRMap _map;

		/** The current party */
		private MDRParty Party;

		private const int gridWidth = 32;
		private const int gridHeight = 32;

		/** If culling is currently applied */
		public bool IsCulled { get { return _isCulled; } }

		private bool _isCulled;

		bool enabled = false;

		// -------------------------------------------------------
		// Public
		// -------------------------------------------------------

		public AStarCuller()
		{
			TransitMap = new bool[gridWidth, gridHeight, 2];
			TraceMap = new int[gridWidth, gridHeight];
			ObjectGrid = new GridSquare[gridWidth, gridHeight];

			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					ObjectGrid[xlp, ylp] = new GridSquare();
		}


		/** Finds dungeon objects and places them into approriate grid squares. */
		public void Initialize(MDRParty party, int mapNumber)
		{
			Util.Assert(party != null, "Invalid null party for culler.");
			Party = party;
			Map = party.Map;
			enabled = true;

			RemoveAllObjects();

			GameObject root = GameObject.Find("Map" + mapNumber);

			AllObjects = new Dictionary<string, GameObject>();

			if (root == null || root.transform == null) {
				Trace.LogWarning("Can not initialize culler, no map number {0}", mapNumber);
				return;
			}

			var objects = root.transform.AllChildren();

			int totalObjects = objects.Count;
			int placedlObjects = 0;
			int globalObjects = 0;

			foreach (var item in objects) {

				// If the object is disabled the bounds will not register, so I need to enable it.  This is very strange behavour, and might be new to unity 5.2.1?
				item.gameObject.SetActive(true);

				var itemRenderer = item.GetComponent<MeshRenderer>();
				if (itemRenderer == null) {
					AllObjects[item.name] = item.gameObject;
					globalObjects++;
					if (item.GetComponent<Light>() != null) {
						AddObject(item.gameObject, (int)(item.position.x + 0.5f), (int)(item.position.z + 0.5f));
						if (item.GetComponent<FadeLightOn>() != null) {
							// start all lights off, one ones visibile will fade on.
							item.GetComponent<FadeLightOn>().EnableLight = false;
							item.GetComponent<FadeLightOn>().Sync();
						}
					}
					continue;
				}

				var bounds = itemRenderer.bounds;
				var itemCollider = item.GetComponent<Collider>();
				if (itemCollider != null)
					bounds = itemCollider.bounds;

				bounds.center = bounds.center + new Vector3(0.5f, 0, 0.5f);

				float sizeAdjustment = 0;

				// Make sure aligned 1x1 objects only take up 1 cell by reducing their size a little
				if (bounds.extents.magnitude > 0.0001f)
					bounds.extents = bounds.extents - Vector3.one * 0.0001f;

				int xmin = Mathf.FloorToInt(bounds.min.x + sizeAdjustment);
				int xmax = Mathf.FloorToInt(bounds.max.x - sizeAdjustment);
				int ymin = Mathf.FloorToInt(bounds.min.z + sizeAdjustment);
				int ymax = Mathf.FloorToInt(bounds.max.z - sizeAdjustment);

				if (xmin > xmax)
					xmin = xmax;
				if (ymin > ymax)
					ymin = ymax;
				
				for (int xlp = xmin; xlp <= xmax; xlp++)
					for (int ylp = ymin; ylp <= ymax; ylp++) {
						AddObject(item.gameObject, xlp, ylp);
						placedlObjects++;
					}					
			}

			Trace.Log("Placed {0} objects from a pool of {1}, {2} global objects.", placedlObjects, totalObjects, globalObjects);

			cull(true);

		}

		public List<GameObject> GetObjectsAtLocation(int gridX, int gridY)
		{
			gridX = (int)Util.Clamp(gridX, 0, gridWidth - 1);
			gridY = (int)Util.Clamp(gridY, 0, gridHeight - 1);
			return new List<GameObject>(ObjectGrid[gridX, gridY].ObjectList);
		}

		/** Adds game object at given grid location */
		public void AddObject(GameObject obj, int gridX, int gridY)
		{
			gridX = (int)Util.Clamp(gridX, 0, gridWidth - 1);
			gridY = (int)Util.Clamp(gridY, 0, gridHeight - 1);

			ObjectGrid[gridX, gridY].AddObject(obj);
		}

		/** Removes references to all objects this culler is responsiable for */
		public void RemoveAllObjects()
		{
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					ObjectGrid[xlp, ylp].Reset();
		}

		/** 
	 * Lets the culler know that the wall section on this grid square and direction can be seen through. 
	 */
		public void SetCanTransit(int gridX, int gridY, Direction direction, bool value)
		{
			if (CanTransit(gridX, gridY, direction) == value)
				return;

			int x = gridX + ((direction.isWest) ? -1 : 0);
			int y = gridY + ((direction.isSouth) ? -1 : 0);

			x = Util.ClampInt(x, 0, gridWidth - 1);
			y = Util.ClampInt(y, 0, gridHeight - 1);

			bool vertical = (direction.DX == 0);

			TransitMap[x, y, vertical ? 0 : 1] = value;

			Invalidate();
		}

		/** Disables all culling, shows all objects */
		public void Reset()
		{
			previousHash = "";
			_isCulled = false;
			ShowAll();
		}

		/** Forces culling to update next time cull is called */
		public void Invalidate()
		{
			previousHash = "";
		}

		/** Displays map showing where transits can occur */
		public void DrawTransitMap()
		{
			SmartUI.DrawFillRect(new Rect(10, 10, 10 * gridWidth, 10 * gridHeight), Color.black);
			for (int ylp = 0; ylp < gridHeight; ylp++)
				for (int xlp = 0; xlp < gridWidth; xlp++) {
					if (!CanTransit(xlp, gridWidth - ylp - 1, Direction.NORTH))
						SmartUI.DrawFillRect(new Rect(xlp * 10, ylp * 10, 10, 1), Color.white);
					if (!CanTransit(xlp, gridWidth - ylp - 1, Direction.WEST))
						SmartUI.DrawFillRect(new Rect(xlp * 10, ylp * 10, 1, 10), Color.white);
				}
		}

		private void cull(bool force = false)
		{
			string locationHash = Party.LocationX + "," + Party.LocationY + "," + Party.Facing.ToString();
			if (locationHash == previousHash && !force)
				return;

			previousHash = locationHash;

			DateTime startTime = DateTime.Now;

			cullTests = 0;

			markAll(false);
			ClearTraceMap();

			int x = (int)Util.Clamp(Party.LocationX, 0, gridWidth - 1);
			int y = (int)Util.Clamp(Party.LocationY, 0, gridHeight - 1);
			AStar(x, y);

			applyMarkings();

			DateTime finishTime = DateTime.Now;

			_isCulled = true;
		}

		/** 
	 * Culls objects based on given party location and map.
	 */
		public void Update()
		{
			if (Party == null)
				return;
		
			if (!enabled)
				return;				

			Map = Party.Map;

			if (Map == null)
				return;

			cull();
		}

		/** Disables culling, shows all objects. */
		public void Disable()
		{
			enabled = false;
			Reset();
		}

		// -------------------------------------------------------
		// Private
		// -------------------------------------------------------

		/** Calculates if an object may transit from the given location in the given direction.  Walls block transit */
		private bool CanTransit(int gridX, int gridY, Direction direction)
		{
			int x = gridX + ((direction.isWest) ? -1 : 0);
			int y = gridY + ((direction.isSouth) ? -1 : 0);

			x = Util.ClampInt(x, 0, gridWidth - 1);
			y = Util.ClampInt(y, 0, gridHeight - 1);

			bool vertical = (direction.DX == 0);

			return TransitMap[x, y, vertical ? 0 : 1];
		}

		/** 
	 * Performs an itteration of the A* algorithm 
	 * 
	 * @param gridX the X location on the grid
	 * @param gridY the Y locaiton on the grid
	 * @param steps The current itteration level
	 */
		private void AStar(int gridX, int gridY, int steps = 0)
		{
			if (steps > 16)
				return;
			if (gridX < 0)
				return;
			if (gridY < 0)
				return;
			if (gridX >= gridWidth)
				return;
			if (gridY >= gridHeight)
				return;

			if (TraceMap[gridX, gridY] <= steps)
				return;

			ObjectGrid[gridX, gridY].marked = true;
			TraceMap[gridX, gridY] = steps;
			cullTests++;

			if (CanTransit(gridX, gridY, Direction.NORTH))
				AStar(gridX, gridY + 1, steps + 1);

			if (CanTransit(gridX, gridY, Direction.SOUTH))
				AStar(gridX, gridY - 1, steps + 1);

			if (CanTransit(gridX, gridY, Direction.EAST))
				AStar(gridX + 1, gridY, steps + 1);

			if (CanTransit(gridX, gridY, Direction.WEST))
				AStar(gridX - 1, gridY, steps + 1);
		}

		private void ClearTraceMap()
		{		
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					TraceMap[xlp, ylp] = 255;
		}

		private void ClearTransitMap()
		{		
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++) {
					TransitMap[xlp, ylp, 0] = false;
					TransitMap[xlp, ylp, 1] = false;
				}
		}

		/** 
	 * Configures culler for given map. 
	 */
		private void setMap(MDRMap value)
		{
			if (Map == value)
				return;

			_map = value;
			BuildTransitMap();
			Invalidate();
		}

		/**
	 * Works out which wall sections can be seen through 
	 */
		private void BuildTransitMap()
		{
			ClearTransitMap();
			if (Map == null)
				return;
			for (int ylp = 0; ylp < gridHeight; ylp++)
				for (int xlp = 0; xlp < gridWidth; xlp++) {
					FieldRecord tile = Map.GetField(xlp, ylp);
					SetCanTransit(xlp, ylp, Direction.NORTH, tile.NorthWall.CanSeeThrough);
					SetCanTransit(xlp, ylp, Direction.EAST, tile.EastWall.CanSeeThrough);
				}
		}

		private void ShowAll()
		{
			setAll(true);
		}

		/** Makes all objects visible or invisible */
		private void setAll(bool value)
		{
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					ObjectGrid[xlp, ylp].SetVisible(value);
		}

		/** Makes all marked squares visible, all unmarked ones invisible. */
		private void applyMarkings()
		{
			/* doing this twice like this makes sure an object overlapping two squares isn't turned off by the second square. */
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					if (!ObjectGrid[xlp, ylp].marked)
						ObjectGrid[xlp, ylp].SetVisible(false);
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					if (ObjectGrid[xlp, ylp].marked)
						ObjectGrid[xlp, ylp].SetVisible(true, true);
			
		}


		/** Makes all objects invisible */
		private void markAll(bool value)
		{
			for (int xlp = 0; xlp < gridWidth; xlp++)
				for (int ylp = 0; ylp < gridHeight; ylp++)
					ObjectGrid[xlp, ylp].marked = value;
		}
	}
}
