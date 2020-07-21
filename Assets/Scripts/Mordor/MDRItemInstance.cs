
using System;
using System.Collections;
using System.Xml.Linq;

using UnityEngine;

using Data;

namespace Mordor
{

	/** Represents an instance of an item in mordor.  Records per instance values such as charges */
	public class MDRItemInstance
	{
		/** The item this instance references */
		public MDRItem Item;
		/** Number of charges item has remaining */
		public int RemainingCharges;

		/** If true this item is currently cursed.  Item instances can be uncursed even if their item is a cursed item */
		public bool Cursed = false;

		/** True if this item is known to be cursed.  It must be either equiped, or fully identified to know this. */
		public bool KnownToBeCursed = false;

		/** The identification level of the item */
		public IdentificationLevel IDLevel { get { return _idLevel; } set { setIDLevel(value); } }

		private IdentificationLevel _idLevel;

		public string Name { get { return IDLevel.ItemName(Item); } }

		public static MDRItemInstance Create(MDRItem item, IdentificationLevel idLevel = null)
		{
			if (item == null)
				throw new ArgumentNullException("item");
			MDRItemInstance result = new MDRItemInstance();
			result.Item = item;
			result.IDLevel = idLevel ?? IdentificationLevel.Full;
			result.RemainingCharges = Math.Min(item.DefaultCharges, item.Usable ? 1 : 0);
			result.Cursed = (item.CurseType != ItemCurseType.None);
			result.KnownToBeCursed = (idLevel == IdentificationLevel.Full) && (result.Cursed);
			return result;
		}

		/** Creates an instance of the given item */
		public MDRItemInstance()
		{
		}

		/**
		 * Returns if the player can use this item or not 
		 */
		public bool CanBeUsedBy(MDRCharacter target)
		{			
			return (target == null) ? false : GetCanNotUseReason(target) == "";
		}

		/**
		 * Returns if the player can use this item or not 
		 */
		public bool CanBeEquipedBy(MDRCharacter target)
		{
			return target == null ? false : GetCanNotEquipReason(target) == "";
		}


		public string GetCanNotEquipReason(MDRCharacter target)
		{
			if (!IDLevel.CanUse)
				return "This item needs to be identified before it can be equiped.";

			return Item.GetCanNotEquipItemReason(target);
		}

		/** Returns the reason why the given player can't use the item, or an empty string if they can */
		public string GetCanNotUseReason(MDRCharacter target)
		{
			if (!IDLevel.CanUse)
				return "This item needs to be identified before it can be used.";

			return Item.GetCanNotEquipItemReason(target);
		}

		/** Removes any item from instance */
		public void Clear()
		{
			Item = null;
			RemainingCharges = 0;
			Cursed = false;
			KnownToBeCursed = false;
			IDLevel = IdentificationLevel.Auto;
		}

		/** Updates the identification level */
		private void setIDLevel(IdentificationLevel idLevel)
		{
			_idLevel = idLevel;
			if (idLevel == IdentificationLevel.Full)
				KnownToBeCursed = Item.CurseType != ItemCurseType.None;
		}

		/** Returns the cost required to ID this item to the next level */
		public int IDPrice()
		{
			if (IDLevel == IdentificationLevel.Full)
				return 0;

			float factor = 1.0f;

			// not sure why, but potions seem to be considerably cheaper to identify.  Might be related to number
			// of charges.
			if (Item.Type == CoM.ItemTypes.ByName("Potion"))
				factor = 1f / 3.5f;

			return (int)(Item.Value * IDLevel.IDMod * factor);
		}

		/** The cost to uncurse an item */
		public int UncursePrice()
		{
			//todo:
			return 0;
		}

		public override string ToString()
		{
			return Item.ToString();
		}

		/** Causes item to be used on given character */
		public void Use(MDRCharacter target)
		{
			if (target == null)
				throw new Exception("Item target must not be null");
			
			if (RemainingCharges <= 0)
				throw new Exception("Item has no charges");

			if (!CanBeUsedBy(target))
				throw new Exception("Target can not use this item");
			
			// add stats

			target.ModifyStats(Item.StatsMod);

			// reduce charges
			RemainingCharges--;
			target.UnequipInvalidItems();
			target.RemoveUsedItems();
		}
	}

