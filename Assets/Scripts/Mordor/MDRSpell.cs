
using System.Xml.Linq;

using UnityEngine;

using Data;
using System;
using Engines;
using System.Collections.Generic;

namespace Mordor
{
	/** List of spell classes */
	[DataObject("SpellClassLibrary")]
	public class MDRSpellClassLibrary : DataLibrary<MDRSpellClass>
	{
	}

	public enum SpellTarget
	{
		/** Spell has no target */
		None,
		/** Spell targets single monster stack. */
		Single,
		/** Spell targets all monster stacks. */
		Area,
		/** Spell target origional caster. */
		Caster,
		/** Spell targets a party member. */
		Character,
		/** Spell targets all party members. */
		Party
	}

	/** Struct for passing around damage amounts. */
	public struct DamageInfo
	{
		public int Amount;
		public MDRDamageType DamageType;
		public MDRActor Source;

		public DamageInfo(int damage, MDRActor source = null, MDRDamageType damageType = null)
		{
			Amount = damage;
			DamageType = damageType ?? MDRDamageTypeLibrary.GlobalDefault;
			Source = source;
		}
	}


	/** Results of casting a spell */
	public class SpellResult
	{
		/** If the spell cast or failed. */
		public bool DidCast = false;
		public int DamageDone = 0;
		public int HealingDone = 0;
		public int MonstersKilled = 0;

		public MDRSpell Spell;

		public MDRActor Caster;
		public MDRActor Target;

		public SpellResult(MDRActor caster, MDRActor target, MDRSpell spell)
		{
			Caster = caster;
			Target = target;
			Spell = spell;
		}

		public override string ToString()
		{
			var format = "{0} cast {2} on {1}"; 

			if (Spell.HealingSpell)
				format = "{0} casts {2} and heals for {4}.";

			//todo:
			return "Spell";
			//return String.Format(format, CombatEngine.Format(Caster), CombatEngine.Format(Target), CombatEngine.Format(Spell), CombatEngine.Format(DamageDone), CombatEngine.Format(HealingDone)); 
		}
	}

	/** Class of spell.  */
	[DataObject("SpellClass", true)]
	public class MDRSpellClass : NamedDataObject
	{
		public int IconID;
		public string ShortName;
		public string SFXName;

		[FieldAttr("Skill", FieldReferenceType.Name)]
		/** The skill this spell class requires. */
		public MDRSkill Skill;

		public MDRSpellClass()
		{
		}

		/**
		 * The icon that represents this spell class.
		 */
		public Sprite Icon {
			get { return (IconID == -1) ? CoM.Instance.SpellIconSprites[0] : CoM.Instance.SpellIconSprites[IconID]; }
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			// Make sure we have a valid skill assigned.
			if (Skill == null) {
				Trace.LogWarning("Data error [SpellClass]: SpellClass {0} needs a valid skill, using default.", this);
				this.Skill = CoM.Skills[0];
			}
		}

	}

	/** Library containing all the games spells. */
	[DataObject("SpellLibrary")]
	public class MDRSpellLibrary : DataLibrary<MDRSpell>
	{
	}

	/** Different affects spells can have. */
	public enum SpellEffect
	{
		Damage,
		Kill,
		Heal,
		Dispell,

		FeatherEssence,

		SightVeil,
		SeeInvisible,
		DetectRock,
		FindDirection,
		DepthPerception,
		SoulSearch,

		Protection,
		Resist,

		Teleport,
		Displacement,
		EtherealPortal,
		RetreiveSoul,

		SetSanctury,
		Sanctury,

		Fate,

		Cure,
		CurePoison,
		CureDisease,
		CureParalysis,
		RaiseDead,
		RestoreFlesh,
		Resurrect,

		OpenChest,

		Charm,
		Control,
		Restrain,
	}

	[DataObject("Spell", true)]
	public class MDRSpell : NamedDataObject
	{
		/** The class of spell. */
		[FieldAttr("Class", FieldReferenceType.Name)]
		public MDRSpellClass SpellClass;

		/** The spell level required to cast this spell. */
		public int SpellLevel;

		/** The base damage this spell will do. */
		public int DamageBase;

		/** Additional damage based on level, up to a maximum of 4x DamageMod. */
		public int DamageMod;

		/** The type of damage this this spell does. */ 
		[FieldAttr("DamageType", FieldReferenceType.Name)]
		public MDRDamageType DamageType;

		/** Any speical effect this spell has, such as giving a resistance. */
		public SpellEffect Effect;

		/** Parameter for effect, for example if Effect is "resist" the effectvalue would be the name of the damage 
		 * type it resists. */
		public string EffectValue;

