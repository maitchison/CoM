using System;
using System.Collections.Generic;
using Mordor;
using UnityEngine;
using UI.State;

namespace UI
{
	/** A grid of character portrait tiles. */
	public class GuiCharacterGrid : GuiPanel
	{
		public List<MDRCharacter> CharacterList { set { setCharacterList(value); } }

		private List<MDRCharacter> characterList;

		/** Called when a new character is created. */
		public SimpleAction OnCreateNewCharacter;

		/** Enables the create new character button. */
		public bool EnableCreateNew { get { return _enableCreateNew; } set { setCreateNew(value); } }

		private bool _enableCreateNew;

		private int COLUMNS = 4;

		private GuiCharacterSlot createCharacterButton;

		public GuiCharacterGrid(bool enableCreateNew = false)
			: base(550, 200)
		{
			_enableCreateNew = enableCreateNew;
			EnableBackground = false;
		}

		/** Add character to the list, remove it from source. */
		public override bool Transact(IDragDrop source)
		{
			MDRCharacter character;
			character = source == null ? null : (source.DDContent as GuiCharacterPortrait).Character;
			if (character != null && !characterList.Contains(character))
				characterList.Add(character);
			source.DDContent = null;
			Refresh();
			return true;
		}

		/** Hilight when draging characters. */
		public override void Update()
		{
			base.Update();

			Color = Color.gray;

			if (Engine.DragDrop.IsDragging) {
				if (Feedback == DDFeedback.Accept)
					Color = Color.Lerp(Color.gray, Color.green, 0.5f);
				else
					Color = Color.Lerp(Color.gray, Color.white, 0.5f);
			}		
				
			// remove empty slots.
			for (int lp = 0; lp < Children.Count; lp++) {
				if (Children[lp] is GuiCharacterSlot && Children[lp] != createCharacterButton) {					
					if (Children[lp].IsEmpty)
						Children[lp].Remove();
				}
			} 

		}

		/** Sets the list of characters and updates. */
		private void setCharacterList(List<MDRCharacter> value)
		{
			characterList = value;
			Refresh();
		}

		public override bool CanReceive(GuiComponent value)
		{
			return value is GuiCharacterPortrait;
		}

		private void setCreateNew(bool value)
		{
			_enableCreateNew = value;
			Refresh();
		}

		/** Places button at correct position. */
		private void positionButton(GuiComponent component, int index)
		{
			int xlp = index % COLUMNS;
			int ylp = index / COLUMNS;
			PositionComponentToColumns(component, (index % COLUMNS) + 1, COLUMNS, 100);
			component.Y = 10 + ylp * 120;
		}

		/** Updates the character tiles to represent the current list. */
		public void Refresh()
		{
			Clear();

			if (characterList == null)
				return;

			int maxY = 0;
			int lp;

			for (lp = 0; lp < characterList.Count; lp++) {
				var slot = new GuiCharacterSlot();

				slot.CharacterPortrait = new GuiCharacterPortrait(characterList[lp]);
				positionButton(slot, lp);
				maxY = (int)slot.Bounds.yMax;
				Add(slot);
			}

			if (EnableCreateNew) {
				createCharacterButton = new GuiCharacterSlot();

				createCharacterButton.OnMouseClicked += delegate {					
					var createState = new CreateCharacterState();
					createState.OnStateClose += delegate {
						if (OnCreateNewCharacter != null)
							OnCreateNewCharacter();
					};
					Engine.PushState(createState);
				};					

				positionButton(createCharacterButton, lp);

				createCharacterButton.Caption = "Create\nNew";
				maxY = (int)createCharacterButton.Bounds.yMax;
				Add(createCharacterButton);
			} else
				createCharacterButton = null;

			Height = maxY + 40;
		}

	}
}

