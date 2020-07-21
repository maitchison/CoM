using Mordor;
using UnityEngine;
using System.Collections.Generic;

namespace UI
{
	/** 
	 * Displays a characters spells */
	public class GuiCharacterSpellsPage : GuiCharacterPage
	{
		private GuiSpellClassList spellClassList;

		private GuiSpellList spellList;
		private GuiSpellClassHeader spellHeader;
		private GuiButton backButton;

		/** Creates a new gui character stats page.*/
		public GuiCharacterSpellsPage()
		{
			InnerShadow = true;
			spellClassList = new GuiSpellClassList(308, 364) { X = -2, Y = -2 };
			spellClassList.Character = Character;

			Add(spellClassList);		
		
			spellHeader = new GuiSpellClassHeader();
			spellHeader.Width = (int)ContentsFrame.width;
			Add(spellHeader);

			spellList = new GuiSpellList();
			Add(spellList, 10, spellHeader.Height + 10);

			spellClassList.OnSpellClassSelected += delegate(object source, System.EventArgs e) {
				if (!(source is GuiComponent))
					return;
				int id = (source as GuiComponent).Id;
				viewSpellClass(CoM.SpellClasses.ByID(id));
			};		

			backButton = new GuiButton("Back");
			Add(backButton, 0, -10, true);

			backButton.OnMouseClicked += delegate {
				viewSpellClassList();
			};
		}

		/** Shows list of spell classes player knowns */
		private void viewSpellClassList()
		{
			if (Character == null) {
				return;
			}
				
			spellClassList.Visible = true;
			spellHeader.Visible = false;
			spellList.Visible = false;
			backButton.Visible = false;

		}

		/** Shows all spells from given spell class. */
		private void viewSpellClass(MDRSpellClass spellClass)
		{
			spellClassList.Visible = false;
			spellHeader.Visible = true;
			spellList.Visible = true;
			backButton.Visible = true;

			spellHeader.SpellClass = spellClass;

			if (Character == null) {
				spellList.Clear();
				return;
			}

			var knownSpells = new List<MDRSpell>();
			foreach (MDRSpell spell in Character.KnownSpells) {
				if (spell.SpellClass.ID == spellClass.ID)
					knownSpells.Add(spell);
			}

			spellList.SetSpells(knownSpells);
		}

		/** Detect character change and update controls */
		protected override void SetCharacter(MDRCharacter value)
		{
			var previousCharacter = Character;

			base.SetCharacter(value);

			spellClassList.Character = Character;

			/** If character changed reset back to class list. */
			if (Character != previousCharacter) {
				viewSpellClassList();
				Trace.LogDebug("Switched from {0} to {1}", previousCharacter, Character);
			}
		}
	}


}


