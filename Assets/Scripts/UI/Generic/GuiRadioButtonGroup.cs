using System;
using UnityEngine;
using System.Collections.Generic;

namespace UI
{
	public enum GuiRadioButtonMode
	{
		/** Buttons are arranged horizontaly. */
		Horizontal,
		/** Buttons are arranged verticaly. */
		Vertial
	}

	/** A group of buttons where only one button can be selected at a time. */
	public class GuiRadioButtonGroup : GuiContainer
	{
		/** The size of a button in the radio group. */
		public Vector2 ButtonSize {
			get { return buttonSize; }
			set {
				if (buttonSize != value) {
					buttonSize = value;
					positionButtons();
				}
			}
		}

		/** The spacing between buttons. */
		public int ButtonSpacing {
			get { return buttonSpacing; }
			set {
				if (buttonSpacing != value) {
					buttonSpacing = value;
					positionButtons();
				}
			}
		}

		/** The spacing between buttons. */
		public GuiRadioButtonMode RadioMode {
			get { return radioMode; }
			set {
				if (radioMode != value) {
					radioMode = value;
					positionButtons();
				}
			}
		}

		/** The index of the currently selected item.  -1 for none. */
		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (value < 0)
					value = 0;
				if (value >= Buttons.Count)
					value = Buttons.Count - 1;
				if (selectedIndex != value) {					
					selectedIndex = value;
					updateButtonSelection();
					if (OnValueChanged != null)
						OnValueChanged(this, null);
				}
			}
		}

		/** The style to use to display buttons. */
		public bool DesaturateNonSelectedButtonIcons {
			get { return desaturateNonSelectedButtonIcons; }
			set {
				if (desaturateNonSelectedButtonIcons != value) {					
					desaturateNonSelectedButtonIcons = value;
					updateButtonSelection();
				}
			}
		}

		/** Sets selection by name.  If the name isn't found selection is unmodified. */
		public string SelectedName {
			get { return selectedIndex == -1 ? "" : ButtonNames[SelectedIndex]; }
			set {
				var button = getButtonByName(value);
				if (button != null)
					selectedIndex = button.Id;
			}
		}

		/** The style to use to display buttons. */
		public GUIStyle ButtonStyle {
			get { return buttonStyle; }
			set {
				if (buttonStyle != value) {					
					buttonStyle = value;
					recreateButtons();
				}
			}
		}

		public GuiEvent OnValueChanged;

		private Vector2 buttonSize = new Vector2(120, 30);
		private int buttonSpacing = 10;
		private int selectedIndex = -1;
		private GuiRadioButtonMode radioMode = GuiRadioButtonMode.Horizontal;
		private GUIStyle buttonStyle;
		private bool desaturateNonSelectedButtonIcons = true;

		private List<String> ButtonNames;
		private List<GuiButton> Buttons;
		private List<Sprite> ButtonIcons;


		public GuiRadioButtonGroup()
			: base(100, 100)
		{
			ButtonNames = new List<String>();
			ButtonIcons = new List<Sprite>();
			Buttons = new List<GuiButton>();
			Style = Engine.GetStyleCopy("Frame");
			buttonStyle = Engine.GetStyleCopy("SquareButton");
			EnableBackground = true;
		}

		/** Adds a new item to radio buttons list.  
		 * @param name The name to identify this button
		 * @param icon An optional icon to display instead of the name
		 * Returns the newly created button. */
		public GuiButton AddItem(string name, Sprite icon = null)
		{
			if (ButtonNames.Contains(name))
				throw new Exception("Radio button list already contains a button called [" + name + "]");
			
			ButtonNames.Add(name);
			ButtonIcons.Add(icon);
			recreateButtons();

			if (selectedIndex == -1)
				SelectedIndex = 0;
			
			return getButtonByName(name);
		}

		public override void Update()
		{
			base.Update();
			foreach (GuiButton button in Buttons) {
				button.Update();
			}
		}

		/** Returns button by given name, or null. */
		private GuiButton getButtonByName(string name)
		{		
			int index = ButtonNames.IndexOf(name);
			if (index >= 0)
				return Buttons[index];
			else
				return null;
		}

		private GuiButton createNewButton()
		{
			var button = new GuiButton("", (int)ButtonSize.x, (int)ButtonSize.y);
			button.Style = ButtonStyle;
			button.TextAlign = TextAnchor.MiddleCenter;
			button.OnMouseClicked += delegate {
				SelectedIndex = button.Id;
			};
			return button;
		}

		/** Recreates the buttons in the group. */
		private void recreateButtons()
		{
			Clear();
			Buttons.Clear();			

			int index = 0;

			foreach (string name in ButtonNames) {
				var button = createNewButton();
				var sprite = ButtonIcons[index];

				button.Name = name;
				button.Caption = name;
				button.Id = index;
				Buttons.Add(button);
				Add(button);

				if (sprite != null) {
					button.Caption = "";
					var image = new GuiImage(0, 0, sprite);
					image.X = ((int)button.ContentsFrame.width - image.Width) / 2;
					image.Y = ((int)button.ContentsFrame.height - image.Height) / 2;
					button.Image = image;
				}					

				index++;
			}

			positionButtons();	
			updateButtonSelection();
		}

		/** 
		 * Must be called whenever the selection changes.  
		 * Disables selection on non selected buttons, and updates the buttons to show which is selected. 
		 */
		private void updateButtonSelection()
		{			
			foreach (GuiButton button in Buttons) {				
				var isSelected = (button.Id == SelectedIndex);
				button.SelectedState = isSelected;
							
				if (DesaturateNonSelectedButtonIcons && button.Image != null)
					button.Image.ColorTransform = isSelected ? ColorTransform.Identity : ColorTransform.Saturation(0.33f);
			}	
		}

		/** Updates the positioning and size of the buttons to reflect the current button spacing */
		private void positionButtons()
		{
			if (Buttons.Count == 0) {
				SizeForContent(ButtonSpacing, ButtonSpacing);
				return;
			}

			int xPos = ButtonSpacing;
			int yPos = ButtonSpacing;

			GuiButton lastButton = null;

			foreach (GuiButton button in Buttons) {
				button.X = xPos;
				button.Y = yPos;
				button.Width = (int)ButtonSize.x;
				button.Height = (int)ButtonSize.y;
				switch (RadioMode) {
					case GuiRadioButtonMode.Horizontal:
						xPos += (int)button.Width + ButtonSpacing;
						break;
					case GuiRadioButtonMode.Vertial:
						yPos += (int)button.Height + ButtonSpacing;
						break;
				}
				lastButton = button;
			}

			this.SizeForContent((int)lastButton.Bounds.xMax + ButtonSpacing, (int)lastButton.Bounds.yMax + ButtonSpacing);
		}

		public override void Destroy()
		{
			Buttons.Clear();
			base.Destroy();
		}

	}
}