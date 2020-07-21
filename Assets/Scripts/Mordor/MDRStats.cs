
using System.Xml.Linq;

using Data;
using Mordor;
using System;

namespace Mordor
{
	/** Records stats i.e. str, dex etc */
	public class MDRStats : DataObject
	{
		public static string[] SHORT_STAT_NAME = { "Str", "Int", "Wis", "Con", "Chr", "Dex" };
		public static string[] LONG_STAT_NAME = {
			"Strength",
			"Intelligence",
			"Wisdom",
			"Constitution",
			"Charisma",
			"Dexterity"
		};
		
		public int Str;
		public int Int;
		public int Wis;
		public int Con;
		public int Chr;
		public int Dex;

		public MDRStats() : this(0)
		{
		}

		public MDRStats(int defaultValue)
		{
			for (int lp = 0; lp < 6; lp++)
				this[lp] = defaultValue;
		}

		public static string RelativeStat(string name, int value)
		{
			if (value < 0)
				return value + " " + name; 
			if (value > 0)
				return "+" + value + " " + name;
			return "";
		}

		/** Overload to allow stat adding */
		public static MDRStats operator +(MDRStats s1, MDRStats s2)
		{
			MDRStats result = new MDRStats();
			for (int lp = 0; lp < 6; lp++)
				result[lp] = s1[lp] + s2[lp];
			return result;
		}

		public static MDRStats operator -(MDRStats s1, MDRStats s2)
		{
			MDRStats result = new MDRStats();
			for (int lp = 0; lp < 6; lp++)
				result[lp] = s1[lp] - s2[lp];
			return result;
		}

		/** Overload to allow comparison */
		public static bool operator >=(MDRStats s1, MDRStats s2)
		{
			bool result = true;
			for (int lp = 0; lp < 6; lp++)
				result = result && (s1[lp] >= s2[lp]);
			return result;
		}

		public static bool operator <=(MDRStats s1, MDRStats s2)
		{
			bool result = true;
			for (int lp = 0; lp < 6; lp++)
				result = result && (s1[lp] <= s2[lp]);
			return result;
		}

		public static bool operator >(MDRStats s1, MDRStats s2)
		{
			bool result = true;
			for (int lp = 0; lp > 6; lp++)
				result = result && (s1[lp] > s2[lp]);
			return result;
		}

		public static bool operator <(MDRStats s1, MDRStats s2)
		{
			bool result = true;
			for (int lp = 0; lp < 6; lp++)
				result = result && (s1[lp] < s2[lp]);
			return result;
		}

		public static bool operator ==(MDRStats s1, MDRStats s2)
		{
			bool result = true;
			for (int lp = 0; lp < 6; lp++)
				result = result && (s1[lp] == s2[lp]);
			return result;
		}

		public static bool operator !=(MDRStats s1, MDRStats s2)
		{
			bool result = false;
			for (int lp = 0; lp < 6; lp++)
				result = result || (s1[lp] != s2[lp]);
			return result;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/** Indexer for stats, order is 0=Str, Int, Wis, Con, Chr, Dex */ 
		public int this [int index] {
			get {
				switch (index) {
				case 0:
					return Str; 
				case 1:
					return Int; 
				case 2:
					return Wis; 
				case 3:
					return Con; 
				case 4:
					return Chr; 
				case 5:
					return Dex; 
				default:
					return 0;
				}
			}
			set {
				switch (index) {
				case 0:
					Str = value;
					break;
				case 1:
					Int = value;
					break;
				case 2:
					Wis = value;
					break;
				case 3:
					Con = value;
					break;
				case 4:
					Chr = value;
					break;
				case 5:
					Dex = value;
					break;
				}
			}
		}

		/** 
		 * Returns stats formatted as a string
		 * @param seperator string to use to seperate stats
		 * @param relative if true stats will be listed as +8 Str rather than Str 8
		 */
		public string FormatToString(string seperator = ",", bool relative = false)
		{
			string result;
			
			if (relative) {
				result = 
					RelativeStat("Str" + seperator, Str) +
				RelativeStat("Int" + seperator, Int) +
				RelativeStat("Wis" + seperator, Wis) +
				RelativeStat("Con" + seperator, Con) +
				RelativeStat("Chr" + seperator, Chr) +
				RelativeStat("Dex" + seperator, Dex);
				//trim off the last seperator
				if (result != "")
					result.Remove(result.Length - seperator.Length);            
			} else {
				result = 
					"Str " + Str + seperator +
				"Int " + Int + seperator +
				"Wis " + Wis + seperator +
				"Con " + Con + seperator +
				"Chr " + Chr + seperator +
				"Dex " + Dex + seperator;
			}
			
			return result;
		}

		public override string ToString()
		{
			return FormatToString();
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			WriteAttribute(node, "str", Str);
			WriteAttribute(node, "dex", Dex);
			WriteAttribute(node, "int", Int);
			WriteAttribute(node, "wis", Wis);
			WriteAttribute(node, "chr", Chr);
			WriteAttribute(node, "con", Con);
		}

		public override void ReadNode(XElement node)
		{
			// handle CSV style stats nodes.
			if (node.Value != "") {
				var data = ReadArray<int>(node);	
				if (data.Length != 6) {
					Trace.LogWarning("Data Error [Invalid formatting]: stats node '{0}', wrong number of items.", node.Value);
					for (int lp = 0; lp < 6; lp++)
						this[lp] = 0;
					return;
				}
				for (int lp = 0; lp < 6; lp++)
					this[lp] = data[lp];
			} else {
				// assume the stats are in the attributes.
				Str = ReadAttributeInt(node, "str");
				Dex = ReadAttributeInt(node, "dex");
				Int = ReadAttributeInt(node, "int");
				Wis = ReadAttributeInt(node, "wis");
				Chr = ReadAttributeInt(node, "chr");
				Con = ReadAttributeInt(node, "con");
			}
		}

		#endregion
	}

}