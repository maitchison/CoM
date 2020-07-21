using System;

namespace Mordor
{
	public class MDRTreasureContainerType
	{
		public static MDRTreasureContainerType None = new MDRTreasureContainerType(0, "None", -99, 0, 0, 1.0f);
		public static MDRTreasureContainerType Box = new MDRTreasureContainerType(1, "Box", 0, 0, 0, 2.0f);
		public static MDRTreasureContainerType Chest = new MDRTreasureContainerType(2, "Chest", +25, +1, 0, 2.5f);
		public static MDRTreasureContainerType LockedChest = new MDRTreasureContainerType(3, "Locked Chest", +50, +1, 0, 3.0f);

		public int Ordional;
		public string Name;
		public int DropMod;
		public int MinLevelMod;
		public int MaxLevelMod;
		public float GoldMod;

		private MDRTreasureContainerType(int ordinal, string name, int dropMod, int minLevelMod, int maxLevelMod, float goldMod)
		{
			this.Ordional = ordinal;
			this.Name = name;
			this.DropMod = dropMod;
			this.MinLevelMod = minLevelMod;
			this.MaxLevelMod = maxLevelMod;
			this.GoldMod = goldMod;
		}

	}

}

