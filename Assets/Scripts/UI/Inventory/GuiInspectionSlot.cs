using System;

using UI;
using Mordor;

namespace UI
{
	/** A special slot used to inspect items.  Items dragged to this slot will not be transfered, just refered. */
	public class GuiInspectionSlot : GuiItemSlot
	{
		public GuiItemSlot OrigionalSource;

		public GuiInspectionSlot()
			: base(0, 0, new MDRItemSlot())
		{
			ShowToolTipOnHover = false;
		}

		/** Overrides transact to make a copy of the dragged in item, but leave the origional intact */
		public override bool Transact(IDragDrop source)
		{
			if (!(source is GuiItemSlot))
				return false;

			var item = source.DDContent;

			if (item == null)
				return false;

			if (OrigionalSource != null) {
				OrigionalSource.Locked = false;
				if (OrigionalSource.DDContent != null)
					OrigionalSource.DDContent.SelfEnabled = true;
			}

			OrigionalSource = (GuiItemSlot)source;
			DDContent = (GuiComponent)item.Clone();
		
			OrigionalSource.Locked = true;
			if (OrigionalSource.DDContent != null)
				OrigionalSource.DDContent.SelfEnabled = false;

			return true;
		}

		/** Disable dragging from this reference slot */
		public override bool CanSend(IDragDrop destination)
		{
			return false;
		}

		/** Clears inspection slots contents and unlocks origional. */
		public void Restore()
		{
			if ((OrigionalSource != null) && (!OrigionalSource.IsEmpty)) {
				OrigionalSource.DDContent.SelfEnabled = true;
				DDContent = new GuiItem();
			}
		}

		/** Deletes the origional copy of the currently selected item */
		public void Delete()
		{
			if (!IsEmpty) {						
				if (OrigionalSource != null) {
					OrigionalSource.DDContent = new GuiItem();
				}
				DDContent = new GuiItem();
			}
		}

		/** Returns true if source item is currently equiped */
		public bool SourceIsEquiped()
		{
			if ((OrigionalSource == null) || (OrigionalSource.DataLink == null))
				return false;
			return (OrigionalSource.DataLink.IsEquipSlot);

		}
	}
}

