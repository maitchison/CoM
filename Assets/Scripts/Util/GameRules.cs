
using System;
using Mordor;
using Data;
using UnityEngine;

/** Static class for customisable game rules */
public class GameRules
{
	/** 
	 * Returns the number of days this character needs to wait until rescue.
	 * The character's location should be the location that they died.
	 * 
	 * @param character the character who has died
	 * @returns number of days before given character is found
	 */
	public static int WaitForRescueTime(MDRCharacter character)
	{
		// ----------------------------------------------------------------------
		// see: http://dejenol.com/index.php?title=Waiting_for_Rescue
		// ----------------------------------------------------------------------

		if (character == null)
			throw new ArgumentNullException("character", "[character] must not be null.");

		int depth = character.Depth;

		float baseChancePerDay = (float)Util.Clamp(17 - depth, 1, 16);
		int chanceSwitch = (int)(100 * (float)Math.Log(depth) * Math.Log(depth) + 22);

		int dayCounter = 1;
		float chanceDivisor = 100;

		while (dayCounter < 9999) {
			if (depth >= 1)
				chanceDivisor = (dayCounter <= chanceSwitch) ? 10000 : 1000;
			float chance = baseChancePerDay / chanceDivisor;
			if (UnityEngine.Random.value < chance)
				break;
			dayCounter++;
		}

		return dayCounter;
	}

	/** Returns the cost of raising given character. */
	public static int CostToRaise(MDRCharacter character)
	{
		if (character == null)
			return 0;
		if (character.Membership.MaxLevel < 10)
			return 0;
		return (int)(character.Membership.TotalXP / 10f);
	}

	/** Returns the cost of hiring a rescue party to go to given location. */
	public static int CostToHireRescue(int floorNumber)
	{
		if (floorNumber <= 1)
			return 0;

		return (int)Mathf.Pow(3, floorNumber) * 100;
	}

	/**
	 * Returns the number of days required to heal x number of hits for given character 
	 * 
	 * @param hitsToHeal number if hits that should be healed
	 * @param character the character that needs to be healed
	 * @returns healing time in days
	 */
	public static int HealingTime(int hitsToHeal, MDRCharacter character)
	{
		// ----------------------------------------------------------------------
		// no information for this, just assuming 4 weeks for full heal
		// ----------------------------------------------------------------------

		const int DAYS_TO_FULL_HEAL = 4 * 7;

		if (character == null)
			throw new ArgumentNullException("character", "[character] must not be null.");
		if (hitsToHeal < 0)
			throw new ArgumentOutOfRangeException("hitsToHeal", "[hitsToHeal] must be non negative.");

		float fraction = (float)hitsToHeal / character.MaxHits;
		return (int)(fraction * DAYS_TO_FULL_HEAL);
	}

	/** 
	 * Returns the critical chance of a given item.
	 * Returns percentage chance [0..100]
	 */
	public static float WeaponCriticalHitChance(MDRItem weapon)
	{
		if (!weapon.Abilities.CriticalHit)
			return 0;
		return (float)Math.Log(weapon.AppearsOnLevel + 1) * (float)Math.Log(10);
	}

	/** 
	 * Applies a logarithim weighting to a delta where delta 0 will return 0
	 * High deltas will return higher values, negetive deltas lower values. 
	*/
	private static float CalculateLogDifference(float delta, float weighting = 0.1f)
	{		
		return  (delta >= 0) ?
			Mathf.Log(1f + delta, 2f) * weighting :
			Mathf.Log(1f - delta, 2f) * -weighting;
	}

