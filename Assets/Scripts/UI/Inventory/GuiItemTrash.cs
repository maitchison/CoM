using System;
using UnityEngine;
using UI.Generic;

namespace UI
{
	/** Handles drops of items off any UI componenets.  Will ask, then trash the item */
	public class GuiItemTrash : DDSlot
	{
		private GuiItem trashedItem;

		private Sprite slotGraphic;

		public GuiItemTrash(int x, int y)
			: base(x, y)
		{
			slotGraphic = CoM.Instance.IconSprites["Slot_Trash"];
			EnableBackground = false;
		}


		public override void DrawContents()
		{
			SmartUI.Draw(Frame, slotGraphic);
		}

		public override bool CanReceive(GuiComponent value)
		{
			return (value is GuiItem);
		}

		public override bool Transact(IDragDrop source)
		{
			if ((source == null) || !(source is GuiItemSlot))
				return false;

			var modal = new ModalDecisionState("Are you sure?", "Do you want to destroy " + source.DDContent + "?\nThis item will be permanently destroyed.");

			modal.OnNo += delegate {
				// nothing to do.
			};

			modal.OnYes += delegate {
				(source as GuiItemSlot).DataLink.ItemInstance = null;
			};

			Engine.PushState(modal);

			return false;
		}

	}

}

