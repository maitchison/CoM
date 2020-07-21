using System;
using System.Collections;

namespace Mordor
{
	/** Defines which types of monsters can spawn in this area */
	public class MDRSpawnMask
	{
		public BitArray Mask;

		public bool this [int index] {
			get {
				if ((index < 0) || (index >= 32))
					return false;
				else
					return Mask[index];
			}
		}

		/** Creates a spawn mask from give data.  Where each bit represents a type of monster that can spawn there */
		public MDRSpawnMask(uint data)
		{
			Mask = new BitArray(BitConverter.GetBytes(data));
		}

		public override string ToString()
		{
			string result = "";
			for (int lp = 1; lp < 17; lp++) {
				if (Mask[lp])
					result += CoM.MonsterTypes[lp].Name + " ";
			}
			return result;
		}
	}

}