	/**
	 * Calculates chance one actor can hit another.
	 */
	public static float CalculateChanceToHit(MDRActor source, MDRActor target)
	{		
		// We calculate a base hit from guild level then offset it by dexterity.
		float dexterityDelta = source.Stats.Dex - target.Stats.Dex;

		float dexMod = 
			dexterityDelta >= 0 ?
			(Mathf.Sqrt(1 + dexterityDelta / 4f) - 1f) * 0.5f :
			(Mathf.Sqrt(1 - dexterityDelta / 4f) - 1f) * -0.5f;

		float origionalDexMod = dexMod;

		float levelDelta = (source.CombatLevel - target.CombatLevel) / 5f;
		float levelModifier = CalculateLogDifference(levelDelta);

		// adjust deviation
		if (levelModifier < 0 && dexMod > 0) { 			
			float dexModReduction = Util.Clamp(-levelModifier * 4f, 0f, 1f);
			dexMod *= dexModReduction;
		}

		// bonus from defending.
		float baseHit = (target.ActionThisRound == ActionType.Defend) ? 0.4f : 0.6f;

		float chanceToHit = baseHit + levelModifier + dexMod + (float)source.TotalHitBonus / 100f;

		float clampedChanceToHit = Util.Clamp(chanceToHit, 0.03f, 0.97f);

		/*
		if (Settings.Advanced.LogCombat && Engines.CombatEngine.CurrentInstance != null) {			
			
			var debugString = String.Format("\nChance to hit breakdown [{0:0.00}]:" +
			                  "\n baseHit: {1:0.00}" +
			                  "\n levelMod: {2:0.00}" +
			                  ((dexMod != origionalDexMod) ? "\n dexMod: {3:0.00} (reduced from {4:0.00})" : "\n dexMod: {3:0.00}") +
			                  "\n hitBonus: {5:0.00}", chanceToHit, baseHit, levelModifier, dexMod, origionalDexMod, (float)source.TotalHitBonus / 100f);			                  
			Engines.CombatEngine.CombatLog.Add(new UI.MessageEntry(debugString, Color.gray));
		}  */

		return clampedChanceToHit;
	}

	/** 
	 * Calculates the XP a character should receive for dealing an amount of damage to a monster 
	 */
	public static int CalculateXP(MDRCharacter character, MDRMonster monster, int damage)
	{
		const float CompanionFactor = 1.0f;
		float GuildLevelFactor = 1 / (1 + (character.Membership.Current.CurrentLevel / 333));

		//float PartySizeFactor = (1 - ((Party.Count - 1) * 0.04f));
		float PartySizeFactor = 1f;

		int XPAwarded = (int)(damage * (9 - Math.Log(damage + 1)) * (20 - Math.Log(damage)) * (monster.GuildLevel + 50) * CompanionFactor * GuildLevelFactor / 1000);
		int FinalXPAwarded = (int)(PartySizeFactor * XPAwarded);

		return FinalXPAwarded;
	}


	/**
	 * Calculates damage from one actor to another.
	 */
	public static int CalculateDamage(MDRActor source, MDRActor target, bool didCrit, bool didBackstab, out int mitigation)
	{		
		// First work out how much damage this source could inflict.

		int damageRoll = Util.Roll(6) + Util.Roll(6) + Util.Roll(6);

		float critFactor = didCrit ? 1.5f : 1f;
		float backstabFactor = didBackstab ? 1.25f : 1f;

		float baseDamage = source.NominalDamage * ((float)damageRoll / 9f) * critFactor * backstabFactor;

		float strengthBonus = Util.Clamp(source.Stats.Str - 15, 0f, 99f);
		float strengthHandicap = Util.Clamp(source.Stats.Str - 10, -99f, 0f) * 0.5f;

		float levelDelta = (source.CombatLevel - target.CombatLevel) / 5f;
		float levelModifier = CalculateLogDifference(levelDelta);

		float finalDamage = (int)((1f + levelModifier) * (baseDamage + strengthBonus) + strengthHandicap);

		// Next work out how much of this damage the target's armour can exporbe

		float baseMitigation = target.TotalArmour;

		float modifiedMitigation = baseMitigation;

		// Armour ability is reduced by level modifier.
		if (levelModifier < 0)
			modifiedMitigation *= (1 + levelModifier);

		// bonus from defending.
		if (target.ActionThisRound == ActionType.Defend)
			modifiedMitigation *= 1.5f;
		
		if (didBackstab)
			modifiedMitigation *= 0.25f;

		if (didCrit)
			modifiedMitigation *= 0.75f;	

		// Pentration due to weapon and strength.
		var weaponPiercing = source.TotalArmourPierce + strengthBonus * 2f;
		modifiedMitigation -= Util.Clamp(weaponPiercing, 0f, 999f);

		// Strong hits go through weak armour
		float strongHitModifer = finalDamage / 2f;

		modifiedMitigation -= strongHitModifer;

		modifiedMitigation = Util.Clamp(modifiedMitigation, 0f, 999f) / 2f;

		float mitigationReductionFactor = (finalDamage) / (finalDamage + modifiedMitigation);

		float mitigatedDamage = (int)(finalDamage * (1f - mitigationReductionFactor));

		mitigation = (int)mitigatedDamage;

		/*
		if (Settings.Advanced.LogCombat && Engines.CombatEngine.CurrentInstance != null) {			
			var debugString = String.Format("Damage caused breakdown [{0}]: \n" +
			                  "SourceSTR:{1}={2} NominalDamage:{3} DamageRoll{4} Final:{10}\n" +
			                  "levelDelta:{5} level modifier:{6}\n" +
			                  "mitigation - base:{7} modified:{8} factor:{9}",
				                  mitigatedDamage, source.Stats.Str, strengthBonus + strengthHandicap, 
				                  source.NominalDamage, damageRoll, levelDelta, levelModifier, baseMitigation, modifiedMitigation, mitigationReductionFactor, finalDamage);			             
			Engines.CombatEngine.CombatLog.Add(new UI.MessageEntry(debugString, Color.gray));
		} */

		return (int)Util.Clamp(finalDamage - mitigatedDamage, 1f, 999f);
	}

