using System;
using Data;
using System.Collections.Generic;

namespace Mordor
{
	public class SpawnTable : WeightedTable<MDRMonster>
	{
		/** Creates a spawn list for given area */
		public void BuildList(MDRArea area, int minLevel, int maxLevel)
		{
			PotentialItems.Clear();

			List<MDRMonsterClass> potentialClasses = new List<MDRMonsterClass>();

			// Find list of all potential monster classes.
			for (int lp = 0; lp < CoM.MonsterClasses.Count; lp++)
				if (area.SpawnMask[lp])
					potentialClasses.Add(CoM.MonsterClasses[lp]);

			for (int lp = 0; lp < CoM.Monsters.Count; lp++) {
				MDRMonster monster = CoM.Monsters[lp];
				if (
					(monster.AppearsOnLevel >= minLevel) &&
					(monster.AppearsOnLevel <= maxLevel) &&
					(potentialClasses.Contains(monster.Type.TypeClass))) {
					addWeight(monster, monster.EncounterChance);
				}
			}

			// Add laired monster at 80% chance.
			if (area.LairedMonster != null)
				addWeight(area.LairedMonster, calculateTotalWeighting() * (1f / 0.20f));

		}
	}

}

