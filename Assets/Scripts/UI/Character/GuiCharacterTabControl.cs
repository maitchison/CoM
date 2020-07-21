using UnityEngine;
using Mordor;
using System.Collections.Generic;

namespace UI
{
	/** A tab control showing various pages of a characters information*/
	public sealed class GuiCharacterTabControl : GuiTabControl
	{
		/** The character to display */
		public MDRCharacter Character { get { return _character; } set { setCharacter(value); } }

		private MDRCharacter _character;

		public GuiCharacterTabControl()
			: base(GuiCharacterPage.WIDTH + 16, GuiCharacterPage.HEIGHT + 20)
		{
			WindowStyle = GuiWindowStyle.Titled;

			TabIcons = new List<Sprite>();
			TabIcons.Add(CoM.Instance.IconSprites["Character_1"]);
			TabIcons.Add(CoM.Instance.IconSprites["Character_3"]);
			TabIcons.Add(CoM.Instance.IconSprites["Character_4"]);

			AddPage(new GuiCharacterEquipPage());
			AddPage(new GuiCharacterStatsPage());
			AddPage(new GuiCharacterSpellsPage());

		}

		/** Causes the control to redraw next frame */
		public void ResyncPages()
		{
			foreach (GuiCharacterPage page in Pages) {				
				page.Character = _character;
				page.Sync();
			}
		}

		/** Sets character for all pages */
		private void setCharacter(MDRCharacter value)
		{
			if (_character == value)
				return;

			if (_character != null)
				_character.OnChanged -= ResyncPages;

			_character = value;

			if (_character != null) {
				_character.OnChanged -= ResyncPages;
				_character.OnChanged += ResyncPages;
			}

			ResyncPages();
		}

		public override void Destroy()
		{
			Character = null;
			base.Destroy();

		}

		public override void AddPage(GuiTabPage page)
		{
			base.AddPage(page);
			if (page is GuiCharacterPage) {
				((GuiCharacterPage)page).Character = this.Character;
			}
		}
	}
}

