
using System;
using UnityEngine;

using Mordor;

namespace UI.Store
{

	public enum StoreMode
	{
		Buy,
		Sell
	}

	/** Gui for purchasing and selling items */
	public class GuiStore : GuiWindow
	{
		public MDRStore Store;

		private GuiContainer buyItemArea;
		private GuiSellItemArea sellItemArea;

		private GuiScrollableArea itemListingScrollArea;

		private GuiButton buyButton;
		private GuiItemToolTip SelectedItemInfo;
		private GuiToggleButton filterItemsToggle;

		private GuiLabel notEnoughGold;

		private GuiImage itemInfoBackground;

		private GuiRadioButtonGroup modeButtons;

		/** Used to show selling mode when user is dragging an item. */
		public bool AutoShowSell {
			get { return _autoShowSell; }
			set {
				var oldMode = Mode;
				_autoShowSell = value;
				if (Mode != oldMode)
					updateStoreMode();
			}
		}


		/** Enables or disables the filtering of non usable items */
		public bool ShowOnlyUsableItems {
			get { return _showOnlyUsableItems; }
			set {
				_showOnlyUsableItems = value;
				createStoreListings();
			}
		}

		/** Switches between buy and sell modes. */
		public StoreMode Mode {
			get { return AutoShowSell ? StoreMode.Sell : _mode; }
			set {
				if (_mode == value)
					return;
				_mode = value;
				modeButtons.SelectedIndex = (int)value;
				updateStoreMode();
			}
		}

		private StoreMode _mode = StoreMode.Buy;

		private bool _showOnlyUsableItems;
		private bool _autoShowSell;

		/** ID of the currently selected item in the store listings */
		public int StoreSelectedItemID;

		/** Create the store ui */
		//todo: remove sizes
		public GuiStore(MDRStore store, int width = 800, int height = 600)
			: base(width, height)
		{
			int splitWidth = (width / 2) + 50;

			const int HEADER_HEIGHT = 50;

			WindowStyle = GuiWindowStyle.ThinTransparent;
			StoreSelectedItemID = -1;
			Store = store;
			CanReceiveFocus = true;
			DragDropEnabled = true;

			// -----------------------------------------
			// Main areas
										
			var mainArea = new GuiContainer(0, (int)ContentsBounds.height - HEADER_HEIGHT);
			mainArea.Align = GuiAlignment.Bottom;

			// -----------------------------------------
			// Header

			var headerArea = new GuiContainer((int)ContentsBounds.width, HEADER_HEIGHT);
			headerArea.EnableBackground = true;
			headerArea.Style = Engine.GetStyleCopy("Frame");
			headerArea.Y -= 4;
			headerArea.X -= 4;
			headerArea.Width += 8;

			modeButtons = new GuiRadioButtonGroup();
			modeButtons.AddItem("Buy");
			modeButtons.AddItem("Sell");
			modeButtons.ButtonSize = new Vector2(120, 30);
			modeButtons.ButtonSpacing = 50;
			modeButtons.EnableBackground = false;
			headerArea.Add(modeButtons, 0, 0, true);		

			modeButtons.OnValueChanged += delegate {
				_mode = (StoreMode)modeButtons.SelectedIndex;
				updateStoreMode();
			};

			Add(headerArea);
			Add(mainArea);

			// -----------------------------------------
			// Item Info Area

			GuiPanel itemInfoPanel = new GuiPanel((int)ContentsBounds.width - splitWidth);
			itemInfoPanel.Align = GuiAlignment.Right;
			itemInfoPanel.EnableBackground = false;
			mainArea.Add(itemInfoPanel, -1, 1, true);

			itemInfoBackground = new GuiImage(0, 0, ResourceManager.GetSprite("Gui/InnerWindow"));
			itemInfoBackground.Align = GuiAlignment.Full;
			itemInfoBackground.Color = Colors.StoreItemInfoBackgroundColor;
			itemInfoPanel.Add(itemInfoBackground);

			SelectedItemInfo = new GuiItemToolTip();
			SelectedItemInfo.EnableBackground = false; 
			SelectedItemInfo.Align = GuiAlignment.Full;
			SelectedItemInfo.ShowAllInfo = true;
			itemInfoPanel.Add(SelectedItemInfo);

			// -----------------------------------------
			// Item Buy Area

			buyItemArea = new GuiContainer(splitWidth, (int)ContentsBounds.height);
			buyItemArea.Align = GuiAlignment.Left;
			mainArea.Add(buyItemArea);

			itemListingScrollArea = new GuiScrollableArea(buyItemArea.Width, buyItemArea.Height, ScrollMode.VerticalOnly);
			buyItemArea.Add(itemListingScrollArea);

			filterItemsToggle = new GuiToggleButton();
			filterItemsToggle.OnValueChanged += delegate {
				ShowOnlyUsableItems = filterItemsToggle.Value;
			};
			mainArea.Add(filterItemsToggle, -10, -10);
			filterItemsToggle.Visible = false;

			buyButton = new GuiButton("Buy");
			buyButton.OnMouseClicked += DoBuy;
			itemInfoPanel.Add(buyButton, 0, -30);

			notEnoughGold = new GuiLabel("No enough coins");
			notEnoughGold.FontColor = new Color(0.5f, 0.5f, 0.5f, 0.9f);
			notEnoughGold.Visible = false;
			itemInfoPanel.Add(notEnoughGold, 0, -56);

			// -----------------------------------------
			// item Sell area

			sellItemArea = new GuiSellItemArea((int)mainArea.ContentsBounds.width - splitWidth, (int)mainArea.ContentsBounds.height, store);
			sellItemArea.Align = GuiAlignment.Right;
			sellItemArea.OnSell += delegate {
				Mode = StoreMode.Buy;
			};
			mainArea.Add(sellItemArea);

			// -----------------------------------------

			CoM.Party.OnSelectedChanged += createStoreListings;
			store.OnInventoryChanged += createStoreListings;

			updateStoreMode();
			createStoreListings();
		}

