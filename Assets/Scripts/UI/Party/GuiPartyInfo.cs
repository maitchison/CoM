
using UnityEngine;

using Mordor;

using UI.Generic;
using UI.State.Menu;
using System;

namespace UI
{

	public enum PartyState
	{
		/** Normal UI */
		Normal,
		/** Users is choosing a character as the target of some effect. */
		ChoosingLivingCharacter,
		/** Users is choosing a character as the target of some effect. */
		ChoosingDeadCharacter
	}

	public delegate void GuiChoosenEvent(MDRCharacter character);

	public class GuiPartyInfo : GuiWindow
	{
		public MDRParty Party;

		private GuiCharacterFrame[] CharacterPanel;
		private GuiLabel InfoLabel;

		public GuiEvent OnPartyMemberDoubleClicked;

		/** Called when user chooses a party member.  Will be called with null if canceled */
		public static GuiChoosenEvent OnPartyMemberChoosen;

		private GuiContainer buttonTray;

		private GuiButton menuButton;

		/** Set to true if user is choosing a character (i.e. as the target of a spell).  \
		 * Will send an "OnPartyMemberChoosen" message rather than switching visible character. */
		public static PartyState State { get { return _state; } set { setState(value); } }

		private static PartyState _state;

		public GuiPartyInfo(int x = 0, int y = 0)
			: base(x, y)
		{
			IgnoreClipping = true;

			const int INFOLABEL_HEIGHT = 24;

			Width = 300;
			Height = GuiCharacterFrame.HEIGHT * 2 + INFOLABEL_HEIGHT + 24;

			CharacterPanel = new GuiCharacterFrame[4];
			for (int lp = 0; lp < 4; lp++) {
				CharacterPanel[lp] = new GuiCharacterFrame() {
					X = (lp % 2) * (GuiCharacterFrame.WIDTH),
					Y = (int)(lp / 2) * (GuiCharacterFrame.HEIGHT)
				};
				Add(CharacterPanel[lp]);
			}

			GuiButton RemoveButton = new GuiButton("Remove", 80, 20);
			Add(RemoveButton, 10, Height - 25);

			GuiButton AddButton = new GuiButton("Add", 80, 20);
			Add(AddButton, 100, Height - 25);

			menuButton = new GuiButton("Menu", 80, 20);
			Add(menuButton, 190, Height - 25);

			InfoLabel = new GuiLabel(0, 0, "") { 
				Height = INFOLABEL_HEIGHT,
				TextAlign = TextAnchor.MiddleCenter,
				Align = GuiAlignment.Bottom
			};
			Add(InfoLabel);

			RemoveButton.OnMouseClicked += delegate {
				if (Party.Selected != null) {
					Party.RemoveCharacter(Party.Selected);
					Sync();
				}
			};

			menuButton.OnMouseClicked += delegate {
				Engine.PushState(new InGameMenuState());
			};

			AddButton.OnMouseClicked += delegate {
				if (Party.MemberCount == 4)
					return;
				ModalOptionListState<MDRCharacter> chooseCharacterState = new ModalOptionListState<MDRCharacter>("Select a character to add", CoM.GetCharactersInCurrentArea());
				chooseCharacterState.OnStateClose += delegate {
					if (chooseCharacterState.Result != null)
						Party.AddCharacter(chooseCharacterState.Result);
					Sync();
				};
				Engine.PushState(chooseCharacterState);
			};

		}

		private static void setState(PartyState value)
		{
			_state = value;
			if (_state == PartyState.Normal)
				Mouse.CursorMode = CursorType.Standard;
			else
				Mouse.CursorMode = CursorType.Select;			
		}

		public override void Update()
		{
			menuButton.SelfEnabled = !Party.InCombat;

			base.Update();

			Sync();

			// Restore back to default state when we click the mouse button.
			if (State != PartyState.Normal && Input.GetMouseButtonUp(0)) {
				State = PartyState.Normal;
				if (OnPartyMemberChoosen != null)
					OnPartyMemberChoosen(null);
			}
		}

		/** 
		 * Updates the character panels to match the characters in the currently selected party 
		 */
		public void Sync()
		{
			if (Party == null) {
				for (int lp = 0; lp <= 3; lp++)
					CharacterPanel[lp].Character = null;
			} else {
				for (int lp = 0; lp <= 3; lp++) {
					CharacterPanel[lp].Character = Party[lp];				
					CharacterPanel[lp].ParentPartyInfo = this;				
				}
			}

			if (Party.Selected.CurrentMembership.IsPinned)
				InfoLabel.Caption = " Pinned";
			else {
				int XPRequired = Party.Selected.CurrentMembership.ReqXP;
				if (XPRequired > 0)
					InfoLabel.Caption = " " + Util.Comma(XPRequired) + "xp for level " + (Party.Selected.CurrentMembership.CurrentLevel + 1);
				else
					InfoLabel.Caption = " " + Util.Comma(Party.Selected.CurrentMembership.XPToPin) + "xp to pin";
			}
		}

	}
}

