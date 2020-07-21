using System;
using Data;
using UnityEngine;
using System.Collections.Generic;

namespace Mordor
{
	public delegate void RecieveDamageTrigger(DamageInfo damage);
	public delegate void WasMissedTrigger(MDRActor attacker);

	/** Base class for things that can be in combat.  I.e. monsters and characters */
	[DataObject("Actor")]
	public class MDRActor: NamedDataObject
	{
	
		/** Alignment, good, evil etc. */
		public MDRAlignment Alignment;

		/** Current stats */
		public MDRStats Stats;

		/** Our current resistances */
		public MDRResistance Resistance;

		public int MaxSpells;
		public int MaxHits;
		public int Spells;
		public int Hits;

		[FieldAttrAttribute(true)]
		public List<MDRSpell> KnownSpells;

		virtual public Sprite Portrait { get { return null; } }

		virtual public float NominalDamage { get { return 1.0f; } }

		virtual public int CombatLevel { get { return 0; } }

		virtual public int TotalArmour { get { return 0; } }

		virtual public float TotalCriticalHitChance { get { return 0; } }

		virtual public float TotalBackstabChance { get { return 0; } }

		virtual public float TotalArmourPierce { get { return 0; } }

		virtual public float TotalHitBonus { get { return 0; } }

		virtual public ActionType ActionThisRound { get { return ActionType.Fight; } }

		public RecieveDamageTrigger OnReceiveDamage;
		public RecieveDamageTrigger OnReceiveHealing;
		public WasMissedTrigger OnWasMissed;

		/** Heals the character given amount.  Caps at max health.  Returns number of actual hits healed. */
		virtual public int ReceiveHealing(int amount)
		{
			if (Hits + amount > MaxHits) {
				amount = MaxHits - Hits;
			}
			Hits += amount;

			if (OnReceiveHealing != null) {
				OnReceiveHealing(new DamageInfo(amount, null, MDRDamageType.Healing));
			}

			return amount;
		}

		/** Notifies the actor that someone attacked but missed them. */
		virtual public void WasMissed(MDRActor attacker = null)
		{
			if (OnWasMissed != null)
				OnWasMissed(attacker);
		}

		/** 
		 * Damages the actor given amount.  Damage is direct, no resistances or armour is applied.		 
		 * Returns number of actual hits done, capped when health reaches 0.
		 * 
		 * @amount The amount to damage.
		 * @damageType The type of damage dealt.
		 * @source Who attacked us.
		 * 
		 */
		virtual public int ReceiveDamage(DamageInfo damage)
		{
			if (damage.Amount > Hits) {
				damage.Amount = Hits;
			}
			Hits -= damage.Amount;
			if (Hits == 0) {
				Die();
			}
			if (OnReceiveDamage != null)
				OnReceiveDamage(damage);
			
			return damage.Amount;
		}


		/**
		 * Damages the actor given amount, uses 'pysical' damage, and null source. 
		 */
		public int ReceiveDamage(int damage)
		{
			return ReceiveDamage(new DamageInfo(damage));
		}

		/**
		 * Returns the actor highest spell level for a given spell, or 0 if they are unable to cast it.
		 * This differs from passing a spell class in that a lv100 paladin would get a lv50 spell level for a basic heal, but not for
		 * a higher level heal that paladins can not cast.
		 */
		virtual public int SpellPower(MDRSpell spell)
		{
			return 0;
		}

		/**
		 * Returns if this actor can cast given spell 
		 */
		virtual public bool CanCast(MDRSpell spell)
		{
			return SpellPower(spell) > 0;
		}

		/** Returns if this actor can cast spells or not */
		virtual public bool CanCastSpells {
			get { return (CalculateKnownSpells().Count > 0); }
		}

		/** Returns a list of all known spells */
		virtual protected List<MDRSpell> CalculateKnownSpells()
		{
			var list = new List<MDRSpell>();
			foreach (MDRSpell spell in CoM.Spells) {				
				if (CanCast(spell))
					list.Add(spell);
			}
			return list;
		}

		/** Called when this actor's health is reduced to 0. */
		virtual public void Die()
		{

		}

		/** 
		 * If this actor is dead or not 
		 */
		public bool IsDead {
			get { return (Hits <= 0); }
		}

		/**
		 * Frees resources and event handleres for actor.
		 */
		public void Destroy()
		{
			OnReceiveDamage = null;
		}

		virtual public void GainXP(int xp)
		{
		}


		/** Returns the sound effect name to use when this actor is hurt. */
		public virtual string HurtSFXName { get { return ""; } }

		/** Returns the sound effect name to use when this actor dies. */
		public virtual string DieSFXName { get { return ""; } }

		/** Returns the sound effect name to use when this actor attacks normally. */
		public virtual string AttackSFXName { get { return ""; } }

	}
}