	/** 
	 * Returns the backstab chance of a given item.
	 * Returns percentage chance [0..100]
	 */
	public static float WeaponBackstabChance(MDRItem weapon)
	{
		if (!weapon.Abilities.Backstab)
			return 0;
		return (float)Math.Log(weapon.AppearsOnLevel + 1) * (float)Math.Log(100);
	}


	/**
	 * Returns the number of additional swings character gains from multiple swings skill.
	 */
	public static float AdditonalSwings(MDRCharacter character)
	{
		return character.MultipleSwingsSkill - 1f;
	}


	/** Returns the characters highest skill level from any guild.  fromGuild will be set to the guild with the highest fighting value */
	public static float CharacterGeneralAbility(MDRCharacter character, MDRSkill skill)
	{
		MDRGuild guild;
		return CharacterGeneralAbility(character, skill, out guild);
	}

	/**
	 * Returns the characters highest skill level from any guild.  
	 * FromGuild will be set to the guild with the highest fighting value. 
	 */
	public static float CharacterGeneralAbility(MDRCharacter character, MDRSkill skill, out MDRGuild fromGuild)
	{
		fromGuild = null;
		float bestSkill = 0;
		foreach (MDRGuild guild in CoM.Guilds) {
			float skillLevel = CharacterGeneralAbilityFromGuild(character, guild, guild.SkillRate[skill.ID], skill.LearningDifficulty);
			if (skillLevel > bestSkill) {
				fromGuild = guild;
				bestSkill = skillLevel;
			}
		}
		return bestSkill;
	}

	/** Used for all skills */
	private static float CharacterGeneralAbilityFromGuild(MDRCharacter character, MDRGuild guild, float guildSkillRate, float skillDifficulty)
	{
		if (!character.Membership[guild].IsMember)
			return 0;

		if (guildSkillRate == 0)
			return 0;

		float guildLevel = character.Membership[guild].CurrentLevel;

		return Mathf.Floor(Mathf.Sqrt((guildLevel - 1) * guildSkillRate) / skillDifficulty * 10f + 10f);
	}

	/** Returns the critical hit chance reduction for a given monster */ 
	public static float MonsterCritReduction(MDRMonster monster)
	{
		return (float)Math.Log(1 + (monster.GuildLevel / 999)) * 50f;
	}

	/** Returns the backstab hit chance reduction for a given monster */ 
	public static float MonsterBackstabReduction(MDRMonster monster)
	{
		return (float)Math.Log(1 + (monster.GuildLevel / 999)) * 50f;
	}

	/**
	 * Calculates the change this source could critical hit the target.
	 */
	public static float CriticalHitChance(MDRActor source, MDRActor target)
	{
		float baseChance = (float)source.TotalCriticalHitChance / 100f;

		float levelDelta = (source.CombatLevel - target.CombatLevel) / 5f;
		float levelModifier = CalculateLogDifference(levelDelta);

		float final = baseChance * (1f + levelModifier);

		return final;
	}

