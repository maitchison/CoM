using Mordor;
using UI;
using UI.Generic;
using UnityEngine;

namespace UI.State.Town
{
	/**
	 * Ancestor for any state that wishes to display character information 
	 * Allows the display of character and party information, aswell as the message log 
	 */
	public class TownBuildingState : GuiState
	{
		private GuiPartyInfo Party;
		private GuiCharacterTabControl CharacterInfo;
		private GuiItemInventory ItemInventory;
		private GuiMessageBox MessageLog;
		private GuiContainer LeftTwoThirds;

		protected GuiWindow MainWindow;
		protected GuiWindow LowerSection;
		protected GuiButton BackButton;


		public TownBuildingState(string name)
			: base(name)
		{
			int uiMargin = 10;

			GuiImage Background = new GuiImage(0, 0, ResourceManager.GetSprite("Backgrounds/Town"));
			Background.BestFit(ContentsFrame);
			Add(Background, 0, 0);

			LeftTwoThirds = new GuiContainer(Width - 340, Height);
			Add(LeftTwoThirds);

			LowerSection = new GuiWindow(0, 0);
			LowerSection.Background.Color = new Color(0.4f, 0.4f, 0.4f);
			LeftTwoThirds.Add(LowerSection);

			MainWindow = new GuiWindow(600, 500, name);
			LeftTwoThirds.Add(MainWindow, 0, 0);

			BackButton = new GuiButton("Back");
			BackButton.OnMouseClicked += delegate {
				Engine.PopState();
			};
			LowerSection.Add(BackButton);

			// UI

			Party = new GuiPartyInfo(0, 0);
			Party.Party = CoM.Party;
			Add(Party, -uiMargin, -uiMargin);
			
			CharacterInfo = new GuiCharacterTabControl();
			Add(CharacterInfo, -uiMargin, uiMargin);
			
			ItemInventory = new GuiItemInventory();
			Add(ItemInventory, -uiMargin, CharacterInfo.Height + uiMargin);

			RepositionControls();
		}

		protected void RepositionControls()
		{
			LowerSection.Height = 80;
			LowerSection.Width = MainWindow.Width - 20;

			LeftTwoThirds.PositionComponent(MainWindow, 0, 0);

			// Tuck the store under the character tab buttons in low res. 
			if (Engine.SmallScreen && MainWindow.Bounds.xMax + 30 > CharacterInfo.Bounds.xMin)
				MainWindow.Y += 32;			

			MainWindow.Y -= LowerSection.Height / 2;
		
			LowerSection.Y = MainWindow.Y + MainWindow.Height - 12;
			LeftTwoThirds.PositionComponent(LowerSection, 0, null);

			LowerSection.PositionComponent(BackButton, 0, 0);

		}

		/** The currently selected character */
		protected MDRCharacter Character {
			get {
				return CoM.Party.Selected;
			}
		}

		public override void Update()
		{
			ItemInventory.Source = CoM.Party.Selected.Inventory;
			CharacterInfo.Character = CoM.Party.Selected;
			base.Update();
		}
	}
}