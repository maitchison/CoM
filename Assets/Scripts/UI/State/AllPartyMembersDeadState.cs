using System;
using UI;
using UI.Generic;
using Mordor;
using UnityEngine;
using UI.State.Town;

namespace State
{
	/** Dialogue for when all the party members have died. */
	public class AllPartyMembersDeadState : ModalState
	{
		public MDRParty Party;

		private int costToHire;

		public AllPartyMembersDeadState(MDRParty party) : base("Party has Died")
		{
			Party = party;

			Window.Width = 500;
			Window.Height = 300;

			costToHire = GameRules.CostToHireRescue(party.Depth);

			var label = new GuiLabel("", 450, 200);
			label.Color = Colors.FourNines;
			label.TextAlign = TextAnchor.UpperCenter;
			label.WordWrap = true;
			Window.Add(label, 0, 0, true);

			var okButton = new GuiButton("Ok");
			okButton.Visible = false;
			Window.Add(okButton, 0, -20, true);

			var returnButton = new GuiButton("Return to Menu", 160, 24);
			returnButton.Visible = false;
			Window.Add(returnButton, 0, 20, true);

			var hireButton = new GuiButton("Hire", 140, 30);
			hireButton.Visible = false;
			hireButton.SelfEnabled = Party.Gold >= costToHire;
			Window.Add(hireButton, 0, -25, true);		

			var hireCost = new GuiCoinAmount();
			Window.Add(hireCost, (int)hireButton.Bounds.xMax + 20, -31, true);
			hireCost.Value = costToHire;
			hireCost.Visible = false;

			var retrieveButton = new GuiButton("Retrieve");
			retrieveButton.Visible = false;
			Window.Add(retrieveButton, 0, 0);

			string message = "All party members have died.\n";

			if (party.Depth == 1) {
				message += "\nThankfuly some friendly adventures have found you and brought you back to the town temple.";
				okButton.Visible = true;
			} else if (party.Depth < 10) {
				message += "\nIt might take a while for anyone to find you down here.";

				if (Party.Gold >= costToHire)
					message += "\n\nYou'll need to either hire some adventurers or organise your own rescue party.";
				else {
					message += "\n\nLooks like you don't have enough to hire a rescue party.";
					message += "\nTogether you have only {0}.";
					message += "\n\nYou'll need to either form your own rescue party or have another character pay to send rescuers out.";

					Window.Height += 60;
				}

				returnButton.Visible = true;
				hireCost.Visible = true;
				hireButton.Visible = true;
			} else {
				message += "\nVery few venture this deep.  You won't be able to hire any adventures this time.";
				message += "\nYou need to either organise your own rescue party or pay at the temple for them to retrieve your soul.";
				returnButton.Visible = true;
				retrieveButton.Visible = true;
			}

			label.Caption = string.Format(message, CoM.CoinsAmount(Party.Gold));

			okButton.OnMouseClicked += delegate {
				CoM.AutoGotoTemple = true;
				CoM.Party.ReturnToTown();
				Engine.PopState();
			};

			hireButton.OnMouseClicked += delegate {
				CoM.AutoGotoTemple = true;
				CoM.Party.DebitGold(costToHire);
				CoM.Party.ReturnToTown();
				Engine.PopState();
			};

			returnButton.OnMouseClicked += CoM.ReturnToMainMenu;
		}
	}
}