		/** The stats required to cast this spell. */
		public MDRStats RequiredStats;

		/** A description of  the spell. */
		public string Description;

		/** The maxmimum number of monsters this spell can effect in each group. */
		public int MaxTargetsPerGroup;

		/** The base cost of the spell.  Will reduce with spell level. */
		public int SpellCost;
	
		/** The icon to use for this spell. */
		public int IconID;

		/** True if this spell requires a combat target. */
		public bool CombatSpell { get { return SpellTarget == SpellTarget.Single || SpellTarget == SpellTarget.Area; } }

		/** If this spells requires user intervention to select a specific target. */
		public bool NeedsTargetSelection { get { return SpellTarget == SpellTarget.Character; } }

		/** Non combat spells can only be cast outside of combat.*/
		public bool NonCombatSpell;

		/** If this spell is a kill spell or not. */
		public bool KillSpell { get { return Effect == SpellEffect.Kill; } }

		/** If this spell is a healing spell or not. */
		public bool HealingSpell { get { return Effect == SpellEffect.Heal; } }

		/** If this spell is a charm spell or not. */
		public bool CharmSpell { get { return Effect == SpellEffect.Charm; } }

		/** Who this spell targets, i.e. a character or monster.*/
		public SpellTarget SpellTarget;

		/** Creates a new spell. */
		public MDRSpell()
		{
			RequiredStats = new MDRStats();
		}

		/** Detailed description of this spell, such as damage etc used in tooltips.*/
		public string FormattedDescription()
		{
			string targetDescription = "";

			switch (SpellTarget) {
				case SpellTarget.Single: 
					if (MaxTargetsPerGroup == 1)
						targetDescription = Util.Colorise("[Single]", Colors.GeneralHilightValueColor);
					else
						targetDescription = Util.Colorise("[Group] ", Colors.GeneralHilightValueColor) + "(max " + MaxTargetsPerGroup + ")";
					break;
				case SpellTarget.Area: 
					targetDescription = Util.Colorise("[Area] ", Colors.GeneralHilightValueColor) + "(max " + MaxTargetsPerGroup + ")";
					break;					
				case SpellTarget.Caster: 
					targetDescription = Util.Colorise("[Caster]", Colors.GeneralValueColor);
					break;			
				case SpellTarget.Character: 
					targetDescription = Util.Colorise("[Character]", Colors.GeneralValueColor);
					break;			
				case SpellTarget.Party: 
					targetDescription = Util.Colorise("[Party]", Colors.GeneralValueColor);
					break;											
			}

			string damageDescription = Util.Colorise(DamageBase + "-" + (DamageBase + DamageMod), Colors.GeneralValueColor);	

			switch (Effect) {
				case SpellEffect.Damage:					
					return string.Format("Deals {0} {1} damage.\n{2}", damageDescription, DamageType.Formatted, targetDescription);								
				case SpellEffect.Kill:					
					return string.Format("Deals {0} {1} damage and kills target, or deals no damge at all.\n{2}", damageDescription, DamageType.Formatted, targetDescription);								
				case SpellEffect.Heal:					
					return string.Format("Heals {0}.\n{1}", damageDescription, targetDescription);
				case SpellEffect.Resist:					
					return string.Format("Target gains resist {0}.\n{1}", EffectValue, targetDescription);
				case SpellEffect.Charm:					
					return string.Format("Charms {0}.\n{1}", EffectValue, targetDescription);
				
			}

			return "";
		}

		/** Applies healing. */
		protected void ApplyHealing(SpellResult info)
		{
			Util.Assert(HealingSpell, "Tried casting a non healing spell as a healing spell.");
				
			Util.Assert(info.Target != null, "Target for healing spell must not be null.");				

			if (info.Target.IsDead) {
				CoM.PostMessage("Can not cast {0} on {1}, they are dead.", info.Spell, info.Target);
				return;
			}

			int healing = CalculateSpellHealing(info.Caster, info.Target);	

			// caclulate the amount of healing done
			info.HealingDone = info.Target.ReceiveHealing(healing);
			info.DidCast = true;
			
		}

		/** Applies damage to given target. */
		protected void ApplyDamage(SpellResult info)
		{			
			//todo:
			/*
			for (int monsterLoop = 0; monsterLoop < MaxTargetsPerGroup; monsterLoop++) {

				int resisted = 0;

				int damage = CalculateSpellDamage(info.Caster, info.Target, out resisted);	

				//stub:
				damage = 99;

				bool killed = (!info.Target.IsDead && (damage >= info.Target.Hits));
				damage = info.Target.ReceiveDamage(new DamageInfo(damage, info.Caster, DamageType));

				Trace.LogDebug(" -damage = {0}, killed? {1}", damage, killed);

				if (info.Caster != null && info.Target != null && (info.Caster is MDRCharacter) && (info.Target is MDRMonsterStack))
					info.Caster.GainXP(GameRules.CalculateXP((MDRCharacter)info.Caster, ((MDRMonsterStack)info.Target).Monster, damage));

				if (!killed)
					break;

				info.MonstersKilled++;
			}			
			*/
			info.DidCast = true;
		}

