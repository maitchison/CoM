
using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Data;



namespace Mordor
{
	/** Contains the data for which items are currently being held in the store */
	[DataObject("Store")]
	public class MDRStore : DataObject
	{
		/** Quanity of each item type (by itemID) */
		protected Dictionary<int,int> itemQuantity;

		/** Occurs when contents of store changes. */
		public event GameTrigger OnInventoryChanged;

		/** Creates a new store */
		public MDRStore()
		{
			itemQuantity = new Dictionary<int, int>();
		}

		public int GetQuantity(MDRItem item)
		{
			if (itemQuantity.ContainsKey(item.ID))
				return itemQuantity[item.ID];
			else
				return 0;
		}

		private void doInventoryChanged()
		{
			if (OnInventoryChanged != null)
				OnInventoryChanged();
		}

		public void SetQuanity(MDRItem item, int value)
		{
			itemQuantity[item.ID] = value;
			doInventoryChanged();
		}

		public void SubtractQuantity(MDRItem item, int delta = 1)
		{			
			AddQuanity(item, -delta);
		}

		public void AddQuanity(MDRItem item, int delta = 1)
		{
			itemQuantity[item.ID] = GetQuantity(item) + delta;
			doInventoryChanged();

			// Auto id anything that goes into the store.
			CoM.GameStats.RegisterItemIdentified(item, IdentificationLevel.Full);
		}

		/** Adds one of each item to store.  For debuging */
		public void AddOneOfEach()
		{
			for (int lp = 0; lp < CoM.Items.Count; lp++) {
				AddQuanity(CoM.Items[lp]);
			}
		}

		/** Removes all items in the store */
		public void Clear()
		{
			itemQuantity.Clear();
			doInventoryChanged();
		}

		/** Adds item of given name */
		private void AddItem(string itemName, int count = 1)
		{
			var item = CoM.Items[itemName];
			if (item == null) {
				Trace.LogWarning("No item named '{0}'", itemName);
				return;
			}
			AddQuanity(item, count);
		}

		/** Clears the store, and adds some default items */
		public void SetDefault()
		{
			Clear();

			foreach (MDRItem item in CoM.Items) {
				if (item.BaseStoreQuantity >= 1)
					AddItem(item.Name, item.BaseStoreQuantity);
			}
		}

		/** Returns the price this store will sell a given item for */
		public int SellPrice(MDRItem item)
		{
			float factor = 1 + (float)Math.Log(GetQuantity(item));
			return (int)(item.Value / factor);
		}

		/** Returns the price this store will buy a given item for */
		public int BuyPrice(MDRItemInstance instance)
		{
			int quantity = GetQuantity(instance.Item);
			float quantityFactor = 1f / ((quantity == 0) ? 1 : 1 + (float)Math.Log(quantity));
			float idFactor = instance.IDLevel.PriceMod;
			if (instance.KnownToBeCursed)
				return 1;
			else
				return (int)(instance.Item.Value / 3 * quantityFactor * idFactor);
		}

		/** Purchase item, gives to currently selected party memeber. */
		public bool PurchaseItem(MDRItem item)
		{
			MDRCharacter buyer = CoM.Party.Selected;

			if (buyer == null)
				return false;
			
			if (CoM.Party.Gold < SellPrice(item))
				return false;

			// do the transaction
			if (buyer.GiveItem(MDRItemInstance.Create(item))) {
				CoM.Party.DebitGold(SellPrice(item), buyer);
				SubtractQuantity(item, 1);
				return true;
			} else {
				CoM.PostMessage(buyer + " does not have enough space for " + item);
				return false;
			}
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			foreach (int key in itemQuantity.Keys) {
				int quantity = itemQuantity[key];
				if (quantity == 0)
					continue;
				XElement quanitiyNode = new XElement("Item");
				quanitiyNode.SetAttributeValue("id", key);
				quanitiyNode.Value = quantity.ToString();
				node.Add(quanitiyNode);
			}
		}

		public override void ReadNode(XElement node)
		{
			Clear();
			foreach (XElement childNode in node.Elements()) {
				int newID = ReadAttributeInt(childNode, "id");
				int newValue = int.Parse(childNode.Value);
				var item = CoM.Items.ByID(newID);
				if (item == null) {
					Trace.LogWarning("Data Error [Store]: Store references an item that doesn't exist.  Item ID {0}.  This item will be ignored.", newID);
					continue;
				}
				AddQuanity(item, newValue);
			}
		}

		#endregion
	}
}

