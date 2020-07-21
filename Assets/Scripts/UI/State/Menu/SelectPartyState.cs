/**
 * 2014 Matthew Aitchison
 * Last reviewed: 5/Jul/2014
 */

using UnityEngine;

using Mordor;
using Data;
using UI;
using UI.Generic;

namespace UI.State.Menu
{
	/** State to select the party to play the game with.  Can also create and delete characters. */
	public class SelectPartyState : GuiState
	{
		private GuiScrollableArea partyList;

		/** Weither or not a create party button should be present */
		public bool AllowCreateParty { get { return _allowCreateParty; } set { setCreateParty(value); } }

		private bool _allowCreateParty = false;

		public SelectPartyState(bool allowCreateParty = false)
			: base("SelectPartyState")
		{
			Util.Assert(CoM.CharacterList != null, "Select Party State requires character list to be created before initialization.");

			_allowCreateParty = allowCreateParty;

			GuiWindow window = new GuiWindow(GuiPartySpan.WIDTH + 40, 400);
			window.WindowStyle = GuiWindowStyle.Titled;
			window.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			window.Background.Color = Colors.BackgroundGray;
			window.Title = "Select Party";
			Add(window, 0, 100);

			partyList = new GuiScrollableArea(100, 100, ScrollMode.VerticalOnly);
			partyList.Align = GuiAlignment.Full;
			window.Add(partyList);
	
			GuiWindow buttonsWindow = new GuiWindow(500, 80);
			buttonsWindow.Y = (int)window.Bounds.yMax - 5;
			Add(buttonsWindow, 0);

			GuiButton BackButton = new GuiButton("Back");
			buttonsWindow.Add(BackButton, 0, 0);

			BackButton.OnMouseClicked += delegate {
				Engine.PopState();
			};					
		}

		/** Refresh list on show. */
		public override void Show()
		{
			Trace.LogDebug("Show: Update party list.");
			base.Show();
			updatePartyList();
		}

		/** Formats a charact for display on the character selection window. */
		private string formatCharacterForDisplay(MDRCharacter character)
		{
			string deadString = "";
			if (character.IsDead) {
				if (character.IsInTown)
					deadString = "(dead)";
				else
					deadString = "(dead on " + character.Depth + ")";
			}
				
			return Util.Colorise(string.Format("{1} <I><Size=12>{2}</Size></I>", character.Membership.MaxLevel, character.Name, deadString), Colors.FourNines);
		}

		/** Creates buttons for each party, allowing the user to select one of them. */
		private void updatePartyList()
		{
			CoM.CleanParties();
			partyList.Clear();
			int yPos = 0;

			foreach (MDRParty party in CoM.PartyList) {
				var partyButton = new GuiPartySpan(party) { X = 0, Y = yPos };
				partyButton.OnMouseClicked += delegate {
					selectParty(partyButton.Party);
				};
				partyList.Add(partyButton);
				yPos += partyButton.Height;						
			}

			if (AllowCreateParty) {
				//var createButton = new GuiButton("Create New Party", GuiPartySpan.WIDTH, GuiPartySpan.HEIGHT) { X = 0, Y = yPos };
				var createButton = new GuiPartySpan(null) { X = 0, Y = yPos };
				createButton.Editable = false;
				createButton.Caption = "Create New Party";
				createButton.Color = Color.gray;
				createButton.OnMouseClicked += delegate {
					var party = MDRParty.Create();
					CoM.PartyList.Add(party);
					var state = new EditPartyState(party, true);
					Engine.PushState(state);		 
				};
				partyList.Add(createButton);
				yPos += createButton.Height;											
			}

			partyList.FitToChildren();
		}

		private void setCreateParty(bool value)
		{
			_allowCreateParty = value;
			updatePartyList();
		}

		/** Starts game with given party. */
		private void selectParty(MDRParty party)
		{
			CoM.LoadParty(party);
		}
			
	}
}
