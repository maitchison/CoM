using Data;

using Mordor;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

namespace Mordor
{
	/** A library containing information about each race */
	[DataObject("RaceLibrary")]
	public class MDRRaceLibrary : DataLibrary<MDRRace>
	{
	}

	[DataObject("Race", true)]
	public class MDRRace : NamedDataObject
	{
		public MDRStats MinStats;
		public MDRStats MaxStats;
		public MDRStats DefaultStats;
		public float Size;
		public int BonusPoints;
		public int StartingHP;
		public int StartingSP;
		public float ExperianceFactor;

		public MDRResistance Resistance;

		public MDRRace()
		{
			MinStats = new MDRStats();
			MaxStats = new MDRStats();
			DefaultStats = new MDRStats();
			Resistance = new MDRResistance();
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);
			for (int lp = 0; lp < MDRStats.LONG_STAT_NAME.Length; lp++) {
				MaxStats[lp] = DefaultStats[lp] + 10;
				MinStats[lp] = DefaultStats[lp];
			}
		}
	}


	[DataObject("Resistance")]
	public class MDRResistance : DataObject
	{
		public static List<string> ResistanceNames = new List<string>(new string[] {
			"Fire", "Cold", "Electrial", "Mind", "Disease", "Poison", "Magic",
			"Stone", "Paralysis", "Drain", "Acid"
		});

		// This forces values to be written directly to the node
		public int[] Values;

		public MDRResistance()
		{
			Values = new int[Count];
		}

		/** Indexer to data by index */
		public int this [int index] { 
			get { 
				if ((index < 0) || (index > Count))
					throw new Exception("invalid resistance index " + index);
				return (Values[index]); 
			}
			set { 
				if ((index < 0) || (index > Count))
					throw new Exception("invalid resistance index " + index);
				Values[index] = value;
			}
		}

		private int nameToIndex(string name)
		{
			int index = ResistanceNames.FindIndex((string obj) => string.Compare(obj, name, true) == 0);
			if (index < 0)
				throw new Exception("Invalid resistance name " + name);
			return index;
		}

		/** Indexer to data by string */
		public int this [string name] {
			get { return this[nameToIndex(name)]; }
			set { this[nameToIndex(name)] = value; }
		}

		public static int Count {
			get { return ResistanceNames.Count; }
		}

		/** Override to write values directly to node */
		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);
			WriteDirect(node, Values);
		}

		/** Override to read values directly to node */
		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);
			Values = ReadArray<int>(node);
		}

		public override string ToString()
		{
			string result = "";
			for (int lp = 0; lp < Count; lp++) {
				if (this[lp] != 0)
					result += string.Format("{0} {1}", ResistanceNames[lp], this[lp]) + " ";
			}
			result = result.TrimEnd(' ');
			return result;
		}

		/** Combines the other resistance table with this.  The highest value will be chosen */
		public void Combine(MDRResistance other)
		{
			for (int lp = 0; lp < Count; lp++) {
				this[lp] = Math.Max(other[lp], this[lp]);
			}
		}
	}

}