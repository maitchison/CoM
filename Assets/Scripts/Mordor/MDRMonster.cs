
using System;
using UnityEngine;

using Data;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Mordor
{
	/** A library of monster classes. */
	[DataObjectAttribute("MonsterClassLibrary")]
	public class MDRMonsterClassLibrary : DataLibrary<MDRMonsterClass>
	{
	}

	/** A class monster. */
	[DataObjectAttribute("MonsterClass", true)]
	public class MDRMonsterClass : NamedDataObject
	{
	}

	/** A library of monster types. */
	[DataObjectAttribute("MonsterTypeLibrary")]
	public class MDRMonsterTypeLibrary : DataLibrary<MDRMonsterType>
	{
	}

	/** A type of monster. */
	[DataObjectAttribute("MonsterType", true)]
	public class MDRMonsterType : NamedDataObject
	{
		[FieldAttr("Class", FieldReferenceType.Name)]
		public MDRMonsterClass TypeClass;
		public string AttackSound;
		public string DieSound;

		public int DefaultBoxChance;
		public int DefaultChestChance;
		public int DefaultLockedChance;
		public int DefaultItemChance;
		public string DefaultLootList;

		public MDRMonsterType()
		{
			AttackSound = null;
			DieSound = null;
		}
	}

	/** A library containing information about each item */
	[DataObject("MonsterLibrary")]
	public class MDRMonsterLibrary : DataLibrary<MDRMonster>
	{
		private void LinkCompanions()
		{
			foreach (MDRMonster monster in DataList) {
				// Match companion name to id's
				var companionName = monster._companionName;
				if (!String.IsNullOrEmpty(companionName)) {
					monster.Companion = ByName(monster._companionName);
					if (monster.Companion == null)
						Trace.LogWarning("Data Error [Companion]: Companion '{0}' not found for monster [{1}] {2}.  Ignoring.", companionName, monster.ID, monster.Name);
				}
			}
		}

		public override void ReadNode(XElement node)
		{						
			base.ReadNode(node);
			LinkCompanions();
		}
	}

	public enum MonsterAIMode
	{
		// The normal movement AI mode.  Move wander the dungeon, then chase down party.
		Normal
	}

	/** Defines a monster */
	[DataObject("Monster", true)]
	public class MDRMonster : NamedDataObject
	{
		[FieldAttr("Type", FieldReferenceType.Name)]
		public MDRMonsterType Type;

		public int HitPoints;
		public int SpawnCount;
		[FieldAttr(true)]
		public int PortraitID;
		public int AppearsOnLevel;
		public float EncounterChance;
		public float Damage;
		public int GuildLevel;

		public int Armour;
		public int BonusHit;
		public int ArmourPierce;

		public MDRStats Stats;
		public MDRResistance Resistance;

		/** The distance this monster will wander from it's spawn area.  0 will mean it will never leave it's area. */
		//todo: this is hard coded for the moment
		[FieldAttr(true)]
		public int WanderDistance = 10;

		/** Attack speed in attacks per second. */
		//todo: this is hard coded for the moment
		[FieldAttr(true)]
		public float AttackSpeed = 1.0f;

		/** Movement speed in tiles per second */
		//todo: this is hard coded for the moment
		[FieldAttr(true)]
		public float MoveSpeed = 0.5f;

		/** The distance in tiles before this monster will agro towards player. */
		//todo: this is hard coded for the moment
		[FieldAttr(true)]
		public float AgroRange = 5f;

		/** If this monster is able to open doors or not */
		//todo: this is hard coded for the moment
		[FieldAttr(true)]
		public bool CanOpenDoors = true;

		/** Defines the movement patterns of the monster. */
		//todo: this is hard coded for the moment
		public const MonsterAIMode AIMode = MonsterAIMode.Normal;

		public int BoxChance;
		public int ChestChance;
		public int LockedChance;
		public int ItemChance;
		public String LootList;

		public MonsterAbilities Abilities;
		public BitArray SpellClasses;

		/** Overrides monster type sound if not null or empty string. */
		[FieldAttr("AttackSound")]
		public string AttackSoundOverride;
		/** Overrides monster type sound if not null or empty string. */
		[FieldAttr("DieSound")]
		public string DieSoundOverride;

		/** Gold dropped per monster */
		public int GoldDrop;

		public int ItemLevel { get { return AppearsOnLevel + ItemLevelMod; } }

		public int ItemLevelMod;

		public string AttackSound { get { return (AttackSoundOverride ?? Type.AttackSound) ?? "MonsterGenericAttack" + Util.Roll(5, true); } }

		public string DieSound { get { return (DieSoundOverride ?? Type.DieSound) ?? "MonsterGenericDie" + Util.Roll(3); } }

		/** Name of companion for this monster, will be linked up with Companion. The MonsterDataLibrary does this
		   automatically.*/
		internal string _companionName;

		[FieldAttrAttribute(true)]
		public MDRMonster Companion;

		public Sprite Portrait { get { return (PortraitID >= 0) ? CoM.Instance.MonsterSprites[PortraitID] : null; } }

		/** How difficult this monster is to identify */
		public float IdDifficulty { get { return GameRules.MonsterIdentifyDifficulty(this); } }

		/** How difficult this monster is to defeat in combat */
		public int ToughnessRaiting { get { return GuildLevel * 2 + Stats.Str + Stats.Dex + (int)Damage + (int)(CriticalHitChance) + (int)(BackstabChance); } }

		public float CriticalHitChance { get { return Abilities.CriticalHit ? 4f + Mathf.Log(GuildLevel, 2f) : 0; } }

		public float BackstabChance {
			get { 
				if (!Abilities.Backstab)
					return 0;
				float dexMod = Stats.Dex - 12;
				if (dexMod < 0)
					dexMod *= 0.25f;
				return 6f + Mathf.Log(GuildLevel * 2f, 2f) + Util.Clamp(dexMod, -5f, 99f);
			}
		}


		//stub: ignore known spells for a sec... as they can't be loaded at the moment (generic list of spells not supported in reader)
		[FieldAttrAttribute(true)]
		public List<MDRSpell> KnownSpells;

		public int SpellPower(MDRSpell spell)
		{ 
			return AppearsOnLevel;
		}

		public MDRMonster()
		{
			Stats = new MDRStats();
			Abilities = new MonsterAbilities();
			SpellClasses = new BitArray(32);
			Resistance = new MDRResistance();
			KnownSpells = new List<MDRSpell>();
		}

		public override string ToString()
		{
			return Name;
		}

		/** Returns if this monster can cast this spell class or not */
		public bool CanCast(MDRSpellClass spellClass)
		{
			return SpellClasses[spellClass.ID];
		}

		/** Returns if this monster can cast given spell or not */
		public bool CanCast(MDRSpell spell)
		{
			if (!CanCast(spell.SpellClass))
				return false;
			return (spell.CombatSpell) && (AppearsOnLevel >= spell.SpellLevel);
		}

		public bool CanCastSpells {
			get {
				return KnownSpells.Count > 0;
			}
		}

		public override void WriteNode(XElement node)
		{
			Trace.LogWarning("Writing MDRMonsters is not fully supported.  Some custom fields will be missing.");
			base.WriteNode(node);
			WriteValue(node, "KnownSpells", KnownSpells, FieldReferenceType.Name);
			WriteValue(node, "IdDifficulty", IdDifficulty);
			WriteValue(node, "_ToughnessRating", ToughnessRaiting);
		}

		public override void ReadNode(XElement node)
		{						
			// Get defaults from type.
			Util.Assert(CoM.MonsterTypes != null, "Monster types must be loaded before monsters.");
			Type = CoM.MonsterTypes.ByName(ReadValue(node, "Type"));

			BoxChance = Type.DefaultBoxChance;
			ChestChance = Type.DefaultChestChance;
			LockedChance = Type.DefaultLockedChance;
			ItemChance = Type.DefaultItemChance;

			base.ReadNode(node);

			LootList = LootList.Replace("[default]", Type.DefaultLootList);

			_companionName = ReadValue(node, "Companion");

			// match portrait names to id's
			if (CoM.GraphicsLoaded) {
				var portraitIDString = ReadValue(node, "PortraitID");
				if (!Int32.TryParse(portraitIDString, out PortraitID)) {
					var sprite = CoM.Instance.MonsterSprites.ByName(portraitIDString);
					if (sprite == null) {
						Trace.LogWarning("Data Error [Missing Sprite]: Sprite '{0}' for monster [{1}] {2} not found. Using default.", portraitIDString, ID, Name);
						sprite = CoM.Instance.MonsterSprites.Default;
					}
					PortraitID = sprite.ID;
				}
			}

			if (Type == null) {						
				Trace.LogError("Monster [{0}]{1} has no type.", ID, Name);
				throw new Exception("Invalid type for monster.");
			}
		}
	}

	[DataObject("Abilities")]
	public class MonsterAbilities : BooleanPropertyList
	{
		[FieldAttrAttribute("See Invisible")]
		public bool SeeInvisible;
		public bool Invisible;
		[FieldAttrAttribute("Immune to Magic")]
		public bool ImmuneToMagic;
		[FieldAttrAttribute("Charm Resistent")]
		public bool CharmResistent;
		[FieldAttrAttribute("Weapon Resistent")]
		public bool WeaponResistent;
		[FieldAttrAttribute("Immune to Weapon")]
		public bool ImmuneToWeapon;
		[FieldAttrAttribute("Critical Hit")]
		public bool CriticalHit;
		public bool Backstab;
		[FieldAttrAttribute("Destroy Item")]
		public bool DestroyItem;
		public bool Poison;
		public bool Disease;
		public bool Paralyze;
		[FieldAttrAttribute("Breath Fire")]
		public bool BreathFire;
		[FieldAttrAttribute("Breath Cold")]
		public bool BreathCold;
		public bool Acid;
		public bool Electrocute;
		public bool Drain;
		public bool Stone;
		public bool Age;
		public bool Steal;

	}
}

