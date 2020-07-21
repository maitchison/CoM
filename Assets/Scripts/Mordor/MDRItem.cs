using System;
using System.Collections;
using System.Xml.Linq;

using UnityEngine;

using Data;

namespace Mordor
{
	[DataObjectAttribute("ItemClassLibrary")]
	public class MDRItemClassLibrary : DataLibrary<MDRItemClass>
	{
	}

	/** Library if item classes */
	[DataObjectAttribute("ItemClass", true)]
	public class MDRItemClass : NamedDataObject
	{
		public ItemLocation Location;

		public bool Usable;

		public MDRItemClass()
		{

		}

		public bool isWeapon { get { return Name == "Weapon"; } }

		public bool isShield { get { return Name == "Shield"; } }
	}

	/** Library of item types */
	[DataObjectAttribute("ItemTypeLibrary")]
	public class MDRItemTypeLibrary : DataLibrary<MDRItemType>
	{
	}

	/** Defines the class of an item.  A item may have only one class */
	[DataObjectAttribute("ItemType", true)]
	public class MDRItemType : NamedDataObject
	{
		[FieldAttr("Class", FieldReferenceType.Name)]
		public MDRItemClass TypeClass;

		public int IconID;

		[FieldAttr("SkillRequired", FieldReferenceType.Name)]
		public MDRSkill SkillRequired;

		public MDRItemType()
		{
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			if (ReadValue(node, "SkillRequired") != "" && SkillRequired == null)
				Trace.LogWarning("Data Error [SkillRequired]: Item type has a skill requirement {0} but this does not match any skills.  Ignoring requirement.", ReadValue(node, "SkillRequired"));

			if (TypeClass == null) {
				TypeClass = CoM.ItemClasses.Default;
				Trace.LogWarning("Data Error [ItemType]: ItemType {0} has invalid class, assigning default.", this);
			}
		}
	}

	/** A library containing information about each item */
	[DataObjectAttribute("ItemLibrary")]
	public class MDRItemLibrary : DataLibrary<MDRItem>
	{
	}

	/** Represents a Mordor item */
	[DataObjectAttribute("Item", true)]
	public class MDRItem : NamedDataObject
	{
		[FieldAttr("Type", FieldReferenceType.Name)]
		public MDRItemType Type;

		public ItemRarity Quality;

		public int Value;
		public int AppearsOnLevel;
		public int ChanceOfFinding;
		public ItemAbilities Abilities;
		public int Swings;
		public int SpellID;
		/** The items max charges */
		public int DefaultCharges;

		public int BaseStoreQuantity;

		public int Armour;
		public int Hit;
		public int Pierce;

		/** Which guilds can use this item, organised by ID. */
		private BitArray guildsMask;

		public float Damage;
		public int Hands;
		/** Required stats to use */
		public MDRStats StatsReq;
		/** Modifiers to stats */
		public MDRStats StatsMod;
		/** Spell level of item, -1 for casters spell level */
		public int SpellLevel;

		/** The skill level required to use this item.  Skill type is Type.SkillRequirement.  If this is null SkillLevel
		 * has no effect. */
		public float SkillLevel;

		/** How difficult this item is to identify */
		public float IdDifficutly { get { return GameRules.ItemIdentifyDifficulty(this); } }

		public ItemCurseType CurseType;

		public MDRResistance Resistance;

		/** The icon to use to display this item */
		[FieldAttrAttribute(true)]
		public int IconID;

		public bool isWeapon { get { return Type.TypeClass.isWeapon; } }

		public bool isShield { get { return Type.TypeClass.isShield; } }

		public Sprite Icon { get { return (IconID >= 0 || IconID < CoM.Instance.ItemIconSprites.Count) ? CoM.Instance.ItemIconSprites[IconID] : null; } }

		/** Creates a new item */
		public MDRItem()
		{
			guildsMask = new BitArray(32);
			StatsReq = new MDRStats();
			StatsMod = new MDRStats();
			Abilities = new ItemAbilities();
			Resistance = new MDRResistance();
		}

		/**
		 * Returns if a guild is able to use this item or not
		 */
		public bool GuildCanUseItem(MDRGuild guild)
		{
			return guildsMask[guild.ID];
		}

		/** Returns a colorised version of the items name.  The color refelects the quality */
		public string ColorisedName { 
			get {
				return Util.Colorise(Name, QualityColor);
			}
		}

		public override string ToString()
		{
			return Name;
		}

		/** If this item is potentially usable */
		public bool Usable {
			get { return Type.TypeClass.Usable; }
		}

