using System;
using System.Collections;

namespace Mordor
{
	/// <summary>
	/// Stores spawning information about a specific tile in a mordor map
	/// </summary>
	public class FieldRecord
	{
		private int _x;
		private int _y;
		private MDRMap map;
		private ushort areaNumber;

		//todo: should be private
		public BitArray BitMask = new BitArray(64);

		/// <summary>
		/// The area this field belongs to
		/// </summary>
		public ushort AreaNumber { get { return areaNumber; } set { areaNumber = value; } }

		public MDRArea Area {
			get {				
				if (map == null)
					throw new Exception("Map is null on field.");
				if (map.Area == null)
					throw new Exception("Map has a null area.");
				if (AreaNumber < 0)
					return null;
				if (AreaNumber >= map.Area.Count)
					return null;
				return map.Area[AreaNumber]; 
			}
		}

		/** 
		 * Returns the transit information at given tile.  If "None" is returned it means we can travel from given 
		 * tile in given direction without anything blocking.  Otherwise the obstical is returned, which will be the 
		 * first of either a wall, monster, or door.		 
		 */
		public TransitObstacal GetTransit(Direction direction)
		{			
			var wall = getWallRecord(direction.Sector);

			if (wall.Wall)
				return TransitObstacal.Wall;

			return TransitObstacal.None;
		}


		/// <summary>
		/// Returns the bit represented by index for this field
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetBit(int index)
		{
			return BitMask[index];
		}

		public int X				{ get { return _x; } }

		public int Y				{ get { return _y; } }


		public WallRecord NorthWall { 
			get { 				
				if (BitMask[1])
					return new WallRecord(WallType.Wall);
				if (BitMask[3])
					return new WallRecord(WallType.Door);
				if (BitMask[5])
					return new WallRecord(WallType.Secret);
				if (BitMask[37])
					return new WallRecord(WallType.Gate);
				if (BitMask[39])
					return new WallRecord(WallType.Arch);
				return new WallRecord(WallType.None);
			} 
			set {
				BitMask[1] = value.Wall;
				BitMask[3] = value.Door;
				BitMask[5] = value.Secret;
				BitMask[37] = value.Gate;
				BitMask[39] = value.Arch;
			} 
		}

		public WallRecord EastWall { 
			get { 
				if (BitMask[0])
					return new WallRecord(WallType.Wall);
				if (BitMask[2])
					return new WallRecord(WallType.Door);
				if (BitMask[4])
					return new WallRecord(WallType.Secret);
				if (BitMask[36])
					return new WallRecord(WallType.Gate);
				if (BitMask[38])
					return new WallRecord(WallType.Arch);

				return new WallRecord(WallType.None);
			}
			set {
				BitMask[0] = value.Wall;
				BitMask[2] = value.Door;
				BitMask[4] = value.Secret;
				BitMask[36] = value.Gate;
				BitMask[38] = value.Arch;
			} 
		}

		public WallRecord SouthWall {
			get {
				if (South != null)
					return South.NorthWall;
				else
					return WallRecord.Empty();
			}
			set {
				if (South != null)
					South.NorthWall = value;
			}
		}

		public WallRecord WestWall {
			get {
				if (West != null)
					return West.EastWall;
				else
					return WallRecord.Empty();
			}
			set {
				if (West != null)
					West.EastWall = value;
			}
		}

		/** Relative height of floor segment.  Used to detect transitions from differing heights. */
		public float FloorHeight {
			get { 
				if (StairsUp)
					return 0.15f;
				if (Water)
					return -0.20f;
				if (Pit)
					return -0.10f;
				if (Dirt)
					return -0.01f;
				if (Grass)
					return -0.01f;
				return 0;
			}
		}

		/** Used to work out what kind of stepping to use between tiles  */
		public float EdgeHeight {
			get { 
				if (Water)
					return -0.20f;
				if (Dirt)
					return -0.01f;
				if (Grass)
					return -0.005f;
				return 0;
			}
		}

		public FieldRecord West 	{ get { return map.GetField(X - 1, Y); } }

		public FieldRecord South	{ get { return map.GetField(X, Y - 1); } }

		public FieldRecord East 	{ get { return map.GetField(X + 1, Y); } }

		public FieldRecord North	{ get { return map.GetField(X, Y + 1); } }

		public bool FaceNorth       { get { return BitMask[6]; } set { BitMask[6] = value; } }

		public bool FaceEast        { get { return BitMask[7]; } set { BitMask[7] = value; } }

		public bool FaceSouth       { get { return BitMask[8]; } set { BitMask[8] = value; } }

		public bool FaceWest        { get { return BitMask[9]; } set { BitMask[9] = value; } }

		public bool Extinguisher    { get { return BitMask[10]; } set { BitMask[10] = value; } }

		public bool Pit             { get { return BitMask[11]; } set { BitMask[11] = value; } }

		public bool StairsUp        { get { return BitMask[12]; } set { BitMask[12] = value; } }

