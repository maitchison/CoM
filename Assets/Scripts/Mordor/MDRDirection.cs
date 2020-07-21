using System;
using UnityEngine;

namespace Mordor
{

	/** Facing direction */
	public struct Direction
	{
		public static Direction NORTH = new Direction(0);
		public static Direction EAST = new Direction(90);
		public static Direction SOUTH = new Direction(180);
		public static Direction WEST = new Direction(270);

		public float Angle;

		public Direction(float angle)
		{
			angle = angle / 360f;
			if (angle < 0)
				angle += (int)(1 - angle);
			angle = angle - Mathf.Floor(angle);
			angle = angle * 360f;

			Angle = angle;
		}

		/** 
		 * Gets direction from direction name, i.e. "north".
		 * @defaultDirection The direction to return name doesn't match. 
		 */
		public static Direction FromString(string name, String defaultDirection = "north")
		{
			if (string.Compare(name, "north", true) == 0)
				return Direction.NORTH;
			if (string.Compare(name, "south", true) == 0)
				return Direction.SOUTH;
			if (string.Compare(name, "east", true) == 0)
				return Direction.EAST;
			if (string.Compare(name, "west", true) == 0)
				return Direction.WEST;
			if (defaultDirection == "")
				throw new Exception("invalid default direction '" + defaultDirection + "'");
			return Direction.FromString(defaultDirection, "");
		}

		public Direction(int angle)
			: this((float)angle)
		{
		}

		public static implicit operator Direction(float f)
		{
			return new Direction(f);
		}

		public static implicit operator float(Direction direction)
		{
			return direction.Angle;
		}

		/** Sets angle to a value between 0 to 360 */
		public void Normalise()
		{
			Angle = ClippedAngle;
		}

		public int DX
		{ get { return Mathf.RoundToInt(DeltaX); } }

		public int DY
		{ get { return Mathf.RoundToInt(DeltaY); } }


		/** Create direction from vector. */
		public Direction FromVector(Vector2 vector)
		{
			float angle;
			if (vector.x < 0) {
				angle = 360 - (Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg * -1);
			} else {
				angle = Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
			}
			return new Direction(angle);
		}


		public float DeltaX
		{ get { return (float)Mathf.Sin(Mathf.Deg2Rad * Angle); } }

		public float DeltaY
		{ get { return (float)Math.Cos(Mathf.Deg2Rad * Angle); } }

		/** Returns the sector this direction is in, 0 being north, 1 being east, 2 being south, 3 being west */
		public int Sector
		{ get { return Mathf.RoundToInt((ClippedAngle) / 90f); } }

		public bool isNorth
		{ get { return Sector == 0; } }

		public bool isEast
		{ get { return Sector == 1; } }

		public bool isSouth
		{ get { return Sector == 2; } }

		public bool isWest
		{ get { return Sector == 3; } }

		public float ClippedAngle {
			get {
				if (Angle < 0)
					return (Angle + 360 * Mathf.Ceil(-Angle / 360)) % 360;
				else
					return Angle % 360;
			}
		}

		public override string ToString()
		{
			return Angle.ToString("0.0");
		}

	}
}
        
