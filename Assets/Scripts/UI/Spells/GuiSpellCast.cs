using System;
using Data;
using Mordor;

namespace UI
{
	/** Represents a spell being cast.  Typically drawn under the mouse during a targeted spell cast. */
	public class GuiSpellCast : GuiComponent
	{
		public MDRSpell Spell;

		/** Create a new instance of a spellCast gui object */
		public GuiSpellCast(MDRSpell spell) : base(32, 32)
		{
			Spell = spell;
		}

		public override void Draw()
		{
			Trace.Log("Draw");
			if (Spell != null) {
				SmartUI.Draw(Bounds, Spell.Icon);
			}
		}
	}
}

