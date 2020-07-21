using System;
using Data;
using System.Collections.Generic;
using UnityEngine;

namespace Mordor
{
	public class SkillLevelRank
	{
		public static Color GetSkillColor(float level)
		{
			level = Mathf.Floor(level);
			if (level < 10)
				return new Color(0.4f, 0.4f, 0.4f);
			if (level < 20)
				return new Color(0.8f, 0.8f, 0.8f);
			if (level < 30)
				return new Color(1f, 1f, 1f);
			if (level < 40)
				return new Color(0.8f, 1f, 0.8f);
			if (level < 50)
				return new Color(0.7f, 0.8f, 1.0f);
			if (level < 60)
				return new Color(1.0f, 0.8f, 1.0f);
			if (level < 70)
				return new Color(1.0f, 1.0f, 0.5f);
			return Color.gray;
		}

		public static string GetSkillRank(float level)
		{
			level = Mathf.Floor(level);
			if (level < 10)
				return "No ability";
			if (level < 20)
				return "Novice";
			if (level < 30)
				return "Apprentice";
			if (level < 40)
				return "Journeyman";
			if (level < 50)
				return "Expert";
			if (level < 60)
				return "Master";
			if (level < 70)
				return "Artisan";
			if (level < 80)
				return "Legendary";
			if (level < 90)
				return "Legendary II";
			if (level < 100)
				return "Legendary III";
			if (level < 110)
				return "Legendary IV";
			return "unknown";
		}
	}

	[DataObject("SkillLibrary")]
	public class MDRSkillLibrary : DataLibrary<MDRSkill>
	{
		override public int MaxRecords
		{ get { return 32; } }
	}

	/** Defines a character skill, such as fire magic, or ability with axes */
	[DataObjectAttribute("Skill", true)]
	public class MDRSkill : NamedDataObject
	{
		public string Category;
		public string Description;
		/** How fast skill points are gained in this skill. */
		public float LearningDifficulty;
		/** Names of the various ranks in this skill. */
		public string Rankings;

		public MDRSkill()
		{
		}
	}
}

