using System;
using System.Collections;
using Data;

using Mordor;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Data
{
	/** A library containing information about each guild */
	[DataObject("GuildLibrary")]
	public class MDRGuildLibrary : DataLibrary<MDRGuild>
	{
		override public int MaxRecords
		{ get { return 32; } }
	}

	[DataObject("Guild", true)]
	public class MDRGuild : NamedDataObject
	{
		public int MaxLevel;

		public int AvHitsPerLevel;
		public int HitsAtMaxLevel;
		public int AvSpellsPerLevel;
		public int SpellsAtMaxLevel;

		public float LevelMod;
		public float XPFactor;
		public float GoldFactor;
		public MDRStats RequiredStats;

		public int AttackBase;
		public int DefenseBase;
		public float AttackIncrease;
		public float DefenseIncrease;
		public float ADInterval;
		public int ADLevelCap;

		public int JoinCost;

		public float[] SkillRate;

		protected List<MDRRace> acceptedRaces;

		[FieldAttr(false)]
		protected string requiredGuildName;

		/** Level you must be in requierd guild before joining this guild. */
		public int RequiredGuildRequiredLevel = 30;

		/** Guild we must be a certian level of to join this guild. */
		public MDRGuild RequiredGuild
		{ get { return string.IsNullOrEmpty(requiredGuildName) ? null : CoM.Guilds[requiredGuildName]; } }

		public MDRGuild()
		{
			RequiredStats = new MDRStats();
			acceptedRaces = new List<MDRRace>();
		}

		/** 
		 * Returns if this guild will accept characters of given race or not 
		 */ 
		public bool CanAcceptRace(MDRRace race)
		{
			return acceptedRaces.Contains(race);
		}

		/** Returns true if this guild can accept the given player.  Checks stats, alignment, and level */
		public bool CanAccept(MDRCharacter character)
		{			
			if (!CanAcceptRace(character.Race))
				return false;
			if (!MeetsLevelRequiredments(character))
				return false;
			if (!(character.BaseStats >= RequiredStats))
				return false;
			return true;
		}

		/** Returns if given character meets the pre requisit guild requirements. */
		public bool MeetsLevelRequiredments(MDRCharacter character)
		{
			return (RequiredGuild == null) || character.Membership[RequiredGuild].CurrentLevel >= RequiredGuildRequiredLevel;
		}

		/** 
		 * Returns the experiance required for given character to obtain given level.
		 * This does not factor in the race coefficent, which will need to be applied afterwards
		 */
		public int XPForLevel(int level)
		{
			float GEF = XPFactor;
			int ExperienceNeeded = (int)((88.09020776 * (level - 1) * (level - 1) * (GEF / 8)) + (45.45454 * (((level - 1) * 2) - 1)) - 0.264);
			return ExperienceNeeded;
		}

		public override string LongDescription()
		{
			string magicSchools = "";
			for (var lp = 0; lp < CoM.SpellClasses.Count; lp++) {
				MDRSpellClass sc = CoM.SpellClasses[lp];
				MDRSkill skill = sc.Skill;
				if (skill != null)
				if (SkillRate[skill.ID] > 0)
					magicSchools += sc + ",";
			}
			if (magicSchools != "")
				magicSchools = magicSchools.TrimEnd(',');

			return Name + " may cast spells from [" + magicSchools + "]";
		}

		/** 
		 * Calculates the attack / defense this guild provides at given level 
		 */
		public void CalculateAttackDefense(int level, out int attack, out int defense)
		{
			level = (int)Util.Clamp(level, 1, ADLevelCap);
			
			float testLevel = 1;			

			int factor = 0;

			while (testLevel < level) {
				float interval = (float)(ADInterval * (1 + (testLevel / 400)));
				testLevel += interval;
				factor++;
			}

			attack = AttackBase + (int)(factor * AttackIncrease);
			defense = DefenseBase + (int)(factor * DefenseIncrease);
		}

		public override void WriteNode(XElement node)
		{
			Trace.LogWarning("Writing MDRGuilds is not fully supported.  Some custom fields will be missing.");
			base.WriteNode(node);
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			// load guild requirement
			requiredGuildName = ReadValue(node, "requiredGuildName");

			// Load the accepted races.
			string[] racesList = ReadValue(node, "AllowedRaces").Split(',');
			acceptedRaces.Clear();
			foreach (string raceName in racesList) {
				var race = CoM.Races.ByName(raceName.Trim());
				if (race != null)
					acceptedRaces.Add(race);
				else
					Trace.LogError("Data Error [Guilds]: No race {0} found in allowed races list.", raceName);
			}
		}
	}
}