	/** 
	 * Stores an item instance. 
	 */
	public class MDRItemSlot : DataObject
	{
		/** Called before an item put in this slot, return false to cancel */
		public event BeforeSetItemEvent OnBeforeSetItem;

		public delegate bool BeforeSetItemEvent(object source,MDRItemInstance item);

		/** Called just after an item has been placed in this slot, use to update status. */
		public event AfterSetItemEvent OnAfterSetItem;

		public delegate void AfterSetItemEvent(MDRItemSlot source,MDRItemInstance item);

		/** Used for two handed items.  If this slots is paired it will check if paired slots for an equiped item when
		 * equiping a two handed item, and display the two handed item faded out in the paried slot if equiped */
		public MDRItemSlot PairedSlot;

		/** Used to restrict the types of items that can be put in this slot */
		public ItemLocation Restriction;

		/** The character this slot is associated with (may be null in the case of shop slots etc */
		public MDRCharacter Character;

		/** The item stored */
		private MDRItemInstance _itemInstance;

		public MDRItemInstance ItemInstance { get { return _itemInstance; } set { setItem(value); } }

		/** True if this slot is an equipable slot */
		public bool IsEquipSlot { get { return Restriction != ItemLocation.Any; } }

		/** Number of items stacked together */
		public int Quantity;

		/** Creates a new item slot */
		public MDRItemSlot()
		{
			Restriction = ItemLocation.Any;
			Quantity = 0;
		}

		public MDRItemSlot Create(MDRItemInstance item = null, ItemLocation restriction = ItemLocation.Any, MDRCharacter character = null)
		{
			MDRItemSlot result = new MDRItemSlot();
			result._itemInstance = item;
			result.Restriction = restriction;
			result.Character = character;
			return result;
		}

		/** Returns true if this slot is empty */
		public bool IsEmpty {
			get { return (ItemInstance == null) || (ItemInstance.Item == null); }
		}

		/** Attempts to set this slots item to given value, returns true if sucessful.  Items can only be set if the restriction conditions are meet */
		private bool setItem(MDRItemInstance item)
		{
			if (CanAccept(item)) {
				_itemInstance = item; 
				if (Character != null) {
					if (item != null)
						Character.IdentifyItem(item);					
				}
				Quantity = 1;
				if (OnAfterSetItem != null)
					OnAfterSetItem(this, item);
				return true;
			} else {
				Trace.Log("Can not transfer " + item + " to this slot.");
				return false;
			}
		}

		/** Returns a string representation of the slots content */
		public override string ToString()
		{
			if (ItemInstance != null)
				return ItemInstance.ToString();
			else
				return "Empty";
		}

		/** Returns if this slots can accept the given item */
		public bool CanAccept(MDRItemInstance itemInstance)
		{
			if ((OnBeforeSetItem != null) && (OnBeforeSetItem(this, itemInstance) == false))
				return false;
			if (itemInstance == null)
				return true;
			if (Restriction == ItemLocation.Any)
				return true;
			return (itemInstance.Item.Type.TypeClass.Location == Restriction);
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			if (IsEmpty)
				return;
			node.Value = ItemInstance.Item.ID.ToString();
			if (Quantity != 1)
				WriteAttribute(node, "Quantity", Quantity);
			if (ItemInstance.RemainingCharges != 0)
				WriteAttribute(node, "Charges", ItemInstance.RemainingCharges);
			if (ItemInstance.IDLevel != IdentificationLevel.Full)
				WriteAttribute(node, "IDLevel", ItemInstance.IDLevel.Name);
			if (ItemInstance.KnownToBeCursed)
				WriteAttribute(node, "KnownToBeCursed", ItemInstance.KnownToBeCursed);
			if (ItemInstance.Cursed)
				WriteAttribute(node, "Cursed", ItemInstance.Cursed.ToString());

		}

