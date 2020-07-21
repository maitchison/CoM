/**
 * 
 * Fixup:
 * sparse saving on inventory (makes it look cleaner for editing, but required storing an index id
 * 
 */


using System.Collections;
using System.Xml.Linq;

using Data;

namespace Mordor
{
	/** List of equiped items for character */
	public class MDREquiped : MDRInventory
	{
		private const int CHARACTER_EQUIP_SLOTS = 14;

		public MDRItemSlot Neck { get { return this[0]; } }

		public MDRItemSlot Head { get { return this[1]; } }

		public MDRItemSlot Back { get { return this[2]; } }

		public MDRItemSlot LeftHand { get { return this[3]; } }

		public MDRItemSlot Body { get { return this[4]; } }

		public MDRItemSlot RightHand { get { return this[5]; } }

		public MDRItemSlot Hands { get { return this[6]; } }

		public MDRItemSlot Waist { get { return this[7]; } }

		public MDRItemSlot Arm { get { return this[8]; } }

		public MDRItemSlot Finger1 { get { return this[9]; } }

		public MDRItemSlot Feet { get { return this[10]; } }

		public MDRItemSlot Finger2 { get { return this[11]; } }

		public MDRItemSlot Misc1 { get { return this[12]; } }

		public MDRItemSlot Misc2 { get { return this[13]; } }

		public MDRItemSlot Misc3 { get { return this[14]; } }

		/** returns the players current weapon, or hands if no weapon is defined */
		public MDRItem Weapon {
			get {
				if (!RightHand.IsEmpty && (RightHand.ItemInstance.Item.isWeapon))
					return RightHand.ItemInstance.Item;
				return (CoM.Items[0]); // first item should be hands
			}
		}

		private MDRCharacter _character;

		public MDREquiped()
			: base(CHARACTER_EQUIP_SLOTS)
		{
			// add hooks for when equiped changes
			for (int lp = 0; lp < CHARACTER_EQUIP_SLOTS; lp++) {
				Slot[lp].OnBeforeSetItem += DoItemCheck;
				Slot[lp].OnAfterSetItem += DoEquipmentChanged;
			}

			// add restrictions
			Slot[0].Restriction = ItemLocation.Neck;
			Slot[1].Restriction = ItemLocation.Head;
			Slot[2].Restriction = ItemLocation.Back;
			Slot[3].Restriction = ItemLocation.LeftHand;
			Slot[4].Restriction = ItemLocation.Body;
			Slot[5].Restriction = ItemLocation.RightHand;
			Slot[6].Restriction = ItemLocation.Hands;
			Slot[7].Restriction = ItemLocation.Waist;
			Slot[8].Restriction = ItemLocation.Feet;
			Slot[9].Restriction = ItemLocation.Finger;
			Slot[10].Restriction = ItemLocation.Finger;	
			Slot[11].Restriction = ItemLocation.Misc;	
			Slot[12].Restriction = ItemLocation.Misc;	
			Slot[13].Restriction = ItemLocation.Misc;	

			Slot[5].PairedSlot = Slot[3];
			Slot[3].PairedSlot = Slot[5];
		}

		public static MDREquiped Create(MDRCharacter character)
		{
			MDREquiped result = new MDREquiped();
			result._character = character;
			return result;
		}

		/** Update players stats when equipment changes */
		protected void DoEquipmentChanged(MDRItemSlot source, MDRItemInstance item)
		{
			// if paired item was two handed then unequip paired item
			if ((item != null) && (source.PairedSlot != null) && (!source.PairedSlot.IsEmpty) && (source.PairedSlot.ItemInstance.Item.Hands == 2)) {
				_character.UnequipItem(source.PairedSlot);
			}

			// if we are two handed unequip paired item
			if ((source.PairedSlot != null) && !(item == null) && (item.Item.Hands == 2)) {
				_character.UnequipItem(source.PairedSlot);
			}

			_character.ApplyChanges();
		}

		/** Makes sure player can equip given item */
		protected bool DoItemCheck(object source, MDRItemInstance item)
		{
			if (item == null)
				return true;
			if (item.Item == null)
				return true;
			return item.CanBeEquipedBy(_character);
		}

		/** Adds given item to the correct inventory slot (of the right type).  Returns the slot used if sucessful */
		override public MDRItemSlot ReceiveItem(MDRItemInstance item)
		{
			MDRItemSlot freeSlot = NextFreeSlot();
			if (freeSlot != null) {
				freeSlot.ItemInstance = item;
			} 
			return freeSlot;
		}


	}

	/** Stores characters items */
	public class MDRInventory : DataObject
	{
		protected MDRItemSlot[] Slot;

		public GameTrigger OnInventoryChanged;

		public MDRInventory(int maxSlots, MDRCharacter character = null)
		{
			Slot = new MDRItemSlot[maxSlots];
			for (int lp = 0; lp < maxSlots; lp++) {
				Slot[lp] = new MDRItemSlot();
				Slot[lp].Character = character;
				Slot[lp].OnAfterSetItem += delegate {
					if (OnInventoryChanged != null)
						OnInventoryChanged();
				};
			}
		}

		public int Count {
			get { return Slot.Length; }
		}

		/** Access to slots */
		public MDRItemSlot this [int index] {
			get { return Slot[index]; }
		}

		/** Enumerator for data */
		public IEnumerator GetEnumerator()
		{
			return Slot.GetEnumerator();
		}

		/** Returns the next free slot, optionaly of the given location */ 
		public MDRItemSlot NextFreeSlot(ItemLocation location = ItemLocation.Any)
		{
			for (int lp = 0; lp < Count; lp++)
				if ((Slot[lp].ItemInstance == null) && (Slot[lp].Restriction == location || location == ItemLocation.Any || Slot[lp].Restriction == ItemLocation.Any))
					return Slot[lp];
			return null;
		}

		/** Returns the next available slot of the given location that contains either no item or a removable item. */ 
		public MDRItemSlot AvailableSlot(ItemLocation location)
		{
			for (int lp = 0; lp < Count; lp++)
				if (((Slot[lp].ItemInstance == null) || (!Slot[lp].ItemInstance.Cursed)) && (Slot[lp].Restriction == location || location == ItemLocation.Any || Slot[lp].Restriction == ItemLocation.Any))
					return Slot[lp];
			return null;
		}

		/** Adds given item to inventory.  Returns the slot used if sucessful */
		virtual public MDRItemSlot ReceiveItem(MDRItemInstance item)
		{
			MDRItemSlot freeSlot = NextFreeSlot();
			if (freeSlot != null) {
				freeSlot.ItemInstance = item;
			} 
			if (OnInventoryChanged != null)
				OnInventoryChanged();
			return freeSlot;
		}

		#region implemented members of DataObject

		public override void WriteNode(XElement node)
		{
			foreach (MDRItemSlot slot in Slot)
				WriteValue(node, "Slot", slot);
		}

		public override void ReadNode(XElement node)
		{
			if (node == null)
				return;
			int index = 0;
			foreach (XElement subNode in node.Elements()) {
				Slot[index].ReadNode(subNode);
				index++;
			}
		}

		#endregion
	}
}
