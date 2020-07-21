using Mordor;
using UnityEngine;

namespace UI.Store
{
	/** Display a listing in the store */
	public class GuiStoreListing : GuiContainer
	{
		public int ItemID;

		public bool Selected { get { return (parentStore != null) ? (parentStore.StoreSelectedItemID == this.ItemID) : false; } }

		private GuiLabel nameLabel;
		private GuiCoinAmount coinsAmount;
		private GuiItemSlot itemContainer;

		/** Position index, 0 being the first, 1 being the second etc */
		public int PositionIndex;

		private MDRItem item;
		private MDRItemSlot link;
		private int price;

		/** Our parent listing, if we belong to one */
		private GuiStore parentStore;

		public GuiStoreListing(GuiStore parentStore, int x, int y, int itemID, int price, int quantity = 1)
			: base(300, 50)
		{
			X = x;
			Y = y;

			this.ItemID = itemID;
			this.Style = Engine.GetStyleCopy("PanelSquare");
			this.price = price;
			this.Style.padding.right = 10;
			this.parentStore = parentStore;
			this.DisableChildInteraction = true;

			link = new MDRItemSlot();
			link.Quantity = quantity;

			itemContainer = new GuiItemSlot(2, 2, link);
			itemContainer.Locked = true;
			itemContainer.ShowToolTipOnHover = false;
			Add(itemContainer);

			nameLabel = new GuiLabel(52, 14, "", 220);
			Add(nameLabel);

			coinsAmount = new GuiCoinAmount();
			Add(coinsAmount, -10, -10, true);

			CacheMode = CacheMode.Solid;

			Refresh();
		}

		private Color getBackgroundColor()
		{
			bool canEquip = (item.GetCanNotEquipItemReason(CoM.SelectedCharacter) == "");

			var backgroundColor = ((PositionIndex % 2) == 0) ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

			if (!canEquip)
				backgroundColor = Color.Lerp(backgroundColor, Color.red, 0.10f);			

			if (ItemID < 0)
				backgroundColor = new Color(0.1f, 0.1f, 0.1f);

			if (Selected)
				backgroundColor = Color.Lerp(backgroundColor, new Color(0.5f, 0.5f, 0.3f), 0.5f);

			return backgroundColor;
		}

		/** Updates the item listings icon, text etc */
		private void Refresh()
		{
			item = CoM.Items.ByID(ItemID);
			nameLabel.Caption = item.ColorisedName + ((link.Quantity > 1) ? (" (x" + link.Quantity + ")") : "");

			coinsAmount.Value = price;
			link.ItemInstance = MDRItemInstance.Create(item);
			Invalidate();
		}

		/** Update selected item */
		public override void DoClick()
		{
			base.DoClick();
			if (parentStore != null) {
				parentStore.StoreSelectedItemID = this.ItemID;
			}
		}

		private bool oldSelected = false;
		private int oldItemID = -1;

		public override void Update()
		{
			// check for invalidation

			if (oldSelected != Selected || oldItemID != ItemID)
				Invalidate();

			oldSelected = Selected;
			oldItemID = ItemID;

			Color = getBackgroundColor();

			base.Update();
		}

		public override void DrawContents()
		{
			if (ItemID >= 0)
				base.DrawContents();
		}
	}

}