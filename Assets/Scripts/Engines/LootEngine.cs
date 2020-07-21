using System;
using System.Collections.Generic;

using Mordor;

using Data;
using System.Text;


namespace Engines
{
	public class LootTable : WeightedTable<MDRItem>
	{
		public int MinLevel { get { return _minLevel; } }

		public int MaxLevel { get { return _maxLevel; } }

		private int _minLevel;
		private int _maxLevel;

		/** Creates a loot list from the given entries under the following rules.
		 * The first matching condition will be followed:
		 * 1: entry matches "Any" all items will be added within level range at 25% probability.
		 * 2: entry matches a [MDRItemClass] name all items of that class and level range are added.
		 * 3: entry matches a [MDRItemType] name all items of that type and level range are added.
		 * 4: entry matches an item name, that items is added with 4x probability at a minimum weighting of 25 */
		public void BuildList(string[] entries, int minLevel, int maxLevel)
		{
			_minLevel = minLevel;
			_maxLevel = maxLevel;

			PotentialItems.Clear();
			foreach (string entryName in entries) {
				var entry = entryName.Trim();
				if (String.Compare(entry, "None", true) == 0) {
					continue;
				}

				if (String.Compare(entry, "Any", true) == 0) {
					foreach (MDRItem item in CoM.Items) {
						if ((item.AppearsOnLevel < minLevel) || (item.AppearsOnLevel > maxLevel))
							continue;
						addWeight(item, item.ChanceOfFinding * 0.25f);
					}
					continue;
				}

				var itemClass = CoM.ItemClasses.ByName(entry);
				if (itemClass != null) {
					foreach (MDRItem item in CoM.Items) {
						if ((item.AppearsOnLevel < minLevel) || (item.AppearsOnLevel > maxLevel))
							continue;
						if (item.Type.TypeClass == itemClass)
							addWeight(item, item.ChanceOfFinding);
					}
					continue;
				}

				var itemType = CoM.ItemTypes.ByName(entry);
				if (itemType != null) {
					foreach (MDRItem item in CoM.Items) {
						if ((item.AppearsOnLevel < minLevel) || (item.AppearsOnLevel > maxLevel))
							continue;
						if (item.Type == itemType)
							addWeight(item, item.ChanceOfFinding);
					}
					continue;
				}

				var itemSpecific = CoM.Items.ByName(entry);
				if (itemSpecific != null) {
					addWeight(itemSpecific, Math.Max(20, itemSpecific.ChanceOfFinding * 4));
					continue;
				}

				Trace.LogWarning("Data Error: [Loot Table] Entry {0} does not match an item, type or class. ", entryName);

			}
		}

		public override string DebugListing()
		{
			var baseListing = base.DebugListing();

			return string.Format("Item levels: [{0}-{1}]\n", MinLevel, MaxLevel) + baseListing;

		}
	}

	public class LootDrop
	{
		// What we rolled to see if anything dropped.
		public int DiceRoll;
		// The chance there was going to be an item in this drop.
		public float LootChance;

		public List<MDRItem> Loot;

		protected void Add()
		{
			
		}

		public LootDrop()
		{
			Loot = new List<MDRItem>();			
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			foreach (var item in Loot)
				result.Append(item + ",");
			return result.ToString().TrimEnd(',');			
		}
	}

	/** Handles loot drops from monsters */
	public class LootEngine
	{
		
		/** Calculates the drops from given area on a given map.  Result may be no, or many items */
		/*
		public static LootDrop Drop(MDRArea area)
		{
			var result = new LootDrop();

			float currentItemChance = area.Stack[0].Monster.ItemChance + Settings.Advanced.DropRateMod + area.TreasureContainerType.DropMod;

			int counter = 0;

			while (counter < 10) {
				int diceRoll = Util.Roll(100);

				if (counter == 0) {
					result.DiceRoll = diceRoll;
					result.LootChance = currentItemChance;
				}

				if (diceRoll <= currentItemChance) {
					MDRItem item = DropItem(area);
					bool alreadyDroppedItem = result.Loot.Contains(item);
					// There is always a 5% chance an item will not drop.  However we still get a second chance.  So monsters with drop rates of 200 or so will have a second chance.
					if (!alreadyDroppedItem && item != null && (Util.Roll(100) > 5)) {
						result.Loot.Add(item);
					}
				} else {
					break;
				}

				currentItemChance = (currentItemChance - 25) / 5f;

				counter++;
			} 
				
			return result;
		}

		public static LootTable GetLootTable(MDRMonster monster)
		{			
			// GetItemLevelRanges.
			int maxLevel = ,onster.ItemLevel;
			int minLevel = maxLevel / 2;

			// ApplyChestModifiertoItemLevelRanges.
			//maxLevel += area.TreasureContainerType.MaxLevelMod;
			//minLevel += area.TreasureContainerType.MinLevelMod;

			minLevel = Util.ClampInt(minLevel, 0, 99);
			maxLevel = Util.ClampInt(maxLevel, 1, 99);

			if (minLevel > maxLevel)
				minLevel = maxLevel;

			var lootTable = new LootTable();
			lootTable.BuildList(monster.LootList.Split(','), minLevel, maxLevel);
			return lootTable;
		}
		*/

		//		/** Drops a single item from given area */
		//		private static MDRItem DropItem(MDRArea area)
		//		{
		//			return GetLootTable(area).SelectRandomItem();
		//		}
	}
}

