using System;
using Data;
using System.Collections.Generic;
using Mordor;
using System.Xml.Linq;

/** Information about monsters spawning by area. */
public class SpawnInformation : DataObject
{
	/** Time in ticks that this area is scheduled for respawn, 0 for no respawn. */
	public long RespawnTimer;
	/** List of monsters spawned by this area. */
	public List<MDRMonsterInstance> SpawnedMonsters;

	public override void WriteNode(XElement node)
	{
		WriteValue(node, "RespawnTimer", RespawnTimer);

		int[] SpawnedMonstersIDArray = new int[SpawnedMonsters.Count];

		for (int lp = 0; lp < SpawnedMonsters.Count; lp++) {
			SpawnedMonstersIDArray[lp] = SpawnedMonsters[lp].ID;
		}

		WriteValue(node, "SpawnedMonsters", SpawnedMonstersIDArray);
	}

	public SpawnInformation()
	{
		SpawnedMonsters = new List<MDRMonsterInstance>();
	}
}

