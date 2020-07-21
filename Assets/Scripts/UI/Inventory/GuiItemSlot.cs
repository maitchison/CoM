
using UnityEngine;

using Mordor;

using UI.Generic;

namespace UI
{


	/** UI componenet that holds an item.  The items in these containers can be dragged around. */
	public class GuiItemSlot : DDSlot
	{
		private static GuiItemToolTip toolTip;

		protected static Sprite IDPartial;
		protected static Sprite IDNone;

		/** Our contents cast as a GUIItem */
		public GuiItem ItemIcon { get { return (DDContent as GuiItem); } set { DDContent = value; } }

		/** Link to the data we control */
		private MDRItemSlot _dataLink;

		public MDRItemSlot DataLink { get { return _dataLink; } set { setDataLink(value); } }

		protected MDRItemInstance itemInstance { get { return DataLink == null ? null : DataLink.ItemInstance; } }

		/** Indicates that the items contents have changed, but have not been applied yet. */ 
		private bool _dirty;

		/** Create a new item */
		public GuiItemSlot(int x, int y, MDRItemSlot dataLink = null)
			: base(x, y)
		{
			IDPartial = IDPartial ?? ResourceManager.GetSprite("Icons/SlotIDPartial");
			IDNone = IDNone ?? ResourceManager.GetSprite("Icons/SlotIDNone");
			ItemIcon = new GuiItem();
			DataLink = dataLink;
			ShowToolTipOnHover = true;
			OnDoubleClicked += delegate {
				if (!Locked)
					UseOrEquipItem();
			};
		}

		/** Notice when our linked item changes. */
		private void monitorDataLinkChanges(MDRItemSlot source, MDRItemInstance item)
		{
			_dirty = true;
		}

		/** Sets the data link, monitors for changes. */
		private void setDataLink(MDRItemSlot newDataLink)
		{
			if (_dataLink != null)
				_dataLink.OnAfterSetItem -= monitorDataLinkChanges;
			_dataLink = newDataLink;
			if (_dataLink != null)
				_dataLink.OnAfterSetItem += monitorDataLinkChanges;

			_dirty = true;
		}

		/** Returns if this slots contains a cursed item or not. */
		protected bool containsCursedItem()
		{
			if ((DataLink == null) || (DataLink.IsEmpty))
				return false;
			return DataLink.ItemInstance.Cursed;
		}

		public void Apply()
		{
			RingColor = Color.clear;
			IndicatorColor = Color.clear;

			if (ItemIcon == null)
				return;

			if ((DataLink == null) || (DataLink.ItemInstance == null))
				ItemIcon.ItemInstance = null;
			else {
				ItemIcon.ItemInstance = DataLink.ItemInstance;
			}
				
			if (DataLink == null)
				return;

			updateItemDisplay();

			// show restricted slots when dragging an item
			if ((ItemRestriction != ItemLocation.Any) && (Engine.DragDrop.DDContent != null) && (Engine.DragDrop.DDContent is GuiItem)) {
				GuiItem item = (Engine.DragDrop.DDContent as GuiItem);
				if (ItemRestriction == item.ItemInstance.Item.Type.TypeClass.Location)
					RingColor = item.ItemInstance.CanBeEquipedBy(CoM.Party.Selected) ? Colors.ItemCanEquipRing : Colors.ItemCanNotEquipedRing;
			}
			_dirty = false;
		}

		public override void Update()
		{
			base.Update();
			Apply();
		}

		/** Override can send to notify player the item is cursed */
		public override bool CanSend(IDragDrop destination)
		{
			if ((DataLink != null) && (!(destination is GuiInspectionSlot)) && DataLink.IsEquipSlot && containsCursedItem()) {
				CoM.PostMessage("This item is cursed!");
				return false;
			}
			return true;
		}

		protected override bool showToolTip()
		{
			if (toolTip == null) {
				toolTip = new GuiItemToolTip();
				gameState.Add(toolTip);
			}
				
			toolTip.ItemInstance = DataLink == null ? null : DataLink.ItemInstance;
			toolTip.PositionToMouse();

			return true;
		}
				
		// ----------------------------------------------------------------------------------
		// ----------------------------------------------------------------------------------

		/** Sets the item color, indicator color, and overlay based on this items characteristics. */
		private void updateItemDisplay()
		{
			IndicatorColor = Color.clear;

			ItemIcon.Color = Color.white;
			ItemIcon.ColorTransform = ColorTransform.Identity;

			OverlaySprite = null;

			if (IsEmpty)
				return;
			
			bool isCursed = DataLink.ItemInstance.KnownToBeCursed;
			IdentificationLevel idLevel = DataLink.ItemInstance.IDLevel;

			// Indicator color:
			var backgroundColor = isCursed ? Color.red : DataLink.ItemInstance.Item.QualityBackgroundColor;
			IndicatorColor = backgroundColor.Faded(0.33f);

			// Item color:
			MDRCharacter character = DataLink.Character;
			if (idLevel.CanUse && character != null) {				
				bool usable = DataLink.ItemInstance.Item.Usable;
				bool canUseOrEquip = usable ? DataLink.ItemInstance.CanBeUsedBy(character) : DataLink.ItemInstance.CanBeEquipedBy(character);

				if (!canUseOrEquip) {
					ItemIcon.Color = new Color(0.5f, 0.25f, 0.25f);
					ItemIcon.ColorTransform = ColorTransform.Saturation(0.5f);
				}				
			}

			// Item ID level
			if (idLevel == IdentificationLevel.Full)
				OverlaySprite = null;
			else
				OverlaySprite = (idLevel.CanUse) ? IDPartial : IDNone;					
		}

