
using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml.Linq;

using Data;

namespace Mordor
{

	public enum TileCopyMode
	{
		/** Tiles default contents is copied, this excludes the south and west walls */ 
		STANDARD,
		/** Tiles full contents is copied, including setting the south and west walls */ 
		FULL
	}

	/** Defines a dungeon trap */
	public class TrapInfo : DataObject
	{
		public int X;
		public int Y;

		/** Returns if this trap has sensiable values. */
		public bool IsValid {
			get { 
				return (X >= 1) && (Y >= 1) && (X <= MDRMap.MAX_WIDTH) && (Y <= MDRMap.MAX_HEIGHT); 
			} 
		}
	}

	/** Defines a teleporter trap */
	[DataObjectAttribute("Teleport", true)]
	public class TeleportTrapInfo : TrapInfo
	{
		public int DestX;
		public int DestY;
		public int DestFloor;

		/** Returns true if this is a random teleporter */
		public bool IsRandom()
		{
			return DestX == 0;
		}
	}

	/** Defines a chute trap */
	[DataObjectAttribute("Chute", true)]
	public class ChuteTrapInfo : TrapInfo
	{
		public int DropDepth;
	}

	public enum WallType
	{
		None,
		Wall,
		Door,
		Secret,
		Arch,
		Gate,
	}

	/** Represents a tile wall */
	public struct WallRecord
	{
		public WallType Type;

		public bool Wall { get { return Type == WallType.Wall; } }

		public bool Door { get { return Type == WallType.Door; } }

		public bool Secret { get { return Type == WallType.Secret; } }

		public bool Arch { get { return Type == WallType.Arch; } }

		public bool Gate { get { return Type == WallType.Gate; } }

		public bool IsEmpty { get { return Type == WallType.None; } }

		public WallRecord(WallType type)
		{
			this.Type = type;
		}

		public static WallRecord Empty()
		{
			return new WallRecord(WallType.None);
		}

		public static implicit operator bool(WallRecord foo)
		{
			return (foo.Wall);
		}

		public bool CanSeeThrough { get { return Type == WallType.Gate || Type == WallType.Arch || Type == WallType.None; } }
	}



	/** Defines the type of obstical that prevents moving in a given direction. */
	public enum TransitObstacal
	{
		None,
		Monster,
		Door,
		Wall
	}

	/**
     * Classs to store a single map.  Maps are often grouped together as a dungeon. 
	 */
	[DataObjectAttribute("Map")]
	public class MDRMap : NamedDataObject
	{
		/** The maximum number of areas a map can store */
		public static int MAX_AREAS = 201;

		/** The maximum number of teleport traps a map can store */
		public static int MAX_TELEPORTS = 200;

		/** The maximum number of chute traps a map can store */
		public static int MAX_CHUTES = 200;

		public static int MAX_WIDTH = 256;
		public static int MAX_HEIGHT = 256;

		/** The maps fields, unlike mordor co-ords these are zero based */
		private FieldRecord[,] _field;

		/** Defines the contents of each area on the map */
		public List<MDRArea> Area;

		/** Information about teleport traps on this level */
		public List<TeleportTrapInfo> Teleport;
		/** Information about chute traps on this level */
		public List<ChuteTrapInfo> Chute;
			
		/** Width of map in tiles */
		private int width = 0;

		public int Width { get { return width; } }

		/** Height of map in tiles */
		private int height = 0;

		/** Used to update only a few areas per frame. */
		private int currentAreaUpdateIndex = 0;

		public int Height { get { return height; } }

		/** The floor number of this map */
		private int floorNumber = 0;

		public int FloorNumber { get { return floorNumber; } set { floorNumber = value; } }

		/** Sets all field record tiles to empty */
		public void Clear()
		{
			for (int xlp = 0; xlp < width; xlp++)
				for (int ylp = 0; ylp < height; ylp++)
					this[xlp, ylp].Clear();
		}

		/** 2D Indexer to fields. */
		public FieldRecord this [int x, int y] {
			get {
				return GetField(x, y);
			}
			set {
				_field[x, y] = value;
			}
		}

		/**
         * Returns field at given location, or empty field if out of bounds
         * 
         * @param x Tile x co-ord
         * @param y Tile y co-ord
         * @returns The tile at given co-ords, empty field if out of bounds 
         */
		public FieldRecord GetField(int x, int y)
		{
			if (InBounds(x, y))
				return _field[x, y];
			else
				return new FieldRecord(x, y, this);
		}

		/** 
		 * Returns if the given tile co-ords are within the bounds of the map or not 
		 */
		public bool InBounds(int x, int y)
		{
			return ((x >= 0) && (y >= 0) && (x < Width) && (y < Height));
		}