		public bool StairsDown      { get { return BitMask[13]; } set { BitMask[13] = value; } }

		public bool Teleporter      { get { return BitMask[14]; } set { BitMask[14] = value; } }

		public bool Water 			{ get { return BitMask[15]; } set { BitMask[15] = value; } }

		public bool Dirt 			{ get { return BitMask[16]; } set { BitMask[16] = value; } }

		public bool Rotator         { get { return BitMask[17]; } set { BitMask[17] = value; } }

		public bool Antimagic       { get { return BitMask[18]; } set { BitMask[18] = value; } }

		public bool Rock            { get { return BitMask[19]; } set { BitMask[19] = value; } }

		public bool Chute           { get { return BitMask[21]; } set { BitMask[21] = value; } }

		public bool Stud            { get { return BitMask[22]; } set { BitMask[22] = value; } }

		public bool Light           { get { return BitMask[23]; } set { BitMask[23] = value; } }

		/** denotes alternate for tile, i.e. doors become arches, secrets become iron bar doors */
		public bool _alt           { get { return BitMask[24]; } set { BitMask[24] = value; } }

		public bool Lava           { get { return BitMask[25]; } set { BitMask[25] = value; } }

		public bool Grass           { get { return BitMask[26]; } set { BitMask[26] = value; } }

		public bool Explored		{ get { return BitMask[31]; } set { BitMask[31] = value; } }


		public FieldRecord(int atX, int atY, MDRMap parentMap)
		{
			this._x = atX;
			this._y = atY;
			this.map = parentMap;
		}

		/** Returns if given bit represents a wall. */
		public static bool isWallBit(int bit)
		{
			return isNorthWallBit(bit) || isEastWallBit(bit);
		}

		public static bool isNorthWallBit(int bit)
		{
			return (bit == 1) || (bit == 3) || (bit == 5) || (bit == 37) || (bit == 39);
		}

		public static bool isEastWallBit(int bit)
		{
			return (bit == 0) || (bit == 2) || (bit == 4) || (bit == 36) || (bit == 38);
		}

		/** Represents tile as a 64bit value */
		public UInt64 Value {
			get { return toInt64(); }
			set { fromInt64(value); }
		}

		/** Represents tile as a 64bit value but without walls */
		public UInt64 GroundValue {
			get { 
				BitArray modifiedBits = (BitArray)BitMask.Clone();
				for (int lp = 0; lp < 64; lp++)
					if (isWallBit(lp))
						modifiedBits.Set(lp, false);
				byte[] data = new byte[8];
				BitMask.CopyTo(data, 0);
				return BitConverter.ToUInt64(data, 0);
			}
		}

		/** Converts tile bitmask to Int64 */
		private UInt64 toInt64()
		{
			byte[] data = new byte[8];
			BitMask.CopyTo(data, 0);
			return BitConverter.ToUInt64(data, 0);
		}

		/** Converts int64 to bitmask */
		private void fromInt64(UInt64 input)
		{
			byte[] data = BitConverter.GetBytes(input);
			BitMask = new BitArray(data);
		}

		/** Gets wall by direction */
		public WallRecord getWallRecord(Direction direction)
		{
			return getWallRecord(direction.Sector);
		}


		/** Gets wall by index.  0 = north, 1 = east, 2 = south, 3 = west */
		public WallRecord getWallRecord(int index)
		{
			switch (index) {
				case 0:
					return NorthWall;
				case 1:
					return EastWall;
				case 2:
					return SouthWall;
				case 3: 
					return WestWall;
			}
			throw new Exception("Invalid wall index: " + index);
		}

		/** Sets wall by index.  0 = north, 1 = east, 2 = south, 3 = west */
		public void setWallRecord(int index, WallRecord record)
		{
			switch (index) {
				case 0:
					NorthWall = record;
					return;
				case 1:
					EastWall = record;
					return;
				case 2:
					SouthWall = record;
					return;
				case 3: 
					WestWall = record;
					return;
			}
			throw new Exception("Invalid wall index: " + index);
		}

		/** Returns true if this tile is empty */
		public bool Empty
		// only check the lower 32 bits 
		{ get { return ((toInt64() & UInt32.MaxValue) == 0); } }

		public void Clear()
		{
			Value = 0;
		}

		/** 
		 * Sets this field to the same field values as the source with different copy modes (see tileCopy enum) 
		 */
		public void CopyFrom(FieldRecord source, TileCopyMode mode = TileCopyMode.STANDARD)
		{
			switch (mode) {
				case TileCopyMode.STANDARD:
					BitMask = new BitArray(source.BitMask);
					break;
				case TileCopyMode.FULL:
					BitMask = new BitArray(source.BitMask);
					if (South != null) {
						SouthWall = source.SouthWall;
					}
					if (West != null) {	
						WestWall = source.WestWall;
					}
					break;
			}
			Explored = source.Explored;
		}

	}
}

