using System;
using UI.Generic;
using Mordor;
using UnityEngine;
using System.Collections.Generic;
using UI.State;

namespace UI
{
	/** This state allows the user to drag and drop memebers into a party aswell as dispand the party. */
	public class EditPartyState : GuiState
	{
		private GuiWindow window;

		private GuiScrollableArea unassignedCharactersScrollArea;
		private GuiCharacterGrid unassignedCharacters;

		private GuiCharacterSlot[] characterSlot;
		private GuiPanel characterSlotsPanel;

		private GuiButton doneButton;
		private GuiButton dispandButton;

		private int refreshDepth = 0;

		public MDRParty Party {
			get { return _party; }
			set { setParty(value); }
		}

		private MDRParty _party;

		public EditPartyState(MDRParty party = null, bool createInsteadOfEdit = false)
			: base("Edit Party State")
		{
			var windowTitle = createInsteadOfEdit ? "Create Party" : "Edit Party";
			window = new GuiWindow(600, 520, windowTitle);

			window.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			window.Background.Color = new Color(0.4f, 0.42f, 0.62f);

			characterSlotsPanel = new GuiPanel(500, 110);
			characterSlotsPanel.Color = Color.clear;
			characterSlotsPanel.Align = GuiAlignment.Top;
			window.Add(characterSlotsPanel);

			characterSlot = new GuiCharacterSlot[4];

			for (int lp = 0; lp < 4; lp++) {
				var slot = new GuiCharacterSlot();
				slot.Tag = lp + 1;
				characterSlot[lp] = slot;
				characterSlotsPanel.Add(slot, 0, 10);
				characterSlotsPanel.PositionComponentToColumns(slot, lp + 1, 4, 100);
				characterSlot[lp].OnDDContentChanged += delegate {					
					applyToParty(slot.Tag - 1);
					refreshUnassignedCharacters();
				};
			}				

			unassignedCharacters = new GuiCharacterGrid(true);
			unassignedCharacters.DragDropEnabled = true;
			unassignedCharacters.OnCreateNewCharacter += delegate {
				refreshUI();
			};

			unassignedCharactersScrollArea = new GuiScrollableArea(550 + 21, 310, ScrollMode.VerticalOnly);
			unassignedCharactersScrollArea.Add(unassignedCharacters);

			var frame = GuiWindow.CreateFrame(unassignedCharactersScrollArea, "", GuiWindowStyle.ThinTransparent);
			frame.Color = new Color(0.1f, 0.1f, 0.1f);
			window.Add(frame, 0, -46, true);

			var unusedCaption = new GuiLabel("<B>Available Heroes</B>", (int)window.ContentsBounds.width + 10, 24);
			unusedCaption.EnableBackground = true;
			unusedCaption.TextAlign = TextAnchor.MiddleCenter;
			unusedCaption.Color = Color.Lerp(Colors.BackgroundRed, Color.black, 0.5f);
			unusedCaption.FauxEdge = true;
			unusedCaption.FontColor = new Color(1, 1, 1, 0.9f);
			unusedCaption.FontSize = 18;
			window.Add(unusedCaption, 0, frame.Y - 15);

			doneButton = new GuiButton("Done");
			window.Add(doneButton, 0, -10);		

			dispandButton = new GuiButton("Dispand");
			UIStyle.RedWarning(dispandButton);
			window.Add(dispandButton, 0, -10);

			window.PositionComponentToColumns(dispandButton, 1, 2, 100);
			window.PositionComponentToColumns(doneButton, 2, 2, 100);

			doneButton.OnMouseClicked += delegate {
				applyToParty();
				Engine.PopState();
			};
				
			dispandButton.OnMouseClicked += delegate {
				party.Dispand();
				Engine.PopState();
			};

			Party = party;

			Add(window, 0, 0);
		}

		/** Delegate to create a new character. */
		private void createCharacter(object source, EventArgs e)
		{
			int senderTag = 0;
			if (source is GuiComponent)
				senderTag = (source as GuiComponent).Tag;

			var createState = new CreateCharacterState();
			createState.OnStateClose += delegate {

				applyToParty();

				var character = createState.GetCharacter();

				// when the sender has a tag, use the tag to position the new character in the party.
				Trace.LogDebug("New Character {0} created, placing in position {1}", character, senderTag - 1);
				if (character != null && senderTag >= 1 && senderTag <= 4)
					Party.PlaceCharacter(character, senderTag - 1);
				
				refreshUI();

			};
			Engine.PushState(createState);
		}

		public override void Update()
		{
			base.Update();

			// put "create new" buttons on empty party slots
			for (int lp = 0; lp < 4; lp++) {
				if (characterSlot[lp].IsEmpty) {						
					characterSlot[lp].OnMouseClicked -= createCharacter;
					characterSlot[lp].OnMouseClicked += createCharacter;
					characterSlot[lp].Caption = "Create\nNew";
				} else {
					characterSlot[lp].Caption = "";
					characterSlot[lp].OnMouseClicked -= createCharacter;
				}
			}

		}

		public override void Show()
		{
			base.Show();
			refreshUI();
		}

		public override void Close()
		{
			base.Close();
			Party.CompactParty();
		}

		/** Applies any changes in the UI to the party. 
		 * 
		 * @param slotIndex if defined only this character will be updated. 
		*/
		private void applyToParty(int? slotIndex = null)
		{			
			if (Party == null)
				return;
			
			for (int lp = 0; lp < 4; lp++) {
				if (slotIndex != null && lp != slotIndex)
					continue;				

				var character = characterSlot[lp].CharacterPortrait == null ? null : characterSlot[lp].CharacterPortrait.Character;

				if (character == null && Party[lp] != null) {
					Party.RemoveCharacter(Party[lp]);
				} else if (character != Party[lp])
					Party.PlaceCharacter(character, lp);
			}				
		}

		/** Refreshes slots with given party and unassigned characters. */
		private void refreshUI()
		{			
			refreshDepth++;

			for (int lp = 0; lp < 4; lp++) {
				var character = (Party == null) ? null : Party[lp];
				var oldCharacter = characterSlot[lp].CharacterPortrait == null ? null : characterSlot[lp].CharacterPortrait.Character;
				if (oldCharacter != character)
					characterSlot[lp].CharacterPortrait = character == null ? null : new GuiCharacterPortrait(character);
			}

			refreshUnassignedCharacters();
		}

		private void refreshUnassignedCharacters()
		{
			unassignedCharacters.CharacterList = getUnassignedCharacters();
			unassignedCharactersScrollArea.FitToChildren(10);
		}

		private void setParty(MDRParty value)
		{
			_party = value;
			refreshUI();
		}

		/** Gets a list of all characters currently unassigned to a party. */
		private List<MDRCharacter> getUnassignedCharacters()
		{
			var result = new List<MDRCharacter>();
			foreach (MDRCharacter character in CoM.CharacterList) {
				if (character.Party == null)
					result.Add(character);
			}
			return result;
		}


	}
}