		/** Initializes the map to a given size. */
		public void Initialize(int width, int height)
		{
			this.width = width;
			this.height = height;
			_field = new FieldRecord[width, height];
			Area = new List<MDRArea>();

			Chute = new List<ChuteTrapInfo>();
			Teleport = new List<TeleportTrapInfo>();

			for (int ylp = 0; ylp < height; ylp++) {
				for (int xlp = 0; xlp < width; xlp++) {
					_field[xlp, ylp] = new FieldRecord(xlp, ylp, this);
					_field[xlp, ylp].Explored = false;
				}
			}
		}

		/** Returns the monster at given tile co-ords, or null if none. */
		public MDRMonsterInstance GetMonsterAtLocation(int x, int y)
		{
			for (int lp = 0; lp < CoM.State.SpawnManager.Monsters.Count; lp++) {
				var monster = CoM.State.SpawnManager.Monsters[lp];
				if (monster.X == x && monster.Y == y)
					return monster;				
			}
			return null;
		}

		/** Looks through the trap list for the first trap at the given co-rds.  Returns it or null if not found */
		private TrapInfo SearchForTrap(TrapInfo[] trapList, int atX, int atY)
		{
			foreach (TrapInfo trap in trapList) {
				if ((trap.X == atX) && (trap.Y == atY))
					return trap;
			}
			return null;
		}

		/** Returns the chute information at given co-ords, or null if none found */
		public ChuteTrapInfo GetChutAt(int atX, int atY)
		{
			//stub: not the most efficent.
			return (ChuteTrapInfo)SearchForTrap(Chute.ToArray(), atX, atY);
		}

		/** Returns the telport information at given co-ords, or null if none found */
		public TeleportTrapInfo GetTeleportAt(int atX, int atY)
		{
			//stub: not the most efficent.
			return (TeleportTrapInfo)SearchForTrap(Teleport.ToArray(), atX, atY);
		}

		/** Reads map from XML node */
		override public void ReadNode(XElement node)
		{
			base.ReadNode(node);

			width = ReadInt(node, "Width");
			height = ReadInt(node, "Height");
			FloorNumber = ReadInt(node, "FloorNumber");

			if ((Width == 0) || (Height == 0)) {
				Trace.LogError("Error reading map, invalid dimentions (" + Width + "x" + Height + ")");
				return;
			}

			Initialize(Width, Height);
		
			ulong[] fieldData;

			fieldData = ReadArray<ulong>(node, "FieldMap");

			if (fieldData != null)
				for (int ylp = 0; ylp < width; ylp++)
					for (int xlp = 0; xlp < height; xlp++)
						_field[xlp, ylp].Value = fieldData[xlp + ylp * width];

			fieldData = ReadArray<ulong>(node, "AreaMap");

			if (fieldData != null)
				for (int ylp = 0; ylp < width; ylp++)
					for (int xlp = 0; xlp < height; xlp++)
						_field[xlp, ylp].AreaNumber = (ushort)fieldData[xlp + ylp * width];

			Teleport = ReadDataObjectList<TeleportTrapInfo>(node, "Teleports");
			Chute = ReadDataObjectList<ChuteTrapInfo>(node, "Chutes");
			Area = ReadDataObjectList<MDRArea>(node, "Areas");

			for (int lp = 0; lp < Area.Count; lp++) {				
				Area[lp].Map = this;
				Area[lp].ID = lp;
			}

			if (Teleport == null)
				throw new Exception("No teleports record found in map");
			if (Chute == null)
				throw new Exception("No chutes record found in map");
			if (Area == null)
				throw new Exception("No area record found in map");
		}

		/** Writes map to XML node */
		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);

			WriteValue(node, "Width", Width);
			WriteValue(node, "Height", Height);
			WriteValue(node, "FloorNumber", FloorNumber);
		
			WriteValue(node, "Teleports", Teleport);
			WriteValue(node, "Chutes", Chute);
			WriteValue(node, "Areas", Area);
		
			ulong[] mapData = new ulong[width * height];

			for (int ylp = 0; ylp < width; ylp++)
				for (int xlp = 0; xlp < height; xlp++)
					mapData[xlp + ylp * width] = _field[xlp, ylp].Value;
			WriteValue(node, "FieldMap", mapData);

			for (int ylp = 0; ylp < width; ylp++)
				for (int xlp = 0; xlp < height; xlp++)
					mapData[xlp + ylp * width] = _field[xlp, ylp].AreaNumber;
			WriteValue(node, "AreaMap", mapData);

		}
	}
}