		protected void updateStoreMode()
		{
			buyButton.Visible = Mode == StoreMode.Buy;
			sellItemArea.Visible = Mode == StoreMode.Sell;
		}

		/** Arrow keys to change selection */
		protected override void ProcessKeyboardInput()
		{
			//niy
		}

		private MDRItem selectedItem {
			get {
				int itemID = StoreSelectedItemID;
				if (itemID <= 0)
					return null;
				return CoM.Items.ByID(itemID); 
			}
		}

		private int selectedItemPrice {
			get {
				return selectedItem == null ? 0 : Store.SellPrice(selectedItem);
			}
		}

		/** Process purchasing of an item */
		public void DoBuy(object source, EventArgs e)
		{			
			MDRItem item = selectedItem;

			var didBuy = Store.PurchaseItem(item);

			if (didBuy)
				SoundManager.Play("coin");

			// If the item we had selected is no longer avalible deselect it
			if (Store.GetQuantity(item) <= 0)
				StoreSelectedItemID = -1;			
		}

		/** Update selected item etc */
		public override void Update()
		{
			base.Update();

			buyButton.SelfEnabled = selectedItem != null && CoM.Party.Gold >= selectedItemPrice;
			notEnoughGold.Visible = CoM.Party.Gold < selectedItemPrice;

			// Switch to sell mode as soon as we start dragging
			AutoShowSell = (Engine.DragDrop.IsDragging);				

			switch (Mode) {
				case StoreMode.Buy:
					SelectedItemInfo.ItemInstance = (StoreSelectedItemID == -1) ? null : MDRItemInstance.Create(CoM.Items.ByID(StoreSelectedItemID));
					buyItemArea.CompositeColorTransform = ColorTransform.Identity;
					break;
				case StoreMode.Sell:
					SelectedItemInfo.ItemInstance = sellItemArea.InspectedItem;
					buyItemArea.CompositeColorTransform = ColorTransform.BlackAndWhite;
					break;
			}

			buyItemArea.SelfEnabled = (Mode == StoreMode.Buy);
			SelectedItemInfo.ShowAllInfo = (Mode == StoreMode.Buy);

			// Highlight when user is dragging an item to the sell area.
			itemInfoBackground.Color = Colors.StoreItemInfoBackgroundColor;
			if (Feedback == DDFeedback.Accept)
				itemInfoBackground.Color = Color.Lerp(itemInfoBackground.Color, Color.white, 0.5f);
		}

		/** Creates the listings for the store */
		protected void createStoreListings()
		{
			itemListingScrollArea.Clear();
			int yPos = 0;
			int position = 0;
			foreach (MDRItem item in CoM.Items) {
				if (Store.GetQuantity(item) <= 0)
					continue;
				if (item.CurseType != ItemCurseType.None)
					continue;
				if (ShowOnlyUsableItems && (item.GetCanNotEquipItemReason(CoM.Party.Selected) != ""))
					continue;
				GuiStoreListing listing = new GuiStoreListing(this, 0, 1 + yPos, item.ID, Store.SellPrice(item), Store.GetQuantity(item));
				listing.PositionIndex = position++;
				listing.Width = (int)itemListingScrollArea.ContentsBounds.width;
				itemListingScrollArea.Add(listing);
				listing.Update();
				yPos += listing.Height;
				if (StoreSelectedItemID == -1)
					StoreSelectedItemID = listing.ItemID;
			}
			itemListingScrollArea.ContentsScrollRect.height = yPos;
		}

		/** Override transaction to switch the store to sell mode, and transfer a copy to the inspection slot */
		override public bool Transact(IDragDrop source)
		{
			Mode = StoreMode.Sell;
			return sellItemArea.ItemSlot.Transact(source);
		}

		/** Accept items, and auto set the mode to sell on drag over. */
		override public bool CanReceive(GuiComponent value)
		{
			if (value is GuiItem) {				
				return true;
			}
			return false;
		}

		/** We don't actually want our DDContent set so we override this.  The item will be placed via "transact". */
		override protected void SetDDContent(GuiComponent value)
		{
			// nothing.
		}

		public override void Destroy()
		{
			CoM.Party.OnSelectedChanged -= createStoreListings;
			Store.OnInventoryChanged -= createStoreListings;
			base.Destroy();
		}
	}
}