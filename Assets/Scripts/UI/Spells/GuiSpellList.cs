using System;
using Mordor;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	/** A list of spells. */
	public class GuiSpellList : GuiContainer
	{
		int ypos = 0;

		public GuiSpellList()
			: base(200, 100)
		{
		}

		public void AddSpells(List<MDRSpell> newSpells)
		{
			foreach (MDRSpell spell in newSpells) {
				createSpellEntry(spell);
			}
			FitToChildren();
		}

		public void SetSpells(List<MDRSpell> newSpells)
		{
			ypos = 0;
			Clear();
			AddSpells(newSpells);
		}

		/** Creates a new entry for given spell. */
		private void createSpellEntry(MDRSpell spell)
		{
			var entry = new GuiSpellInfo(spell);
			entry.X = 0;
			entry.Y = ypos;
			Add(entry);
			ypos += entry.Height;
		}
	}

	public class GuiSpellClassHeader: GuiPanel
	{
		private GuiImage icon;
		private GuiLabel title;

		public MDRSpellClass SpellClass { get { return _spellClass; } set { setSpellClass(value); } }

		private MDRSpellClass _spellClass;

		public GuiSpellClassHeader()
			: base(200, 70)
		{
			EnableBackground = true;
			Color = Colors.BackgroundBlue.Faded(0.5f);
			icon = new GuiImage() { X = 5, Y = 5 };
			icon.Scale = 0.75f;
			icon.Framed = true;
			Add(icon);
			title = new GuiLabel("") {
				Align = GuiAlignment.Full,
				TextAlign = TextAnchor.MiddleCenter,
				DropShadow = true, FontColor = Colors.FourNines,
				Font = CoM.Instance.TitleFont,
				FontSize = 20				
			};

			OuterShadow = true;

			Add(title);
		}

		private void setSpellClass(MDRSpellClass value)
		{
			_spellClass = value;
			if (value != null) {
				icon.Sprite = value.Icon;
				title.Caption = value.Name;
			}

		}

	}

}