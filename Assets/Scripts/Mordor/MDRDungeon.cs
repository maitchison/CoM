using System.Linq;
using System.Xml.Linq;

using Data;

namespace Mordor
{
	/// <summary>
	/// Stores dungeon information for Mordor remake
	/// </summary>
	[DataObjectAttribute("Dungeon", 2.0f)]
	public class MDRDungeon : DataObject
	{
		/** Maximum number of floors in dungeon */
		public const int MAX_FLOORS = 99;

		public static int DEFAULT_WIDTH = 32;
		public static int DEFAULT_HEIGHT = 32;
		public static int DEFAULT_FLOORS = 15;

		/** List of floors.  Floor 0 is always a Width by Height empty map */
		public MDRMap[] Floor;

		// stub: hard coded width and height

		/** Width of the dungeon */ 
		public int Width = DEFAULT_WIDTH;
		/** Height of the dungeon */
		public int Height = DEFAULT_HEIGHT;

		/** Number of floors in dungeon.  Will always be one less than Floor.count as floor[0] is ignored */
		public int Floors {
			get { return (Floor == null) ? 3 : Floor.Length - 1; }
		}

		/** Creates an empty dungeon of the given dimentions */
		public void Initialize(int width, int height, int floors)
		{
			Floor = new MDRMap[floors + 1];
			Floor[0] = new MDRMap();
			Floor[0].Initialize(Width, Height);
			Floor[0].FloorNumber = 0;
			for (int lp = 1; lp <= floors; lp++) {
				Floor[lp] = new MDRMap();
				Floor[lp].Initialize(width, height);
				Floor[lp].FloorNumber = lp;
			}
		}

		/** Creates a new empty dungeon of given dimenetions */
		public static MDRDungeon Create(int width, int height, int floors)
		{
			MDRDungeon result = new MDRDungeon();
			result.Initialize(width, height, floors);
			return result;
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			foreach (MDRMap map in Floor) {
				if ((map != null) && (map.FloorNumber != 0))
					WriteValue(node, "Floor", map);
			}
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			Floor = new MDRMap[node.Elements("Floor").Count() + 1];

			Floor[0] = new MDRMap();
			Floor[0].Initialize(Width, Height);

			foreach (XElement subNode in node.Elements("Floor")) {
				MDRMap map = new MDRMap();
				map.ReadNode(subNode);
				if (map.FloorNumber > MAX_FLOORS)
					Trace.LogError("Too many floors in dungeon (" + map.FloorNumber + "), a maximum of " + MAX_FLOORS + " is allowed.");
				Floor[map.FloorNumber] = map;			
			}
		}

		#endregion
	}
}
