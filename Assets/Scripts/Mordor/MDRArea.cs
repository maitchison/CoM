using UnityEngine;

using System;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

using Engines;
using Data;

namespace Mordor
{
	/** Defines a area on the map.  Includes the current monsters, their temperament, and treasure */
	[DataObjectAttribute("Area")]
	public class MDRArea : DataObject
	{
		/** The id number of this area, unique only to the floor. */
		[FieldAttr(true)]
		public int ID;

		/** The mosnter that lives in this lair, null for no lair*/
		public MDRMonster LairedMonster;
		
		/** Mask indicating which types of monsters can spawn in this area. */
		public MDRSpawnMask SpawnMask;

		/** A location within the area, not defined, but usually the topleft tile. */
		public MDRLocation Origion;

		/** The map this area belongs to. */
		public MDRMap Map;

		public long RespawnTime {
			get { return CoM.State.SpawnManager.GetRespawnTime(this); }
			set { CoM.State.SpawnManager.SetRespawnTime(this, value); }
		}

		public List<MDRMonsterInstance> SpawnedMonsters {
			get { return CoM.State.SpawnManager.GetSpawnedMonsters(this); }
		}

		/** If true monsters spawned from this area can be one dungeon level deeper than normal. */
		public bool StudArea {
			get {
				return Map.GetField(Origion.X, Origion.Y).Stud;
			}
		}

		public MDRArea()
		{
			SpawnMask = new MDRSpawnMask(0);		
		}

		public override string ToString()
		{
			return Origion.ToString();
		}

		/** Returns if this area is a monsters lair or not */
		public bool IsLair {
			get { return LairedMonster != null; }
		}

		/** Returns a spawn table for this area. */
		public SpawnTable GetSpawnTable()
		{
			int minLevel = StudArea ? Map.FloorNumber + 1 : Map.FloorNumber - 2;
			int maxLevel = StudArea ? Map.FloorNumber + 1 : Map.FloorNumber;
			if (minLevel < 0)
				minLevel = 0;

			var spawnTable = new SpawnTable();
			spawnTable.BuildList(this, minLevel, maxLevel);
			return spawnTable;
		}

		/** Updates respawning in this area. */
		public void Update()
		{
			if (Map == null)
				throw new Exception("Area has no map assigned.");

			bool hasMonster = (SpawnedMonsters != null);			

			if (!hasMonster && RespawnTime == 0) {	
				//var interval = (10 + Map.FloorNumber * 5) * 60 * TimeSpan.TicksPerSecond;
				var interval = 5 * TimeSpan.TicksPerSecond;
				RespawnTime = DateTime.Now.Ticks + interval;
			}

			if (!hasMonster && DateTime.Now.Ticks >= RespawnTime) {
				Trace.LogDebug("Auto spawning monsters in area {0}", this);
				RespawnTime = 0;
				SpawnMonsters();
			}
		}

		//--------------------------------------------------------------------------------------------------------
		// Private
		//--------------------------------------------------------------------------------------------------------

		/** 
		 * Places a new instance of specific monster at a random location in this area. Will return with null if no
		 * spaces are avalaible in this area.
		 */
		private MDRMonsterInstance placeMonster(MDRMonster monster)
		{
			if (monster == null)
				return null;

			var location = getNextEmptyTile();
			if (location.Floor == -1)
				return null;

			var instance = MDRMonsterInstance.Create(monster);
			CoM.State.AddMonster(instance, this, location);
			return instance;
		}

		/** Returns the next empty tile from this area.  If no empty tiles can be found returns floor -1 */
		private MDRLocation getNextEmptyTile()
		{
			for (int xlp = 0; xlp < Map.Width; xlp++)
				for (int ylp = 0; ylp < Map.Height; ylp++)
					if (Map[xlp, ylp].Area == this && Map.GetMonsterAtLocation(xlp, ylp) == null)
						return new MDRLocation(xlp, ylp, Map.FloorNumber);
			return new MDRLocation(0, 0, -1);
		}

		/** Spawns a specific monster to this area (along with companions) and returns the "main" instance created. */
		private MDRMonsterInstance spawnMonster(MDRMonster selectedMonster)
		{
			if (selectedMonster == null)
				return null;

			if (selectedMonster.SpawnCount == 0)
				return null;			

			var mainInstance = placeMonster(selectedMonster);			

			// Add additional monster instances.
			for (int lp = 1; lp < selectedMonster.SpawnCount; lp++)
				placeMonster(selectedMonster);			

			// Add companions.
			if (selectedMonster.Companion != null)
				for (int lp = 0; lp < selectedMonster.Companion.SpawnCount; lp++)
					placeMonster(selectedMonster.Companion);

			return mainInstance;
		}


		/** Spawns new monsters appropriate for this area */ 
		public void SpawnMonsters()
		{			
			var selectedMonster = GetSpawnTable().SelectRandomItem();
			spawnMonster(selectedMonster);
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);

			if (LairedMonster != null)
				WriteValue(node, "LairedMonster", LairedMonster.ID);
			
			WriteValue(node, "SpawnMask", SpawnMask.Mask);
			WriteAttribute(node, "LocationX", Origion.X);
			WriteAttribute(node, "LocationY", Origion.Y);
			WriteAttribute(node, "Floor", Origion.Floor);
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			int lairedMonsterId = ReadInt(node, "LairedMonster", 0);
			if (lairedMonsterId > 0)
				LairedMonster = CoM.Monsters.ByID(lairedMonsterId);

			SpawnMask.Mask = ReadBitArray(node, "SpawnMask");

			Origion = new MDRLocation(ReadAttributeInt(node, "LocationX"), ReadAttributeInt(node, "LocationY"), ReadAttributeInt(node, "Floor"));
		}

		#endregion

	}
}