		public override void ReadNode(XElement node)
		{
			if (String.IsNullOrEmpty(node.Value)) {
				ItemInstance = null;
			} else {
				_itemInstance = new MDRItemInstance();

				var item = CoM.Items.ByID(int.Parse(node.Value));

				if (item == null) {
					Trace.LogWarning("Data Error [Store]: Inventory references an item that doesn't exist.  Item ID {0}.  This item will be ignored.", node.Value);
					_itemInstance = null;
					return;
				}
					
				_itemInstance.Item = item;
			
				Quantity = ReadAttributeInt(node, "Quantity", 1);
				_itemInstance.RemainingCharges = ReadAttributeInt(node, "Charges", 0);
				_itemInstance.IDLevel = IdentificationLevel.FromName(ReadAttribute(node, "IDLevel", IdentificationLevel.Full.Name));
				_itemInstance.Cursed = ReadAttributeBool(node, "Cursed");
				_itemInstance.KnownToBeCursed = ReadAttributeBool(node, "KnownToBeCursed", false);
			}
		}

		#endregion

	}


	/** Defines how identified an item instance currently is. */
	public class IdentificationLevel : NamedDataObject
	{
		/** The amount this ID level will effect the price of an item.  1 being no affect, 0.5 being half price */
		public float PriceMod = 1f;
		/** The modifyer used to calculate cost of identifying an item at this ID Level where 1.0 is the items full price. */
		public float IDMod;
		public string Description = "";
		public Color Color;
		/** If a character can use this item at this identification level. */
		public bool CanUse;

		/** Item identifcation is not set yet, but will be when it is first given to a character */
		public static IdentificationLevel Auto = new IdentificationLevel(0, "auto", "all");
		/** Verly little idea about what this item is */
		public static IdentificationLevel None = new IdentificationLevel(1, "none", "nothing", 0.25f, 0.085185f * 1.0f, false);
		/** Have some idea about this items function */
		public static IdentificationLevel Partial = new IdentificationLevel(2, "partial", "a little", 0.50f, 0.085185f * 1.5f);
		/** Know mostly about the item but some details still not understood. */
		public static IdentificationLevel Mostly = new IdentificationLevel(3, "mostly", "most things", 0.75f, 0.085185f * 3.0f);
		/** Show everything */
		public static IdentificationLevel Full = new IdentificationLevel(4, "fully", "everything");

		private IdentificationLevel(int id, string name, string description = "", float priceMod = 1f, float idMod = 1f, bool canUse = true)
			: base(id, name)
		{
			PriceMod = priceMod;
			IDMod = idMod;
			Description = description;
			Color = Colors.ItemUnidentifiedRing.Faded(1f - PriceMod);
			CanUse = canUse;
		}

		/** Returns the next ID level from the current one */
		public IdentificationLevel NextLevel()
		{
			if (this == Auto)
				return Auto;
			if (this == None)
				return Partial;
			if (this == Partial)
				return Mostly;
			if (this == Mostly)
				return Full;
			return Full;
		}

		public static IdentificationLevel FromName(string name)
		{
			name = name.ToLower();
			if (name == Auto.Name)
				return Auto;
			if (name == None.Name)
				return None;
			if (name == Partial.Name)
				return Partial;
			if (name == Mostly.Name)
				return Mostly;
			if (name == Full.Name)
				return Full;
			throw new Exception(string.Format("Invalid ItemIDName {0}.", name));
		}

		/** Returns the name for this item at a given ID level */
		public string ItemName(MDRItem item)
		{
			switch (ID) {
				case 4:
				case 3:
					return item.Name;
				case 2:
					return item.Type.Name;
				case 1:
					return item.Type.TypeClass.Name;
				default:
					return item.Type.TypeClass.Name;
			}
		}


		/** Overload to allow comparison */
		public static bool operator >=(IdentificationLevel s1, IdentificationLevel s2)
		{
			return s1.ID >= s2.ID;
		}

		public static bool operator <=(IdentificationLevel s1, IdentificationLevel s2)
		{
			return s1.ID <= s2.ID;
		}

		public static bool operator >(IdentificationLevel s1, IdentificationLevel s2)
		{
			return s1.ID > s2.ID;
		}

		public static bool operator <(IdentificationLevel s1, IdentificationLevel s2)
		{
			return s1.ID < s2.ID;
		}

	}

}
