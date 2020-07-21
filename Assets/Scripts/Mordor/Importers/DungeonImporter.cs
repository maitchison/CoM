
using System;
using System.IO;
using System.Collections;
using UnityEngine;

namespace Mordor.Importers
{
	/** Imports from MDATA formated files */
	//todo: rename dungeon Importer
	public class MapImporter
	{
		public int NumberOfAreas;
		public int NumberOfChutes;
		public int NumberOfTeleports;
	
		/** Used to read data from a MDATA file in a friendly way (i.e support for some VB strings and records) */
		private MDRFileReader data;

		/** 
		 * Configures the stream for input. 
		 * recordSize The size, in bytes, of each record in the source file
		 */
		private void SetupInput(Stream source, int recordSize = 1)
		{
			data = new MDRFileReader(source);
			data.RecordSize = recordSize;
			source.Position = 0;
		}

		public MDRDungeon LoadDungeon(Stream source)
		{
			SetupInput(source, 20);

			MDRDungeon result = new MDRDungeon();
			
			try {
				//find how many levels there are
				int floors = data.ReadMDRWord();
				
				//make sure it's reasonable
				if ((floors > 255) || (floors < 1)) {
					throw new Exception("Invalid number of levels in file, found " + floors + " expecting between 1 and 255");
				}

				//stub fixed width and height
				result.Initialize(32, 32, floors);
				
				for (int level = 1; level <= floors; level++) {
					result.Floor[level] = ReadMap(level);
					result.Floor[level].FloorNumber = level;
					if (result.Floor[level].Width != 32 || result.Floor[level].Height != 32)
						throw new Exception("Floors must all have the standard dimentions.  We expecting 32x32 but found " + result.Floor[level].Width + "x" + result.Floor[level].Height); 
				}
			} finally {
				data.Close();
			}
			
			return result;

		}

