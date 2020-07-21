using System;
using Mordor;

namespace UI
{
	/** A dragable character portrait. */
	public class GuiCharacterPortrait : GuiImage
	{
		public MDRCharacter Character { get { return _character; } set { setCharacter(value); } }

		private MDRCharacter _character;

		public GuiCharacterPortrait(MDRCharacter character)
			: base(0, 0)
		{
			Width = 58;
			Height = 64;
			Character = character;
		}

		private void setCharacter(MDRCharacter value)
		{
			_character = value;
			Apply();
		}

		/** Applies any changes in character to portrait. */
		public void Apply()
		{
			if (Character == null)
				return;
			Sprite = Character.Portrait;
		}
	}
}

