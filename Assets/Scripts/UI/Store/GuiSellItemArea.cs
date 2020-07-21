using System;
using Mordor;
using UnityEngine;

namespace UI.Store
{
	/** Area to allow player to sell, id, and uncurse items. */
	public class GuiSellItemArea : GuiPanel
	{
		public MDRItemInstance InspectedItem { get { return ItemSlot.ItemIcon.ItemInstance; } }

		private MDRStore Store;

		internal GuiInspectionSlot ItemSlot;

		private GuiButton sellButton;
		private GuiButton idButton;
		//private GuiButton uncurseButton;
		private GuiContainer buttonsGroup;
		private GuiLabel dropHint;

		private GuiCoinAmount sellPrice;
		private GuiCoinAmount idPrice;

		/** Called when an item is sold. */
		public GuiEvent OnSell;

		public GuiSellItemArea(int width, int height, MDRStore sourceStore) : base(width, height)
		{
			EnableBackground = false;
			Store = sourceStore;

			ItemSlot = new GuiInspectionSlot();
			Add(ItemSlot, 0, 50);
			ItemSlot.Visible = false;

			dropHint = new GuiLabel("Drop Here");
			dropHint.FontSize = 24;
			dropHint.FontColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
			Add(dropHint, 0, 0);

			createButons();
		}

		public override void Update()
		{
			base.Update();

			sellButton.SelfEnabled = canSell();
			idButton.SelfEnabled = canID();
			//uncurseButton.Enabled = canUncurse();

			buttonsGroup.Visible = InspectedItem != null;

			dropHint.Visible = InspectedItem == null;

			sellPrice.Value = (!canSell() ? 0 : Store.BuyPrice(InspectedItem));
			idPrice.Value = (!canID() ? 0 : InspectedItem.IDPrice());
			idPrice.Visible = (idPrice.Value != 0);
		}


		/** IDs the currently inspected item */
		protected void IdItem()
		{			
			if (!canID())
				return;

			int price = InspectedItem.IDPrice();
			if (price > CoM.Party.Gold) {
				Engine.ShowModal("Not Enough Gold", "You do not have enough gold to identify this item.");
				return;
			}
			var previousName = InspectedItem.Name;
			CoM.Party.DebitGold(price, CoM.Party.Selected);
			InspectedItem.IDLevel = InspectedItem.IDLevel.NextLevel();
			Engine.PostNotification("Item " + Util.Colorise(previousName, Color.green) + " was identified as " + Util.Colorise(InspectedItem.Name, Color.green), InspectedItem.Item.Icon);
		}

		/** Uncurses the currently inspected item */
		protected void UncurseItem()
		{
			if (!canUncurse())
				return;

			int price = InspectedItem.UncursePrice();
			if (price > CoM.Party.Gold) {
				Engine.ShowModal("Not Enough Gold", "You do not have enough gold to uncurse this item.");
				return;
			}
			CoM.Party.DebitGold(price, CoM.Party.Selected);
			InspectedItem.Cursed = false;
			InspectedItem.IDLevel = IdentificationLevel.Full;
			Engine.PostNotification("Item " + Util.Colorise(InspectedItem.Name, Color.green) + " has been uncursed.", InspectedItem.Item.Icon);
		}

		/** Returns true if the currently inspected item can be sold */ 
		private bool canSell()
		{
			return (InspectedItem != null);
		}


		/** Returns true if the currently inspected item can be identified further*/ 
		private bool canID()
		{
			return (InspectedItem != null) && (InspectedItem.IDLevel != IdentificationLevel.Full);
		}


		/** Returns true if the currently inspected item can be recharged */ 
		private bool canRecharge()
		{
			return (InspectedItem != null);
		}


		/** Returns true if the currently inspected item can be uncursed */ 
		private bool canUncurse()
		{
			return (InspectedItem != null) && (InspectedItem.KnownToBeCursed && InspectedItem.Cursed);
		}

		/** Sells the currently inspected item */
		private void SellItem()
		{
			if (!canSell())
				return;

			if (ItemSlot.ItemIcon.ItemInstance.Cursed && ItemSlot.SourceIsEquiped()) {				
				Engine.ShowModal("Item is cursed", "This item is equiped and can not be sold until it has been uncursed.");
				return;
			}

			int price = Store.BuyPrice(InspectedItem);
			CoM.Party.Selected.CreditGold(price);
			SoundManager.Play("coin3");
			Store.AddQuanity(InspectedItem.Item);
			ItemSlot.Delete();

			if (OnSell != null)
				OnSell(this, null);
		}

		private void createButons()
		{			
			buttonsGroup = new GuiContainer(Width, 200);
			Add(buttonsGroup, 0, 250);

			// Sell
			var sellFrame = new GuiContainer(300, 50);
			sellFrame.Style = Engine.GetStyleCopy("Frame");
			buttonsGroup.Add(sellFrame, 0, 1);

			sellButton = new GuiButton("Sell", 80);
			sellFrame.Add(sellButton, 10, 0);

			sellPrice = new GuiCoinAmount();
			sellFrame.Add(sellPrice, 150, 0);

			// ID
			var IDFrame = new GuiContainer(300, 50);
			IDFrame.Style = Engine.GetStyleCopy("Frame");
			buttonsGroup.Add(IDFrame, 0, 1 + 48);

			idButton = new GuiButton("ID", 80);
			IDFrame.Add(idButton, 10, 0);

			idPrice = new GuiCoinAmount();
			IDFrame.Add(idPrice, 150, 0);

			sellButton.OnMouseClicked += delegate {
				SellItem();
			};

			idButton.OnMouseClicked += delegate {
				IdItem();
			};

			buttonsGroup.FitToChildren();
						
		}

	}
}