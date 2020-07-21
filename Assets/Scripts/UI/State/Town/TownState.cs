using UI;
using UI.State;
using UI.Generic;
using UnityEngine;
using UI.State.Menu;
using Engines;

namespace UI.State.Town
{
	/** The state to handle the games town */
	public class TownState : GuiState
	{
		public TownState()
			: base("TownState")
		{
			GuiImage Background = new GuiImage(0, 0, ResourceManager.GetSprite("Backgrounds/Town"));
			Background.BestFit(ContentsFrame);
			Add(Background, 0, 0);

			GuiWindow window = new GuiWindow(400, 260, "Town");
			window.Background.Color = Colors.BackgroundGray;
			Add(window, 0, 0);

			GuiButton MainMenuButton = new GuiButton("Exit", 150);
			MainMenuButton.ColorTransform = ColorTransform.BlackAndWhite;
			MainMenuButton.ColorTransform += ColorTransform.Multiply(1.2f, 1.2f, 1.2f);
			MainMenuButton.Color = new Color(1f, 0.4f, 0.3f);

			GuiButton storeButton = new GuiButton("General Store", 150);
			GuiButton templeButton = new GuiButton("Temple", 150);
			GuiButton guildButton = new GuiButton("Guild", 150);

			GuiButton tavernButton = new GuiButton("Tavern", 150);
			GuiButton libraryButton = new GuiButton("Library", 150);
			GuiButton bankButton = new GuiButton("Bank", 150);
			GuiButton dungeonButton = new GuiButton("Dungeon", 150);

			int buttonY = 25;

			window.Add(MainMenuButton, 0, buttonY - 5);

			window.Add(storeButton, 20, buttonY + 40 * 1);
			window.Add(templeButton, 20, buttonY + 40 * 2);
			window.Add(guildButton, 20, buttonY + 40 * 3);
		
			window.Add(tavernButton, -20, buttonY + 40 * 1);
			window.Add(libraryButton, -20, buttonY + 40 * 2);
			window.Add(bankButton, -20, buttonY + 40 * 3);

			window.Add(dungeonButton, 0, -20);

			storeButton.OnMouseClicked += delegate {
				Engine.PushState(new StoreState());
			};

			guildButton.OnMouseClicked += delegate {
				Engine.PushState(new GuildState());
			};

			templeButton.OnMouseClicked += delegate {
				Engine.PushState(new TempleState());
			};

			bankButton.OnMouseClicked += delegate {
				Engine.PushState(new BankState());
			};

			libraryButton.OnMouseClicked += delegate {
				Engine.PushState(new LibraryState());
			};


			MainMenuButton.OnMouseClicked += CoM.ReturnToMainMenu;

			OnStateShow += delegate {
				SoundManager.PlayMusicPlaylist("City");
			};

			// these are not implemented yet.
			tavernButton.SelfEnabled = false;
			bankButton.SelfEnabled = false;

			dungeonButton.OnMouseClicked += delegate {
				if (CoM.Party.LivingMembers == 0) {
					Engine.ShowModal("Can Not Enter Dungeon.", "All party memebers are dead.");
				} else {
					CoM.Party.Depth = 1;
					PartyController.Instance.SyncCamera();
					Engine.PopState();
				}
			};
		}

	}
}