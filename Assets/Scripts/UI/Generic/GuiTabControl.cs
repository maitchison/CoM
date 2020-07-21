using System;
using System.Collections.Generic;
using UnityEngine;

//todo: this could use a rewrite.  Now that I have out of bounds sub objects I can do this without overwriting drawContents! This would also allow for sub controls.

namespace UI
{

	/** 
	 * Displays a single page, of multiple options within a single component.  
	 * Buttons are used to swap between visible pages. 
	 **/
	public class GuiTabControl : GuiWindow
	{
		/** List of pages in this tab control */
		public List<GuiTabPage> Pages = new List<GuiTabPage>();

		/** If true TabControls title will update to the title of the currently selected tab */
		public bool SetTitleOnTabChange = false;

		/** Sprites to use for each tab. */
		public List<Sprite> TabIcons;

		public GuiRadioButtonGroup Buttons;

		/** 
		 * The currently selected page. 
		 */
		public GuiTabPage Selected {
			get {
				if ((SelectedIndex >= 0) && (SelectedIndex < Pages.Count))
					return Pages[SelectedIndex];
				else
					return null;
			}
		}

		public int SelectedIndex {
			get { return _selected; }
			set {
				_selected = value;
				if (SetTitleOnTabChange)
					this.Title = (Selected != null) ? Selected.Title : "";
			}
		}

		private int _selected = 0;

		/** 
		 * Creates a new new tab control 
		 */
		public GuiTabControl(int width = 250, int height = 350)
			: base(width, height)
		{			
			IgnoreClipping = true;
			Buttons = new GuiRadioButtonGroup();
			Buttons.RadioMode = GuiRadioButtonMode.Vertial;
			Buttons.ButtonSize = new Vector2(30, 30);
			Buttons.ButtonSpacing = 0;
			Buttons.ButtonStyle = Engine.GetStyleCopy("SmallButton");
			Buttons.OnValueChanged += delegate {
				SelectedIndex = Buttons.SelectedIndex;
			};
		}

		/**
		 * Buttons group is handled by us so we need to include them in the getComponentAtPosition call 
		 */
		public override GuiComponent GetComponentAtPosition(Vector2 screenLocation, bool onlyInteractive = false)
		{
			return Buttons.GetComponentAtPosition(screenLocation, onlyInteractive) ?? base.GetComponentAtPosition(screenLocation, onlyInteractive);
		}

		/** 
		 * Adds page to list of pages that can be displayed in the tab control 
		 */
		virtual public void AddPage(GuiTabPage page)
		{
			if (Pages.Contains(page))
				throw new Exception("Can not add page: " + page + " to tab control, as it has already been added.");
			Pages.Add(page);

			Add(page);
			page.Index = Pages.Count - 1;
			page.Active = false;
			Buttons.AddItem(page.Index.ToString(), getSpriteForTab(page.Index));
		}

		/** Returns the sprite for given tab, or null if no sprite for that tab index exists. */
		private Sprite getSpriteForTab(int index)
		{
			if (TabIcons == null || (index >= TabIcons.Count) || (index < 0))
				return null;
			return TabIcons[index];
		}

		/** 
		 * Update our selected page 
		 */
		public override void Update()
		{
			Buttons.Update();
			base.Update();
			Buttons.X = this.X - Buttons.Width + 5;
			Buttons.Y = this.Y + 25;

			if (Selected != null)
				Selected.Update();
		}

		/** 
		 * Special draw to draw buttons under the frame 
		 */
		public override void Draw()
		{			
			Buttons.Draw();
			base.Draw();
		}

		/**
		 * Special draw to handle tab page 
		 */
		public override void DrawContents()
		{	
			Background.Draw();
			if (Selected != null) {
				// draw currently selected page 
				Selected.Draw();
			}		
		}
	}
}