		// ----------------------------------------------------------------------------------
		// ----------------------------------------------------------------------------------


		/** Draw an item type indicator on slots that are restricted by type */
		public override void DrawContents()
		{
			// Sometimes the contents is changed but no update was called inbetween so we make sure the changes
			// are in sync now.
			if (_dirty)
				Apply();

			if (DataLink != null) {
				if ((ItemRestriction != ItemLocation.Any) && DataLink.IsEmpty) {
					int iconID = MDRItem.GetSpriteForItemLocation(ItemRestriction);
					Sprite icon = (iconID >= 0) ? CoM.Instance.ItemIconSprites[iconID] : null;
					if (icon) {
						var dp = DrawParameters.BlackAndWhite;
						SmartUI.Color = new Color(0.35f, 0.35f, 0.35f, 0.75f);
						SmartUI.Draw(6, 6, icon, dp);
						SmartUI.Color = Color.white;
					}
				}
				
				// draw paired item if required.
				if (DataLink.PairedSlot != null) {
					if ((!DataLink.PairedSlot.IsEmpty) && (DataLink.PairedSlot.ItemInstance.Item.Hands == 2)) {
						var dp = DrawParameters.BlackAndWhite;
						SmartUI.DrawFillRect(new Rect(3, 3, Width - 6, Height - 6), new Color(0.1f, 0.1f, 0.1f, 0.9f));
						SmartUI.Color = new Color(0.35f, 0.35f, 0.35f, 0.75f);
						SmartUI.Draw(6, 6, DataLink.PairedSlot.ItemInstance.Item.Icon, dp);
						SmartUI.Color = Color.white;
					}
				} 
			}

			base.DrawContents();
		}

		public override bool IsEmpty { get { return (DataLink == null) || (DataLink.ItemInstance == null); } }

		/** 
		 * Attempts to use or equip the item stored in this container.  
		 * Equipable items will be equiped.  Usable items will be used 
		 */
		protected void UseOrEquipItem()
		{
			if (DataLink.IsEmpty)
				return;

			MDRItemInstance itemInstance = DataLink.ItemInstance;

			//stub: slots should know the character they're associated with */
			MDRCharacter character = CoM.Party.Selected;

			if (!itemInstance.IDLevel.CanUse) {
				CoM.PostMessage("{0} must be more identified before it can be used.", CoM.Format(itemInstance));
				return;
			}

			if (itemInstance.Item.Usable) {
				// first can this item be activated?
				string reason = itemInstance.GetCanNotUseReason(character);
				if (reason == "") {
					if (itemInstance.RemainingCharges <= 0)
						CoM.PostMessage("Item is out of charges.");
					else
						itemInstance.Use(character);
				} else {
					CoM.PostMessage(reason);
				}
			} else {
				// otherwise try equiping it
				string reason = itemInstance.GetCanNotEquipReason(character);
				if (reason == "") {
					character.EquipItem(DataLink);
				} else {
					CoM.PostMessage(reason);
				}
			}
		}

		/** Called when this containers content changes */
		public override void DDContentChanged()
		{
			if (DataLink != null)
				DataLink.ItemInstance = (ItemIcon != null) ? ItemIcon.ItemInstance : null;
		}

		/** Returns the types of items this container can accept */
		public ItemLocation ItemRestriction {
			get {
				return DataLink != null ? DataLink.Restriction : ItemLocation.Any;
			}
		}

		/** Make sure we can only accept items */
		public override bool CanReceive(GuiComponent value)
		{
			if (DataLink == null)
				return true;

			if (value == null)
				return false;

			if (!Enabled)
				return false;

			if (containsCursedItem() && DataLink.IsEquipSlot)
				return false;

			if (value is GuiItem) {
				if ((value as GuiItem).ItemInstance == null)
					return true;
				return DataLink.CanAccept((value as GuiItem).ItemInstance);
			}

			return false;		
		}

		/** Free up any hooks on datalink */
		public override void Destroy()
		{
			DataLink = null;
			base.Destroy();
		}


		public override string ToString()
		{
			return string.Format("[GuiItemContainer: ItemIcon={0}, ItemRestriction={1}]", ItemIcon, ItemRestriction);
		}


	}
}