using Mordor;

/** Privides some helpful routines used for debuging */
public class DebugHelper
{
	/** Makes entire dungeon visible to player */
	public static void MakeDungeonVisible()
	{
		Util.Assert(CoM.ExploredDungeon != null, "Can not make dungeon visible, explored dungeon is null.");
		Util.Assert(CoM.Dungeon != null, "Can not make dungeon visible, dungeon is null.");
		Util.Assert(CoM.Dungeon.Floor.Length == CoM.ExploredDungeon.Floor.Length, "Can not make dungeon visible, dungeon and explored dungeon must have the same number of floors.");

		foreach (MDRMap map in CoM.Dungeon.Floor) {
			if (map == null)
				continue;

			MDRMap destinationMap = new MDRMap();
			destinationMap.Initialize(map.Width, map.Height);
			destinationMap.FloorNumber = map.FloorNumber;

			for (int y = 1; y <= map.Height; y++) {
				for (int x = 1; x <= map.Width; x++) {
					FieldRecord source = map.GetField(x, y);
					FieldRecord destination = destinationMap.GetField(x, y);
					destination.CopyFrom(source, TileCopyMode.STANDARD);
				}
			}

			CoM.ExploredDungeon.Floor[map.FloorNumber] = destinationMap;
		}
	}
}
