
using System;
using System.Collections.Generic;

using UnityEngine;

namespace UI
{
	/** A scrollable listbox without a frame frame */
	public class ScrollableListBox<T> : GuiContainer
	{
		public GuiListBox<T> ListBox;
		public GuiScrollableArea ScrollArea;

		public ScrollableListBox(int width = 200, int height = 200)
			: base(width, height)
		{
			ScrollArea = new GuiScrollableArea(Width, Height);
			ScrollArea.Align = GuiAlignment.Full;
			ScrollArea.ScrollMode = ScrollMode.VerticalOnly;

			ListBox = new GuiListBox<T>(0, 0, width - (int)GUI.skin.verticalScrollbar.fixedWidth);
			ListBox.AutoSize = true;

			Add(ScrollArea);
			ScrollArea.Add(ListBox);
		}

		public override void Update()
		{
			base.Update();
			ScrollArea.FitToChildren();
		}

	}

	/** A listbox within a scrollable frame */
	public class FramedListBox<T> : GuiWindow
	{
		public GuiListBox<T> ListBox;

		public FramedListBox(int width = 200, int height = 200, string title = "")
			: base(width, height)
		{
			this.WindowStyle = title == "" ? GuiWindowStyle.Normal : GuiWindowStyle.Titled;
			Title = title;

			var scrollArea = new GuiScrollableArea(Width, Height);
			scrollArea.Align = GuiAlignment.Full;
			scrollArea.ScrollMode = ScrollMode.None;

			ListBox = new GuiListBox<T>(0, 0);
			ListBox.Align = GuiAlignment.Full;

			Add(scrollArea);
			scrollArea.Add(ListBox);
		}
			
	}

	/** 
	 * A list of selectable items with associated objects 
	 */
	public class GuiListBox<T> : GuiLabeledComponent
	{
		public delegate string CovertItemToString(T item);

		private const int ITEM_HEIGHT = 18;

		private List<T> ItemList;

		/** Called when selected item changes */
		public event GuiEvent OnSelectedChanged;
		/** Called when user selects (double clicks) an item */
		public event GuiEvent OnSelect;

		/** Index of currently selected item, or -1 for no selection */
		public int SelectedIndex { get { return _selectedIndex; } set { setSelectedIndex(value); } }

		private int _selectedIndex;

		/** Style used to draw items */
		protected GUIStyle ItemStyle;

		/** Style used to draw selected item */
		protected GUIStyle SelectedItemStyle;

		/** Gets or sets the font used for the listbox items. */
		public Font ItemFont {
			get { return ItemStyle.font; }
			set {
				ItemStyle.font = value;
				SelectedItemStyle.font = value;
			}
		}

		/** Gets or sets the font used for the listbox items. */
		public int ItemFontSize {
			get { return ItemStyle.fontSize; }
			set {
				ItemStyle.fontSize = value;
				SelectedItemStyle.fontSize = value;
			}
		}

		/** The currently selected object */
		public T Selected { get { return ((_selectedIndex < 0) || (_selectedIndex >= Count)) ? default(T) : ItemList[_selectedIndex]; } set { setSelectedObject(value); } }

		private GUIStyle selectedStyle;

		public CovertItemToString DoConvertItemToString;

		public bool AutoSize = false;

		public GuiListBox(int x, int y, int width = 200, int height = 200, CovertItemToString convertItemToString = null)
			: base(width, height)
		{
			ItemList = new List<T>();
			SelectedIndex = 0;
			CanReceiveFocus = true;

			Style.normal.background = StockTexture.CreateSolidTexture(new Color(0f, 0f, 0f, 0.25f));
			EnableBackground = false;

			ItemStyle = Engine.GetStyleCopy("Solid");
			ItemStyle.font = Engine.Instance.TextFont;
			ItemStyle.fontSize = 16;
			ItemStyle.alignment = TextAnchor.MiddleLeft;
			ItemStyle.normal.background = StockTexture.CreateSolidTexture(new Color(0f, 0f, 0f, 0.0f));

			SelectedItemStyle = new GUIStyle(ItemStyle);
			SelectedItemStyle.normal.background = StockTexture.CreateSolidTexture(new Color(0.2f, 0.2f, 0.5f));

			DoConvertItemToString = convertItemToString ?? ((T item) => item.ToString());
		}