		/** Returns the reason why the given player can't use the item, or an empty string if they can */
		public string GetCanNotUseItemReason(MDRCharacter target)
		{
			if (!Usable)
				return "This type of item is not usable.";

			// check stats
			if (!(target.BaseStats >= StatsReq))
				return target + " does not have enough stats to use this item.";

			// check guild usage
			if (!GuildCanUseItem(target.CurrentGuild))
				return "Our current guild [" + target.CurrentGuild + "] can not use this item.";

			// check skills
			if (Type.SkillRequired != null) {
				int skillID = Type.SkillRequired.ID;
				if (target.SkillLevel[skillID] == 0)
					return string.Format("{0} not have any skill in {1} to use this item.", target, Type.SkillRequired);
				if (target.SkillLevel[skillID] < SkillLevel)
					return string.Format("{0} is not yet skilled enough in {1} to use this item.", target, Type.SkillRequired);
			}
			return "";
		}

		/** The critical chance of this item */
		public float CriticalModifier {
			get { return GameRules.WeaponCriticalHitChance(this); }

		}

		/** The backstab chance of this item */
		public float BackstabModifier {
			get { return GameRules.WeaponBackstabChance(this); }
		}

		/** Returns the reason why the given player can't equip the item, or an empty string if they can */
		public string GetCanNotEquipItemReason(MDRCharacter target)
		{
			// check stats
			if (!(target.BaseStats >= StatsReq))
				return target + " does not have enough stats to equip this item.";

			// check guild usage
			if (!GuildCanUseItem(target.CurrentGuild))
				return "The " + target.CurrentGuild + " guild can not equip this item.";

			// check skills
			if (Type.SkillRequired != null) {
				int skillID = Type.SkillRequired.ID;
				if (target.SkillLevel[skillID] == 0)
					return string.Format("{0} not have any skill with {1} to equip this item.", target, Type.SkillRequired);
				if (target.SkillLevel[skillID] < SkillLevel)
					return string.Format("{0} is not yet skilled enough in {1} to equip this item.", target, Type.SkillRequired);
			}

			return "";
		}

		public override void WriteNode(XElement node)
		{
			Trace.LogWarning("Writing MDRItems is not fully supported.  Some custom fields will be missing.");
			base.WriteNode(node);
			WriteValue(node, "IdDifficulty", IdDifficutly);
		}

		/** True if this item has some guild restrictions. */
		public bool GuildRestricted {
			get {
				return guildsMask.CountBits() != guildsMask.Count;
			}
		}

		public override void ReadNode(XElement node)
		{
			base.ReadNode(node);

			Util.Assert(CoM.Guilds != null, "Guilds must be loaded before items.");

			// guild restrictions
			guildsMask.SetAll(false);
			string guildRestrictions = ReadValue(node, "GuildRestrictions");
			if (guildRestrictions == "")
				guildsMask.SetAll(true);
			else
				foreach (string guildName in guildRestrictions.Split(',')) {
					var guild = CoM.Guilds[guildName.Trim()];
					if (guild != null) {
						guildsMask[guild.ID] = true;
					} else
						Trace.LogWarning("Data Error [MDRItem]: Guild '{0}' in guild restrictions not found.", guildName);
				}

			// match portrait names to id's
			if (CoM.GraphicsLoaded) {
				var IconIDString = ReadValue(node, "IconID");
				if (!Int32.TryParse(IconIDString, out IconID)) {
					var sprite = CoM.Instance.ItemIconSprites.ByName(IconIDString);
					if (sprite == null) {
						Trace.LogWarning("Data Error [Missing Sprite]: Sprite '{0}' for item [{1}] {2} not found. Using default.", IconIDString, ID, Name);
						sprite = CoM.Instance.MonsterSprites.Default;
					}
					IconID = sprite.ID;
				}
			}


			if (Type == null) {
				Type = CoM.ItemTypes.Default;
				Trace.LogWarning("Data Error [Item]: Item {0} has invalid type, assigning default.", this);
			}

		}

