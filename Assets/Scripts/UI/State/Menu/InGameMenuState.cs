using System;

using UnityEngine;
using UI.Generic;

namespace UI.State.Menu
{
	/** UI for main menu */
	public class InGameMenuState : ModalState
	{
		public InGameMenuState()
			: base("In-Game Menu")
		{
			Window.Title = "Menu";
			Window.Width = 250;
			Window.Height = 50;

			int spacing = 40;

			int buttonYPosition = 40;

			var saveButton = new GuiButton("Save Game", 150, 30);
			if (Engine.isWeb) {
				Window.Add(saveButton, 0, buttonYPosition);
				buttonYPosition += spacing;
			}

			var optionsButton = new GuiButton("Options", 150, 30);
			Window.Add(optionsButton, 0, buttonYPosition);
			buttonYPosition += spacing;

			var websiteButton = new GuiButton("Website", 150, 30);
			Window.Add(websiteButton, 0, buttonYPosition);
			buttonYPosition += spacing;

			var exitButton = new GuiButton("Return to Menu", 150, 30);
			Window.Add(exitButton, 0, buttonYPosition);
			buttonYPosition += spacing;

			buttonYPosition += spacing / 2;

			var resumeButton = new GuiButton("Resume Game", 150, 30);
			Window.Add(resumeButton, 0, buttonYPosition);
			resumeButton.ColorTransform = ColorTransform.BlackAndWhite;
			resumeButton.Color = new Color(0.8f, 1.5f, 0.8f);
			buttonYPosition += spacing;

			Window.Height += buttonYPosition;

			resumeButton.OnMouseClicked += delegate {
				Engine.PopState();
			};

			optionsButton.OnMouseClicked += delegate {
				Engine.PushState(new SettingsMenuState());
			};
				
			saveButton.OnMouseDown += delegate {				
				CoM.SaveGame();
				Engine.ShowModal("Game Saved", "\nThe game has been saved.\n");
			};

			websiteButton.OnMouseClicked += delegate {
				// Web version will redirect the contained page, so we need to save first.
				CoM.SaveGame();
				Application.OpenURL("playendurance.com");
			};

			exitButton.OnMouseClicked += CoM.ReturnToMainMenu;
		}

		public override void Show()
		{
			base.Show();
			Time.timeScale = 0.0f;
		}

		public override void Hide()
		{
			base.Show();
			Time.timeScale = 1f;
		}

	}
}