		/** 
		 * Reads the next map from the file.  
		 * levelNumberToLoad is the level to load, 1 being the first. 
		 */
		public MDRMap ReadMap(int levelNumberToLoad)
		{
			MDRMap map = new MDRMap();

			data.Seek(0, SeekOrigin.Begin);
			
			// find how many levels there are
			int numberOfLevels = data.ReadMDRWord();
			
			if ((levelNumberToLoad <= 0) || (levelNumberToLoad > numberOfLevels)) {
				data.Close();
				throw new Exception("Invalid level number " + levelNumberToLoad);
			}
			
			// get the offset
			data.RecordSeek(1 + levelNumberToLoad);
			int levelOffset = data.ReadMDRWord();    
			
			// load in the map header
			data.RecordSeek(levelOffset);
			int width = data.ReadWord();
			int height = data.ReadWord();
			int floorNumber = data.ReadWord();
			if (floorNumber != levelNumberToLoad) {
				data.Close();
				throw new Exception("Level number in file wrong, expecting " + levelNumberToLoad + " but found " + floorNumber);
			}
			
			NumberOfAreas = data.ReadWord();
			NumberOfChutes = data.ReadWord();
			NumberOfTeleports = data.ReadWord();

			// make sure we got some resonable results
			if ((width <= 0) || (width > 256) || (height <= 0) || (height > 256)) {
				data.Close();
				throw new Exception("Level dimentions invalid, found " + width + "x" + height + ", maximum size is 256x256");
			}

			if (NumberOfAreas > MDRMap.MAX_AREAS) {
				data.Close();
				throw new Exception("Too many areas in level, found " + NumberOfAreas + " but maximum is " + MDRMap.MAX_AREAS);
			}

			if (NumberOfChutes > MDRMap.MAX_CHUTES) {
				data.Close();
				throw new Exception("Too many chutes in level, found " + NumberOfChutes + " but maximum is " + MDRMap.MAX_CHUTES);
			}

			if (NumberOfTeleports > MDRMap.MAX_TELEPORTS) {
				data.Close();
				throw new Exception("Too many teleports in level, found " + NumberOfTeleports + " but maximum is " + MDRMap.MAX_TELEPORTS);
			}

			// initialize map with a rock frame
			FieldRecord rockTile = new FieldRecord(0, 0, null);
			rockTile.Rock = true;
			map.Initialize(width + 2, height + 2);
			map.FloorNumber = floorNumber;

			var AreaLocation = new Vector2[MDRMap.MAX_AREAS];

			// load field records
			data.RecordSeek(levelOffset + 1);
			
			for (int ylp = 1; ylp <= height; ylp++) {
				for (int xlp = 1; xlp <= width; xlp++) {
					
					FieldRecord fieldRecord = new FieldRecord(xlp, ylp, map);
					
					fieldRecord.AreaNumber = (ushort)data.ReadWord();
					
					//read the field bitvalues as a currency type, and the convert to byte data                    
					decimal decValue = data.ReadCurrency();
					Int64 value = Convert.ToInt64(decValue);
					byte[] bitData = BitConverter.GetBytes(value);

					AreaLocation[fieldRecord.AreaNumber] = new Vector2(xlp, ylp);
					
					fieldRecord.BitMask = new BitArray(bitData);

					// custom adjustment for grass
					if (fieldRecord.Dirt && fieldRecord.Water) {
						fieldRecord.Dirt = false;
						fieldRecord.Water = false;
						fieldRecord.Grass = true;
					}

					// adjust for new wall types
					if (fieldRecord._alt) {
						if (fieldRecord.NorthWall.Door) {
							fieldRecord.NorthWall = new WallRecord(WallType.Arch);
						}	
						if (fieldRecord.NorthWall.Secret) {
							fieldRecord.NorthWall = new WallRecord(WallType.Gate);
						}	
						if (fieldRecord.EastWall.Door) {
							fieldRecord.EastWall = new WallRecord(WallType.Arch);
						}	
						if (fieldRecord.EastWall.Secret) {
							fieldRecord.EastWall = new WallRecord(WallType.Gate);
						}	
					}
						
					// stub: remove traps
					fieldRecord.Chute = false;
					fieldRecord.Pit = false;
					fieldRecord.Teleporter = false;
					fieldRecord.FaceEast = false;
					fieldRecord.FaceWest = false;
					fieldRecord.FaceNorth = false;
					fieldRecord.FaceSouth = false;
						
					map[xlp, ylp] = fieldRecord;

					map[xlp, ylp].Explored = true;
					
					// skip the unused bytes in this 20 byte record
					data.NextRecord();
				}   
				
			}

			// rock outline
			WallRecord wall = new WallRecord(WallType.Wall);
			for (int lp = 0; lp <= width + 1; lp++) {
				map[lp, 0].Rock = true;
				map[lp, 0].NorthWall = wall;
				map[lp, height + 1].Rock = true;
				map[lp, height + 1].SouthWall = wall;
			}
			for (int lp = 0; lp <= height + 1; lp++) {
				map[0, lp].Rock = true;
				map[0, lp].EastWall = wall;
				map[width + 1, lp].Rock = true;
				map[width + 1, lp].WestWall = wall;
			}

			// Load areas.
			int localAreaCount = data.ReadMDRWord();
			NumberOfAreas = localAreaCount;
			//if (localAreaCount != NumberOfAreas)
			//	Trace.LogWarning("Area count missmatch expecting " + NumberOfAreas + " but found " + localAreaCount + " assuming " + NumberOfAreas);
				
			for (int lp = 0; lp < NumberOfAreas; lp++) {
				
				MDRArea area = new MDRArea();

				area.Origion = new MDRLocation((int)AreaLocation[lp].x, (int)AreaLocation[lp].y, floorNumber);

				area.SpawnMask = new MDRSpawnMask(data.ReadUInt32());
				int lairID = data.ReadWord();
				if (lairID != 0) {
					area.LairedMonster = CoM.Monsters.ByID(lairID);
					if (area.LairedMonster == null)
						Trace.LogWarning("Import Error [Missing Monster]: Can not find monster of ID:{0} for lair{2} at [{1}].", lairID, area.Origion, lp);
				} 

				// some areas will be invalid, so ignore areas with no spawnmask or lair id.
				if (!(area.SpawnMask.Mask.Count == 0 && lairID == 0)) {
					map.Area.Add(area);
					area.Map = map;
					area.ID = lp;
				}

				data.NextRecord();
			}
				
			data.SkipRecords(200 - NumberOfAreas);
			data.SkipRecords(1); // no idea why this is necessary, but there is a blank record here before we start with the teleporters

			// load teleports
			int localTeleportCount = data.ReadMDRWord();
			if (localTeleportCount != NumberOfTeleports)
				Trace.LogWarning("Teleport count missmatch expecting " + NumberOfTeleports + " but found " + localTeleportCount + " assuming " + NumberOfTeleports);

			for (int lp = 0; lp < NumberOfTeleports; lp++) {
				TeleportTrapInfo teleport = new TeleportTrapInfo();
				teleport.X = data.ReadWord();
				teleport.Y = data.ReadWord();
				teleport.DestX = data.ReadWord();
				teleport.DestY = data.ReadWord();
				teleport.DestFloor = data.ReadWord();

				data.NextRecord();

				if (teleport.IsValid)
					map.Teleport.Add(teleport);
			}

			// load chutes
			int localChuteCount = data.ReadMDRWord();
			if (localChuteCount != NumberOfChutes)
				Trace.LogWarning("Chute count missmatch expecting " + NumberOfChutes + " but found " + localChuteCount + " assuming " + NumberOfChutes);

			for (int lp = 0; lp < NumberOfChutes; lp++) {
				ChuteTrapInfo chute = new ChuteTrapInfo();
				chute.X = data.ReadWord();
				chute.Y = data.ReadWord();
				chute.DropDepth = data.ReadWord();

				data.NextRecord();
	
				if (chute.IsValid)
					map.Chute.Add(chute);
			}
			return map;
		}
	}
}