		/**
		 * Returns an icon id representing the given item location 
		 */
		public static int GetSpriteForItemLocation(ItemLocation location)
		{
			var defaultSprite = CoM.Instance.ItemIconSprites.Default;

			switch (location) {
				case ItemLocation.Neck:
					return CoM.Instance.ItemIconSprites.ByName("SlotNeck", defaultSprite).ID;
				case ItemLocation.RightHand:
					return CoM.Instance.ItemIconSprites.ByName("SlotRightHand", defaultSprite).ID;
				case ItemLocation.LeftHand:
					return CoM.Instance.ItemIconSprites.ByName("SlotLeftHand", defaultSprite).ID;
				case ItemLocation.Head:
					return CoM.Instance.ItemIconSprites.ByName("SlotHead", defaultSprite).ID;
				case ItemLocation.Body:
					return CoM.Instance.ItemIconSprites.ByName("SlotBody", defaultSprite).ID;
				case ItemLocation.Back:
					return CoM.Instance.ItemIconSprites.ByName("SlotBack", defaultSprite).ID;
				case ItemLocation.Hands:
					return CoM.Instance.ItemIconSprites.ByName("SlotHands", defaultSprite).ID;
				case ItemLocation.Finger:
					return CoM.Instance.ItemIconSprites.ByName("SlotFinger", defaultSprite).ID;
				case ItemLocation.Waist:
					return CoM.Instance.ItemIconSprites.ByName("SlotWaist", defaultSprite).ID;
				case ItemLocation.Feet:
					return CoM.Instance.ItemIconSprites.ByName("SlotFeet", defaultSprite).ID;
				case ItemLocation.Misc:
					return CoM.Instance.ItemIconSprites.ByName("SlotMisc", defaultSprite).ID;
				default:
					return -1;
			}
		}

		public float BaseIDDifficulty {
			get {
				switch (Quality) {
					case ItemRarity.Poor:
						return 0.5f;
					case ItemRarity.Common:
						return 1f;
					case ItemRarity.Uncommon:
						return 1.5f;
					case ItemRarity.Rare:
						return 2f;
					case ItemRarity.Epic:
						return 3f;
					case ItemRarity.Legendary:
						return 4f;
					default:
						return 1f;
				}
			}
		}

		public Color QualityColor {
			get {
				switch (Quality) {
					case ItemRarity.Poor:
						return new Color(0.7f, 0.7f, 0.7f);
					case ItemRarity.Common:
						return new Color(1.0f, 1.0f, 1.0f);
					case ItemRarity.Uncommon:
						return new Color(0.5f, 1.0f, 0.5f);
					case ItemRarity.Rare:
						return new Color(0.5f, 0.7f, 1.0f);
					case ItemRarity.Epic:
						return new Color(1.0f, 0.3f, 1.0f);
					case ItemRarity.Legendary:
						return new Color(1.0f, 1.0f, 0.3f);
					default:
						return Color.white;
				}
			}
		}

		public Color QualityBackgroundColor {
			get {
				switch (Quality) {
					case ItemRarity.Poor:
						return new Color(1.0f, 1.0f, 1.0f, 0f);
					case ItemRarity.Common:
						return new Color(1.0f, 1.0f, 1.0f, 0f);
					case ItemRarity.Uncommon:
						return new Color(0.5f, 1.0f, 0.5f, 0.5f);
					case ItemRarity.Rare:
						return new Color(0.5f, 0.7f, 1.0f, 1f);
					case ItemRarity.Epic:
						return new Color(1.0f, 0.3f, 1.0f, 1f);
					case ItemRarity.Legendary:
						return new Color(1.0f, 1.0f, 0.3f, 1f);
					default:
						return Color.white;
				}
			}
		}
	}


	[DataObject("Abilities")]
	public class ItemAbilities : BooleanPropertyList
	{
		public bool Levitate;
		public bool Invisible;
		public bool Protect;
		[FieldAttr("See Invisible")]
		public bool SeeInvisible;
		[FieldAttr("Critical Hit")]
		public bool CriticalHit;
		public bool Backstab;
		/** Item permanently increases stats when used */
		[FieldAttr("Permanent Stat Modifier")]
		public bool PermanentStatModifier;
		/** Item recharges spell points when used */
		[FieldAttr("Spell Point Recharge")]
		public bool SpellPointRecharge;
		/** Item reduces age when used */
		[FieldAttr("Reduces Age")]
		public bool ReducesAge;
		/** Item is a guild crest */
		[FieldAttr("Guild Crest")]
		public bool GuildCrest;
	}

	public enum ItemRarity
	{
		Poor,
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary
	}

	/** Defines the curse type of an item */
	public enum ItemCurseType
	{
		None = 0,
		Cursed = 1,
		AutoEquipCursed = 2
	}

	/** Defines where the item is worn */
	public enum ItemLocation
	{
		None,
		RightHand,
		LeftHand,
		Head,
		Body,
		Back,
		Neck,
		Hands,
		Finger,
		Waist,
		Feet,
		Misc,
		// can be equiped anywhere
		Any
	}

}