	/**
	 * Calculates the change this source could backstab hit the target.
	 */
	public static float BackstabChance(MDRActor source, MDRActor target)
	{
		float baseChance = (float)source.TotalBackstabChance / 100f;

		float levelDelta = (source.CombatLevel - target.CombatLevel) / 5f;
		float levelModifier = CalculateLogDifference(levelDelta);

		return baseChance * (1f + levelModifier);
	}

	/** Calculates who attacks first, player or monsters.  Return true for player */
	public static bool CalculateInititive(MDRMonster monster, MDRCharacter character)
	{
		float initiative = 50 + ((monster.Stats.Dex - character.Stats.Dex) * 5f);
		initiative = Util.Clamp(initiative, 5f, 98f);
		return Util.Roll(100) > initiative;
	}

	/** 
	 * Returns if given character fell down a pit on a given level or not.
	`*/
	public static bool FallRoll(MDRCharacter character)
	{
		int depth = character.Depth;
		int baseChance = character.Levitating ? 50 : 150;
		float chance = baseChance + (depth * 4) - (character.Stats.Dex * 5);
		return (Util.Roll(100) <= chance);
	}

	/** Returns damage a given character should receive if they full down a pit. */
	public static int CalculatePitDamage(MDRCharacter character)
	{
		int depth = character.Depth;
		float factor = character.Levitating ? 2 : 10;
		float baseDamage = (float)UnityEngine.Random.value * factor * (float)Math.Sqrt(depth);
		float value = (float)Math.Sqrt(character.MaxHits) * 2f;
		return (int)(baseDamage + value);
	}

	/** Returns the ability given character has at identifying items or monsters. */
	public static float CharacterIndentifyingAbility(MDRCharacter character)
	{
		float wisPart = character.Stats.Wis / 40;
		float intPart = character.Stats.Int / 20;

		float guildPart = (float)Math.Sqrt(character.HighestLevel) * (character.HighestLevel / 1800f);
		return (wisPart + intPart + guildPart) * 3;
	}

	/** Returns how difficult an item is to identify */
	public static float ItemIdentifyDifficulty(MDRItem item)
	{
		return item.AppearsOnLevel * 3f * item.BaseIDDifficulty;
	}

	/** Returns how difficult a monster is to identify */
	public static float MonsterIdentifyDifficulty(MDRMonster monster)
	{
		return monster.AppearsOnLevel * 3f + (float)Math.Max(0f, (Math.Log10(monster.HitPoints) / 2f) - 1f);
	}

	// ----------------------------------------------------------------------
	// Commands
	// ----------------------------------------------------------------------

	/** 
	 * Raises character from the dead. Complications might occur. 
	 * @returns Description of complications if any.
	 */
	public static string RaiseCharacterFromDead(MDRCharacter character)
	{		
		float complicationsChance = 4;

		string complications = "";

		bool whereComplications = (Util.Roll(100) <= complicationsChance);
						
		if (whereComplications) {
			int statToDecrease = Util.Roll(6) - 1;
			if (character.Stats[statToDecrease] > 3) {
				character.ModifyStats(statToDecrease, -1);
				complications = "lost a point of " + MDRStats.LONG_STAT_NAME[statToDecrease];
			} else {
				if (character.MaxHits > 5) {
					character.MaxHits = Util.ClampInt(character.MaxHits, 5, 99);
					complications = "lost hp";
				} else {
					complications = "is as messed up as " + character.Pronoun + "'s going to get.";
					character.MaxHits = 5;
				}
			}
		} else {
			character.FullRestore();
		}			

		return complications;
	}

	// ----------------------------------------------------------------------
	// Roles
	// ----------------------------------------------------------------------

	/**
	 * Returns if character passes a perception test of given level or not 
	 */
	public static bool PerceptionRoll(MDRCharacter character, int difficulty)
	{
		
		float delta = character.PerceptionSkill - difficulty;
		float chanceToDetect = 5f;
		if (delta < 0)
			chanceToDetect += delta * 0.25f;
		if (delta > 0)
			chanceToDetect += delta * 0.5f;
		
		chanceToDetect = Util.Clamp(chanceToDetect, 0f, 100f);
		float roll = UnityEngine.Random.value * 100;
		//Trace.Log("Perception roll is {0}% [rolled {3}] (skill:{1} difficulty:{2})", chanceToDetect.ToString("0.0"), character.PerceptionSkill, difficulty, roll.ToString("0.0"));
		return (roll <= chanceToDetect);
	}
}
