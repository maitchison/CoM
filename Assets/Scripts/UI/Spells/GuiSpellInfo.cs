using System;
using Mordor;
using UnityEngine;

namespace UI
{
	/** Creates a small info panel detailing a spell */
	public class GuiSpellInfo : GuiContainer
	{
		private GuiSpellButton spellButton;
		private GuiLabel spellInfoLabel;

		public GuiSpellInfo(MDRSpell spell)
			: base(300, 50)
		{
			EnableBackground = true;
			Color = new Color(0.1f, 0.1f, 0.1f);

			spellButton = new GuiSpellButton(spell);
			spellButton.Width = 48;
			spellButton.Height = 48;
			Add(spellButton, 1, 0);

			spellInfoLabel = new GuiLabel(0, 0, "");
			spellInfoLabel.FontSize = 11;
			string spellInfoString = "<B>" + spell.Name + "</B>\n" + spell.FormattedDescription();
			spellInfoLabel.Caption = spellInfoString;
			Add(spellInfoLabel, 52, 0);

		}
	}

}

