using System;
using Data;
using Mordor;
using System.Collections.Generic;
using System.Xml.Linq;

/** Handles monsters and their spawning */
[DataObject("SpawnManager", false)]
public class SpawnManager : DataObject
{
	/** The monsters currently active in our game. */
	public MDRMonsterInstanceLibrary Monsters;

	public Dictionary<MDRArea,SpawnInformation> SpawnInfo;

	public SpawnManager()
	{
		Trace.LogDebug("Creating spawn manager.");
		Monsters = new MDRMonsterInstanceLibrary();
		SpawnInfo = new Dictionary<MDRArea, SpawnInformation>();
	}

	/** Returns the monster instances associated with given area or null if there are none. */
	public List<MDRMonsterInstance> GetSpawnedMonsters(MDRArea area)
	{
		if (SpawnInfo.ContainsKey(area)) {
			var result = SpawnInfo[area].SpawnedMonsters;
			if (result != null && result.Count == 0)
				return null;
			return result;
		} else
			return null;
	}

	/** Returns the respawn time for given area. */
	public long GetRespawnTime(MDRArea area)
	{
		if (SpawnInfo.ContainsKey(area))
			return SpawnInfo[area].RespawnTimer;
		else
			return 0;		
	}

	/** Sets the monster instance to be associdated with the given area. */
	public void TrackSpawnedMonster(MDRArea area, MDRMonsterInstance value)
	{
		if (!SpawnInfo.ContainsKey(area)) {
			SpawnInfo[area] = new SpawnInformation();
		}
		SpawnInfo[area].SpawnedMonsters.Add(value);
	}

	/** Sets the spawn time for the given area. */
	public void SetRespawnTime(MDRArea area, long value)
	{
		if (!SpawnInfo.ContainsKey(area)) {
			SpawnInfo[area] = new SpawnInformation();
		}
		SpawnInfo[area].RespawnTimer = value;
	}

	public override void WriteNode(XElement node)
	{
		WriteValue(node, "Monsters", Monsters);	

		//save the spawn information.
		foreach (KeyValuePair<MDRArea,SpawnInformation> entry in SpawnInfo) {
			var subNode = new XElement("SpawnInformation");
			subNode.SetAttributeValue("Map", entry.Key.Map.FloorNumber);
			subNode.SetAttributeValue("Area", entry.Key.ID);
			entry.Value.WriteNode(subNode);
			node.Add(subNode);
		}			
	}

	public override void ReadNode(XElement node)
	{
		//stub:
		//Monsters.ReadNode(node.Element("Monsters"));
	}

}