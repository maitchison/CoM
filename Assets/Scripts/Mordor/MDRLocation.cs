using Mordor;
using Data;
using System.Xml.Linq;

namespace Mordor
{
	/** Represents a location in the dungeon at x,y,floor */
	public struct MDRLocation : iSerializable
	{
		public int X;
		public int Y;
		public int Floor;

		public MDRLocation(int x, int y, int floor)
		{
			this.X = x;
			this.Y = y;
			this.Floor = floor;
		}

		override public string ToString()
		{
			return "(" + X + "," + Y + "," + Floor + ")";
		}

		#region iSerializable implementation

		public void WriteNode(XElement node)
		{
			node.SetAttributeValue("X", X);
			node.SetAttributeValue("Y", Y);
			node.SetAttributeValue("Floor", Floor);
		}

		public void ReadNode(System.Xml.Linq.XElement node)
		{
			X = int.Parse(node.Attribute("X").Value);
			Y = int.Parse(node.Attribute("Y").Value);
			Floor = int.Parse(node.Attribute("Floor").Value);
		}

		#endregion
	}
}

