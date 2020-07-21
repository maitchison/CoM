using Mordor;
using Data;

using UnityEngine;

namespace UI
{
	/** Displays a list of all spell classes known by a character and allows them to select one. */
	public class GuiSpellClassList : GuiContainer
	{
		private const int COLUMNS = 3;
		private const int PADDING = 0;

		public MDRCharacter Character { get { return _character; } set { setCharacter(value); } }

		private MDRCharacter _character;

		/** Called when user selects a spell class. */
		public GuiEvent OnSpellClassSelected;

		public GuiSpellClassList(int width = 302, int height = 359)
			: base(width, height)
		{	
			Style = Engine.GetStyleCopy("Paper");
			EnableBackground = true;
		}

		private void setCharacter(MDRCharacter value)
		{
			if (value == _character)
				return;
			_character = value;
			sync();
		}

		/** 
		 * Shows spell classes for currently selected character. 
		 */
		protected void sync()
		{
			if (Character == null)
				return;
			if (Character.KnownSpells.Count == 0)
				return;		

			createSpellClassButtons();
		}

		private void createSpellClassButtons()
		{
			Clear();

			if (Character == null)
				return;

			int index = 0;

			int COLUMN_WIDTH = 80;

			int requiredWidth = (COLUMNS * COLUMN_WIDTH) + PADDING * 2;
			int extraWidth = (int)ContentsFrame.width - requiredWidth;

			foreach (MDRSpellClass spellClass in CoM.SpellClasses) {
				if (!Character.KnowsAnySpellsFromSpellClass(spellClass))
					continue;

				var button = new GuiSpellClassButton(spellClass);

				button.X = (extraWidth / 2) + (index % COLUMNS) * COLUMN_WIDTH + PADDING;
				button.Y = (index / COLUMNS) * 100;

				button.Id = spellClass.ID;

				button.OnMouseClicked += delegate {
					if (OnSpellClassSelected != null)
						OnSpellClassSelected(button, null);
				};

				Add(button);

				index++;
			}				
		}
	}
}