		/** Applies the effects of the spell on a target. */
		protected void ApplyEffect(SpellResult info)
		{
			switch (Effect) {
				case SpellEffect.Damage:
					ApplyDamage(info);
					break;
				case SpellEffect.Heal:
					ApplyHealing(info);
					break;
			}
		}

		/** 
		 * Applyies the effects of this spell to given target.
		 * Returns the result that can be used to log information about the outcome.  		 
		 */
		public SpellResult CastTargeted(MDRActor caster, MDRActor target)
		{
			Trace.LogDebug("STUB: Casting {0} / {1}", caster, target);
			Debug.Assert(caster != null, "Invalid parameter.  Caster must not be null.");

			var info = new SpellResult(caster, target, this);
			ApplyEffect(info);
			return info;
		}

		/** 
		 * Applyies the effects of this spell to entire party.
		 * Returns one result foreach each party member effected.
		 */
		public List<SpellResult> CastParty(MDRActor caster, MDRParty target)
		{
			if (target == null)
				throw new Exception("Parameter 'target' must not be null.");

			var results = new List<SpellResult>();

			for (int lp = 0; lp < 4; lp++) {
				MDRCharacter character = target[lp];
				if (character != null && !character.IsDead)
					results.Add(CastTargeted(caster, character));				
			}						

			return results;

		}

		/**
		 * Applyies the effects of this spell to each monster stack in an area.
		 * Returns one result foreach each monster effected.
		 */
		public List<SpellResult> CastArea(MDRActor caster, MDRArea target)
		{
			//todo:
			/*
			if (target == null)
				throw new Exception("Parameter 'target' must not be null.");
			
			var results = new List<SpellResult>();

			for (int lp = 0; lp < 4; lp++) {
				MDRMonsterStack stack = target.Stack[lp];
				if (stack != null && !stack.IsEliminated)
					results.Add(CastTargeted(caster, stack));				
			}						

			return results;
			*/
			return new List<SpellResult>();
			

		}

		/**
		 * The icon that represents this spell.
		 */
		public Sprite Icon {
			get { return (IconID == -1) ? CoM.Instance.SpellIconSprites[0] : CoM.Instance.SpellIconSprites[IconID]; }
		}

		public override void WriteNode(XElement node)
		{
			base.WriteNode(node);
			WriteValue(node, "Description", Description);

		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);
		}

		/** Calculates damage delt by given spell to given target. */		 
		public int CalculateSpellDamage(MDRActor source, MDRActor target)
		{
			int resisted;
			return CalculateSpellDamage(source, target, out resisted);
		}

		/**
		 * Calculates the cost for given character to cast given spell.
		 * Returns 0 if spell can not be cast.		 
		 */
		public int CostFor(MDRCharacter character)
		{
			MDRGuild spellGuild = null;
			var characterSpellLevel = GameRules.CharacterGeneralAbility(character, SpellClass.Skill, out spellGuild);

			if (spellGuild == null)
				return 0;

			float factor = Util.Clamp(SpellLevel / characterSpellLevel, 0.1f, 1f);
			return (int)(SpellCost * factor);
		}

		public int CalculateSpellHealing(MDRActor source, MDRActor target)
		{
			int characterSpellLevel = (int)Math.Max(1, source.SpellPower(this));

			float levelFactor = Util.Clamp(4 * (1 - (SpellLevel / characterSpellLevel)), 1f, 4f);

			int bonusHealing = Util.Roll(DamageMod);

			int finalHealing = (int)(DamageBase * levelFactor) + bonusHealing;

			return finalHealing;
		}

		/** Calculates damage delt by given spell to given target. */		 
		public int CalculateSpellDamage(MDRActor source, MDRActor target, out int resisted)
		{
			int characterSpellLevel = (int)Math.Max(1, source.SpellPower(this));

			float levelFactor = Util.Clamp(4 * (1 - (SpellLevel / characterSpellLevel)), 1f, 4f);

			int bonusDamage = Util.Roll(DamageMod);

			int finalDamage = (int)(DamageBase * levelFactor) + bonusDamage;

			if (KillSpell && (target.Hits > finalDamage)) {
				finalDamage = 0;
			}

			//todo: resistance.
			resisted = 0;

			return finalDamage;
		 
		}

	}
}
