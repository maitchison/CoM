
using System;
using Mordor;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Data
{

	/** Records stats on a given monster */
	[DataObject("MonsterStat", true)]
	public class MonsterStatRecord : NamedDataObject
	{
		[FieldAttr(true)]
		public MDRMonster Monster;

		public int CharacterKills;
		public int NumberSeen;
		[FieldAttr(true)]
		public IdentificationLevel IDLevel;
		public String FirstSeenBy;
		public MDRLocation LastSeenLocation;
		public DateTime LastSeenDate;

		public MonsterStatRecord(MDRMonster monster)
		{
			this.Monster = monster;
			this.ID = monster.ID;
			this.Name = monster.Name;
			this.IDLevel = IdentificationLevel.Auto;
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);
			IDLevel = IdentificationLevel.FromName(ReadValue(node, "IDLevel", "none"));
		}

		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);
			WriteValue(node, "IDLevel", IDLevel.Name);
		}
	}

	/** Records stats on a given item */
	[DataObject("ItemStat", true)]
	public class ItemStatRecord : NamedDataObject
	{
		[FieldAttr(true)]
		public MDRItem Item;

		public int NumberFound;
		[FieldAttr(true)]
		public IdentificationLevel IDLevel;
		public String FirstFoundBy;
		public String LastFoundOnMonster;
		public DateTime LastSeenDate;

		public ItemStatRecord(MDRItem item)
		{
			this.Item = item;
			this.ID = item.ID;
			this.Name = item.Name;
			this.IDLevel = IdentificationLevel.Auto;
		}


		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);
			IDLevel = IdentificationLevel.FromName(ReadValue(node, "IDLevel", "none"));
		}

		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);
			WriteValue(node, "IDLevel", IDLevel.Name);
		}
	}

	/** Records a game record */
	[DataObject("Record", true)]
	public class GameRecord : NamedDataObject
	{
		public string RecordHolder;
		public int RecordValue;
		public string ObjectName;
		public string Message;
		public DateTime Date;

		public static GameRecord Create(string recordName, string recordHolder, int value, string message, string objectName = "")
		{
			var result = new GameRecord();
			result.Set(recordHolder, value);
			result.Message = message;
			result.Name = recordName;
			result.ObjectName = objectName;
			return result;
		}

		public GameRecord()
		{
		}

		public void Set(string recordHolder, int value, string objectName = "")
		{
			RecordHolder = recordHolder;
			RecordValue = value;
			ObjectName = objectName;
			Date = DateTime.Now;
		}

		public override string ToString()
		{
			return string.Format(Message ?? "Set by {0} with a record of {1}.", Util.Colorise(RecordHolder, Colors.CHARACTER_COLOR), Util.Colorise(Util.Comma(RecordValue), Colors.VALUES_COLOR), Util.Colorise(ObjectName, Colors.VALUES_COLOR));
		}
	}

	public class GameRecordLibrary : DataLibrary<GameRecord>
	{
		public GameRecordLibrary()
		{
			AutoID = true;
		}
	}

	/** 
 	* Records various game stats, such as which monsters have been killed, which items have been found,
	* and which characters have acheived various awards. 
 	*/
	[DataObject("GameStats", false)]
	public class GameRecords : DataObject
	{
		public GameRecordLibrary Records;
		public Dictionary<MDRMonster,MonsterStatRecord> MonsterStats;
		public Dictionary<MDRItem,ItemStatRecord> ItemStats;

		/** If true new records will be disabled to user via a notification. */
		public bool PostNotifications = true;

		private bool _enabled = true;

		/** Set this to false to disable record setting. */
		public bool Enabled {
			get {
				return _enabled;
			}
			set {
				if (_enabled == value)
					return;				
				_enabled = value;
			}
		}

		public GameRecords()
		{
			Records = new GameRecordLibrary();
			MonsterStats = new Dictionary<MDRMonster,MonsterStatRecord>();
			ItemStats = new Dictionary<MDRItem, ItemStatRecord>();
		}

		/** Checks if there is already a game record of this name.  If there isn't adds the given game record. */
		private void SetDefaultRecord(GameRecord record)
		{
			if (Records.ByName(record.Name) == null)
				Records.Add(record);

		}

		/** Checks game records creating any that need creating */
		private void InitDefaultGameRecords()
		{
			var toughMonster = "Gargoyle";

			var toughMonsterRaiting = 99;
			if (CoM.GameDataLoaded && CoM.Monsters[toughMonster] != null)
				toughMonsterRaiting = CoM.Monsters[toughMonster].ToughnessRaiting;

			SetDefaultRecord(GameRecord.Create("Strongest", "Crashland", 15, "Set by {0} with a strength of {1}."));
			SetDefaultRecord(GameRecord.Create("Smartest", "Mager", 15, "Set by {0} with an intelligence of {1}."));
			SetDefaultRecord(GameRecord.Create("Wisest", "Theshal", 15, "Set by {0} with a wisdom of {1}."));
			SetDefaultRecord(GameRecord.Create("Healthiest", "Orgal", 15, "Set by {0} with a constituation of {1}."));
			SetDefaultRecord(GameRecord.Create("Most Attractive", "Alaya", 15, "Set by {0} with a charisma of {1}."));
			SetDefaultRecord(GameRecord.Create("Quickest", "Nimblefingers", 15, "Set by {0} with a dexterity of {1}."));

			SetDefaultRecord(GameRecord.Create("Deadliest Creature Defeated", "Argon", toughMonsterRaiting, "Set by {0} defeating a {2}.", toughMonster));
			SetDefaultRecord(GameRecord.Create("Most Experienced Explorer", "Argon", 1322451, "Set by {0} having {1} total experience."));
			SetDefaultRecord(GameRecord.Create("Wealthiest Explorer", "Crashland", 5466543, "Set by {0} having {1} gold in the bank."));

		}

		/** Checks if given value execeeds the current record for given stat.  If it does it sets the new value */
		private void CheckCharacterRecord(string recordName, int value, MDRCharacter character, string objectName = "", string notificationMessage = "{0} has set a new record for being the {1} with a value of {2}.")
		{
			if (Records.ByName(recordName) == null)
				return;

			var record = Records[recordName];
			if (value > record.RecordValue) {

				if (record.RecordHolder != character.Name)
					PostNotification(string.Format(notificationMessage, CoM.Format(character), recordName, CoM.Format(value)), character.Portrait);
				record.Set(character.Name, value, objectName);
			}
		}

		/** Notifys the player of a new recod */
		private void PostNotification(string message, Sprite sprite = null)
		{
			if (PostNotifications && (CoM.AllDataLoaded))
				Engine.PostNotification(message, sprite);
		}

		/** Checks if character has broken any records, i.e. stats, or fighting, or money etc */
		public void CheckCharacterRecords(MDRCharacter character)
		{
			if (!Enabled)
				return;

			if (character == null)
				return;

			CheckCharacterRecord("Strongest", character.BaseStats.Str, character);
			CheckCharacterRecord("Smartest", character.BaseStats.Int, character);
			CheckCharacterRecord("Wisest", character.BaseStats.Wis, character);
			CheckCharacterRecord("Healthiest", character.BaseStats.Con, character);
			CheckCharacterRecord("Most Attractive", character.BaseStats.Chr, character);
			CheckCharacterRecord("Quickest", character.BaseStats.Dex, character);

			CheckCharacterRecord("Wealthiest Explorer", character.PersonalGold, character);
			CheckCharacterRecord("Most Experienced Explorer", character.TotalXP, character);
		}

		/** Registers that a given monster has been identified to given level */
		public void RegisterMonsterIdentified(MDRMonster monster, IdentificationLevel idLevel)
		{
			if (!Enabled)
				return;
			
			if (monster == null)
				return;
			
			if (idLevel == IdentificationLevel.Auto || idLevel == null)
				return;

			if (!MonsterStats.ContainsKey(monster))
				MonsterStats[monster] = new MonsterStatRecord(monster);
			MonsterStatRecord record = MonsterStats[monster];

			if (idLevel.ID > record.IDLevel.ID) {
				record.IDLevel = idLevel;
			}
		}

		/** Registers that a given item has been identified to given level */
		public void RegisterItemIdentified(MDRItem item, IdentificationLevel idLevel)
		{			
			if (!Enabled)
				return;

			if (item == null)
				return;
			if (idLevel == IdentificationLevel.Auto || idLevel == null)
				return;

			if (!ItemStats.ContainsKey(item))
				ItemStats[item] = new ItemStatRecord(item);
			ItemStatRecord record = ItemStats[item];

			if (idLevel.ID > record.IDLevel.ID) {
				record.IDLevel = idLevel;
			}
		}

		/** 
		 * Registers an item droped from a given monster. 
		 */
		public void RegisterItemFound(MDRCharacter character, MDRItemInstance itemInstance, MDRMonster monsterFoundOn)
		{
			if (!Enabled)
				return;

			if (itemInstance == null || itemInstance.Item == null)
				return;

			var item = itemInstance.Item;

			if (!ItemStats.ContainsKey(item))
				ItemStats[item] = new ItemStatRecord(item);
			ItemStatRecord record = ItemStats[item];

			if (record.NumberFound == 0) {
				record.FirstFoundBy = (character == null ? "unknown" : character.Name);
				if (character != null)
					PostNotification("Discovered " + itemInstance.Name, item.Icon);
			}

			record.NumberFound++;
			if (monsterFoundOn != null)
				record.LastFoundOnMonster = monsterFoundOn.Name;
			record.LastSeenDate = DateTime.Now;

			RegisterItemIdentified(item, itemInstance.IDLevel);
		}

		/**
		 * Registers a monster being killed by given character.  
		 * @param character The character that killed the monster
		 * @param monster The type of monster that was killed
		 * @param count The number of this kind of monster the character just killed 
		 */
		public void RegisterMonsterKilled(MDRCharacter character, MDRMonster monster, int count = 1)
		{
			if (!Enabled)
				return;
			
			character.MonstersKilled += count;

			var mostDangeriousMonsterRecord = Records["Deadliest Creature Defeated"];
			if (mostDangeriousMonsterRecord != null)
				CheckCharacterRecord("Deadliest Creature Defeated", monster.ToughnessRaiting, character, monster.Name, "{0} has set the record for defeating the most dangerious monster by killing a " + monster.Name + ".");
		}

		/**
		 * Registers a character being killed by a given monster.  
		 * @param monster The type of monster that was killed
		 * @param character The character that killed the monster
		 */
		public void RegisterCharacterKilledByMonster(MDRMonster monster, MDRCharacter character)
		{
			if (!Enabled)
				return;			
		}

		/**
		 * Registers the fact that a character has been killed 
		 */
		public void RegisterCharactersDeath(MDRCharacter character)
		{
			if (!Enabled)
				return;

			character.Deaths++;
		}

		/** 
		 * Registers a monster seen in a given area.  Each time this is called a new monster is registered, so register 
		 * only the first time the player enters an area. 
		 */
		/*
		public void RegisterMonsterSeen(MDRCharacter character, MDRMonsterInstance monsterFound)
		{
			if (!Enabled)
				return;

			if (monsterStackFound == null || monsterStackFound.Monster == null)
				return;
		
			MDRMonster monster = monsterStackFound.Monster;

			if (!MonsterStats.ContainsKey(monster)) {
				MonsterStats[monster] = new MonsterStatRecord(monster);
				if (character != null)
					PostNotification("Discovered " + monsterStackFound.Name, monster.Portrait);
			}

			MonsterStatRecord record = MonsterStats[monster];

			if (record.NumberSeen == 0)
				record.FirstSeenBy = (character == null ? "Unknown" : character.Name);

			record.NumberSeen += monsterStackFound.Count;
			if (area != null)
				record.LastSeenLocation = area.Origion;
			record.LastSeenDate = DateTime.Now;

			RegisterMonsterIdentified(monster, monsterStackFound.IDLevel);
		}
		*/

		/** 
		 * Records initial stats into the library.  
		 * I.e. easy to find monsters and items. 
		 */
		public void AddDefaultStats()
		{
			foreach (MDRMonster monster in CoM.Monsters) {
				if ((monster.EncounterChance >= 10) && (monster.AppearsOnLevel <= 1) && (monster.ToughnessRaiting < 40)) {
					//RegisterMonsterSeen(null, new MDRMonsterStack(monster), null);
					RegisterMonsterIdentified(monster, IdentificationLevel.Full);
				}
			}
			foreach (MDRItem item in CoM.Items) {
				if ((item.ChanceOfFinding >= 20) && (item.AppearsOnLevel < 1)) {
					RegisterItemFound(null, MDRItemInstance.Create(item, IdentificationLevel.Full), null);
				}
			}
			InitDefaultGameRecords();
		}

		/** Reads game stats to XML */
		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			Records = ReadDataObject<GameRecordLibrary>(node, "Records") ?? new GameRecordLibrary();
			InitDefaultGameRecords();

			// Monsters
			var monsterStatsNode = node.Element("MonsterStats");
			if (monsterStatsNode != null)
				foreach (var subNode in monsterStatsNode.Elements("Monster")) {
					int id = int.Parse(subNode.Attribute("ID").Value);
					var monster = CoM.Monsters.ByID(id);
					if (monster == null) {
						Trace.LogWarning("Stat record for monster [{0}] but no matching monster.", subNode.Attribute("ID").Value);
						continue;
					}
					var record = new MonsterStatRecord(monster);
					record.ReadNode(subNode);
					MonsterStats[monster] = record;
				}

			// Items
			var itemStatsNode = node.Element("ItemStats");
			if (itemStatsNode != null)
				foreach (var subNode in itemStatsNode.Elements("Item")) {
					int id = int.Parse(subNode.Attribute("ID").Value);
					var item = CoM.Items.ByID(id);
					if (item == null) {
						Trace.LogWarning("Data Error [Stat record]: Stats for item [{0}] but no matching item.", subNode.Attribute("ID").Value);
						continue;
					}
					var record = new ItemStatRecord(item);
					record.ReadNode(subNode);
					ItemStats[item] = record;
				}
		}

		/** Writes game stats to XML */
		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);

			WriteValue(node, "Records", Records);

			// Monsters
			var monsterStatsNode = new XElement("MonsterStats");
			foreach (MDRMonster monster in MonsterStats.Keys) {
				var record = MonsterStats[monster];
				WriteValue(monsterStatsNode, "Monster", record);
			}

			var itemStatsNode = new XElement("ItemStats");
			foreach (MDRItem item in ItemStats.Keys) {
				var record = ItemStats[item];
				WriteValue(itemStatsNode, "Item", record);
			}
				
			node.Add(monsterStatsNode);
			node.Add(itemStatsNode);


		}

	}
}