using System;

using UnityEngine;
using UI;
using Mordor;

namespace UI.State.Town
{
	/** Handles the the temple state, where hero's can be raised.*/
	public class TempleState : TownBuildingState
	{
		private GuiListBox<MDRCharacter> deadCharactersList;
		private GuiLabel raiseInfo;
		private GuiCoinAmount costLabel;
		private GuiButton raiseCharacterButton;
		private GuiLabel noDeadCharacters;

		int raiseCost;

		public TempleState()
			: base("Temple")
		{
			MainWindow.Width = 650;
			MainWindow.Height = 400;

			// Background:
			MainWindow.InnerShadow = true;
			MainWindow.Background.Align = GuiAlignment.None;
			MainWindow.Background.Sprite = ResourceManager.GetSprite("Backgrounds/TownTemple");				
			MainWindow.Background.Color = Color.white;
			MainWindow.Background.BestFit(MainWindow.ContentsFrame, true);
			MainWindow.PositionComponent(MainWindow.Background, 0, 0);

			// Bodies list:
			deadCharactersList = new GuiListBox<MDRCharacter>(0, 0, 250, 200);
			var deadCharactersListFrame = GuiWindow.CreateFrame(deadCharactersList, "Characters");
			deadCharactersListFrame.Style = Engine.GetStyleCopy("Box50");
			deadCharactersListFrame.Background.Color = Colors.BackgroundYellow.Faded(0.75f);
			MainWindow.Add(deadCharactersListFrame, 10, 0);

			noDeadCharacters = new GuiLabel("There are no dead\ncharacters here.", 250);
			noDeadCharacters.TextAlign = TextAnchor.MiddleCenter;
			noDeadCharacters.DropShadow = true;
			MainWindow.Add(noDeadCharacters, 10, 150);

			// Info:
			raiseInfo = new GuiLabel(0, 0, "", 300, 200);
			raiseInfo.WordWrap = true;
			raiseInfo.Color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
			raiseInfo.Style.padding = new RectOffset(10, 10, 10, 4);
			var raiseInfoFrame = GuiWindow.CreateFrame(raiseInfo, "Details");
			raiseInfoFrame.Style = Engine.GetStyleCopy("Box50");
			raiseInfoFrame.Background.Color = Color.white.Faded(0.75f);
			MainWindow.Add(raiseInfoFrame, (int)deadCharactersList.Bounds.xMax + 40, (int)deadCharactersList.Bounds.yMin);

			// Buttons:
			raiseCharacterButton = new GuiButton("Raise", 120);
			MainWindow.Add(raiseCharacterButton, raiseInfoFrame.X + 70, (int)raiseInfoFrame.Bounds.yMax + 5);

			costLabel = new GuiCoinAmount();

			MainWindow.Add(costLabel, (int)raiseCharacterButton.Bounds.xMax + 20, raiseCharacterButton.Y + 3);

			// Triggers
			deadCharactersList.OnSelectedChanged += delegate {
				doUpdateRaiseInfo();
			};
			PopulateDeadCharacterList();

			raiseCharacterButton.OnMouseClicked += delegate {
				if (deadCharactersList.Selected != null)
					doRaiseCharacter(deadCharactersList.Selected);
			};

			RepositionControls();
		}

		public override void Update()
		{
			base.Update();

			noDeadCharacters.Visible = deadCharactersList.Count == 0;
			raiseCharacterButton.Visible = deadCharactersList.Count >= 1;
			raiseCharacterButton.SelfEnabled = CoM.Party.Gold >= raiseCost;
		}

		/** 
		 * Raises the given character 
		 */
		private void doRaiseCharacter(MDRCharacter character)
		{
			if (character == null)
				throw new ArgumentNullException("character", "[character] is null.");
			if (!character.IsDead)
				throw new Exception("Character is not dead.");

			if (CoM.Party.Gold < raiseCost) {
				Engine.ShowLargeModal("Not Enough Coin", 
					string.Format(
						"The party does not have enough coin to raise {0}.\nYou need {1} but together only have {2}",
						CoM.Format(character), CoM.CoinsAmount(raiseCost), CoM.CoinsAmount(CoM.Party.Gold)
					));
				return;
			}

			CoM.Party.DebitGold(raiseCost, character);



			string complicationsMessage = GameRules.RaiseCharacterFromDead(character);
			if (complicationsMessage == "") {
				SoundManager.Play("OLDFX_RAISE");
				Engine.ShowModal("Success", string.Format("{0} was successfully raised back to life.", CoM.Format(character)));
			} else {
				SoundManager.Play("OLDFX_PARAL");
				Engine.ShowModal("Complications", string.Format("\nThere where problems while raising {0}.\n{0} {1}\n", CoM.Format(character), complicationsMessage));
			}

			PopulateDeadCharacterList();
		}

		/** 
		 * Updates the infomation panel descripting the raising details for the selected character 
		 */
		private void doUpdateRaiseInfo()
		{
			MDRCharacter character = deadCharactersList.Selected;

			raiseCost = GameRules.CostToRaise(character);

			if (character == null) {
				raiseInfo.Caption = "";
				costLabel.Visible = false;
				return;
			}				

			string notice = "{0} has died.";				

			if (raiseCost == 0) {
				notice += "\n\nBecause {0} is less than level 10 the temple has agreed to raise him for free.";
			} else {
				notice += "\n\nThe cost to raise {0} is {1}";
				if (raiseCost > CoM.Party.Gold)
					notice += "\nHowever you only have {2}.";
			}

			raiseInfo.Caption = string.Format(notice, CoM.Format(character), CoM.CoinsAmount(raiseCost), CoM.CoinsAmount(CoM.Party.Gold));

			costLabel.Visible = true;
			costLabel.Value = raiseCost;
		}

		/**
		 * Populates listbox with all dead characters currently in the morgue 
		 */
		private void PopulateDeadCharacterList()
		{
			deadCharactersList.Clear();
			foreach (MDRCharacter character in CoM.CharacterList) {
				if (character.IsDead && character.IsInTown)
					deadCharactersList.Add(character);
			}
			doUpdateRaiseInfo();
			raiseCharacterButton.SelfEnabled = deadCharactersList.Count > 0;

		}
		
	}
}