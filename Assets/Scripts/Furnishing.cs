using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DetailPlacementMethod
{
	/** Objects are placed along wall. */
	Wall,
	/** Objects are placed in the corner of two walls. */
	Corner,
	/** Objects are place on the floor of a tile. */
	Floor
}

/** A list of of dungeon detail descriptions. */
[System.Serializable]
public class FurnishingList
{
	public List<Furnishing> List;

	public int Count {
		get { return List == null ? 0 : List.Count; }
	}

	/** Indexer to properties by name */
	public Furnishing this [int index] {
		get { return List[index]; }
	}
}

/** Describes the placement of dungeon furnishings. */
[System.Serializable]
public class Furnishing
{
	public bool Enabled = true;

	/** The detail to be placed. */
	public GameObject Source;

	/** How the object needs to be placed. */
	public DetailPlacementMethod Placement;

	/** If true position will be randomized in a suitable way for the placement method. */
	public bool PositionVariation = false;

	/** If true rotation will be randomized in a suitable way for the placement method. */
	public bool RotationVariation = false;

	/** Only one object from each group will be placed. */
	public int GroupID;

	/** The percentage chance this type of detail will appear. */
	[Range(0, 100)]
	public float Chance;
}
