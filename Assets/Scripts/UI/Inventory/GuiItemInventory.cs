
using System;

using Mordor;

namespace UI
{
	/** Displays and manages a characters inventory */
	public class GuiItemInventory : GuiWindow
	{
		private const int SLOT_WIDTH = 42;
		private const int SLOT_HEIGHT = 42;

		private const int COLUMNS = 6;

		/** The inventory we will be displaying */
		public MDRInventory Source { get { return _source; } set { setSource(value); } }

		private MDRInventory _source;

		private GuiScrollableArea inventoryScrollArea;

		/** Inventory slots */
		private GuiItemSlot[] slots;

		/** Creates a new item inventory and slots */
		public GuiItemInventory(int width = 300, int height = 185) : base(width, height)
		{
			const int inlay = 4;
			inventoryScrollArea = new GuiScrollableArea((int)ContentsBounds.width - inlay * 2, (int)ContentsBounds.height - inlay * 2, ScrollMode.VerticalOnly) {
				X = inlay,
				Y = inlay
			};
			Add(inventoryScrollArea);
			CacheMode = CacheMode.Enabled;
			AutoRefreshInterval = 0.5f;
			Sync();
		}

		/** Sets this inventory component to display given inventory. */
		private void setSource(MDRInventory newInventory)
		{
			if (_source == newInventory)
				return;

			if (_source != null)
				_source.OnInventoryChanged -= Invalidate;			
			
			_source = newInventory;

			if (_source != null)
				_source.OnInventoryChanged += Invalidate;
			
			Sync();
		}

		/** Creates slots based on the number of slots in the characters inventory */
		private void Sync()
		{
			Invalidate();

			inventoryScrollArea.Clear();

			if (Source == null)
				return;

			slots = new GuiItemSlot[Source.Count];
			for (int index = 0; index < Source.Count; index++) {
				slots[index] = new GuiItemSlot((int)(index % COLUMNS) * SLOT_WIDTH, (int)(index / COLUMNS) * SLOT_HEIGHT, Source[index]);
				inventoryScrollArea.Add(slots[index]);
			}
			inventoryScrollArea.ContentsScrollRect.height = ((int)Math.Ceiling((float)(Source.Count / COLUMNS))) * SLOT_HEIGHT;
		}

		public override void Destroy()
		{
			Source = null;
			base.Destroy();
		}

	}
}

