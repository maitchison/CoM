using UnityEngine;

using Mordor;
using System;

namespace UI
{
	/** Displays information about a spell. */
	public class GuiSpellToolTip : GuiToolTip<MDRSpell>
	{
		public MDRSpell Spell { get { return Data; } set { Data = value; } }

		/** Updates the text for the label that displays this monsters information */
		override public void UpdateToolTip()
		{
			if (Spell == null) {
				IconSprite.Sprite = null;
				Header = "";
				Info = "";
				return;
			}

			IconSprite.Sprite = Spell.Icon;
			Header = Spell.Name;
			Info = Spell.FormattedDescription();
		}


	}


}