		/** Sets the background color of the unselected items */
		public void SetBackgroundColor(Color color)
		{
			ItemStyle.normal.background = StockTexture.CreateSolidTexture(color);
		}

		/** Sorts list alphabeticly */
		public void Sort()
		{
			ItemList.Sort();
		}

		private void MoveSelectionIndex(int delta)
		{
			if (!HasSelection)
				return;
			SelectedIndex = (int)Util.Clamp(_selectedIndex + delta, 0, Count - 1);

		}

		protected override void ProcessKeyboardInput()
		{
			if (Input.GetKeyDown(KeyCode.UpArrow))
				MoveSelectionIndex(-1);
			if (Input.GetKeyDown(KeyCode.DownArrow))
				MoveSelectionIndex(+1);
			if (Input.GetKeyUp(KeyCode.Return) && HasSelection)
				doSelect();
		}

		/**
		 * Occurs when user selects the item.  Either by double clicking it, or by pressing enter 
		 */
		private void doSelect()
		{
			if (!HasSelection)
				return;
			if (OnSelect != null)
				OnSelect(this, new EventArgs());
		}

		/** Draw the items list */
		public override void DrawContents()
		{
			base.DrawContents();

			const int xPos = 0;
			int yPos = 0;
			int index = 0;

			foreach (T item in ItemList) {
				SmartUI.SolidText(new Rect(xPos, yPos, Width, ITEM_HEIGHT), DoConvertItemToString(item), (index == _selectedIndex) ? SelectedItemStyle : ItemStyle);
				yPos += ITEM_HEIGHT;
				index++;
			}
		}

		/** Select item on double click */
		public override void DoDoubleClick()
		{
			base.DoDoubleClick();

			int index = screenPositionToIndex((int)Mouse.Position.x, (int)Mouse.Position.y);
			if (index != -1)
				doSelect();
		}

		/** Clears all items */
		public void Clear()
		{
			ItemList.Clear();
		}

		/** The number of items in this listbox */
		public int Count {
			get {
				return ItemList.Count;
			}
		}

		/** Adds item to list */
		public void Add(T item)
		{
			ItemList.Add(item);
			if (AutoSize)
				Height = Count * ITEM_HEIGHT;
		}

		public override void Update()
		{
			base.Update();
		}

		/**
		 * Finds the item at the given mouse position.  
		 * Returns -1 if no item at that location
		 * 
		 * @param atX the mouse's screen X position
		 * @param atY the mouse's screen Y position
		 * 
		 * returns Index or -1 if no item at position
		 */
		private int screenPositionToIndex(int atX, int atY)
		{
			double index = (atY - this.AbsoluteBounds.y - this.Style.padding.top - 2) / ITEM_HEIGHT;
			if (index < 0)
				return -1;
			if (index >= Count)
				return -1;
			return (int)index;
		}

		/** Select new item */
		public override void DoClick()
		{
			base.DoClick();
			int index = screenPositionToIndex((int)Mouse.Position.x, (int)Mouse.Position.y);
			if (index != -1)
				SelectedIndex = index;
		}

		private void setSelectedIndex(int value)
		{
			if (value == _selectedIndex)
				return;
			_selectedIndex = value;
			if (OnSelectedChanged != null)
				OnSelectedChanged(this, new EventArgs());
		}

		private void setSelectedObject(T value)
		{
			int index = ItemList.IndexOf(value);
			if (index >= 0)
				setSelectedIndex(index);
		}

		/** If the listbox has a current selection */
		public bool HasSelection {
			get {
				return _selectedIndex != -1;
			}
		}

	}
}
