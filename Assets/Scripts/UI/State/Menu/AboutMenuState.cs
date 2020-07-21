/**
 */

using UnityEngine;
using UI;
using UI.Generic;

namespace UI.State.Menu
{
	/** Dispalys information about the game */
	public class AboutMenuState : GuiState
	{

		private static string InfoString = 
			"Inspired by the classic " + Util.Colorise("Mordor: The Depths of Dejenol", Color.white) + ", written by David Allen.";

		private string CreditsString { get { return Util.ResourceToText("Credits"); } }

		private string AboutString { get { return Util.Colorise(InfoString + "\n" + "\n" + CreditsString, new Color(0.9f, 0.9f, 0.9f, 0.9f)); } }

		public AboutMenuState()
			: base("AboutState")
		{
			var window = new GuiWindow(420, 400, "About");
			Add(window, 0, 0);

			window.Add(Util.CreateBackButton("Done"), 0, -10);

			window.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			window.Background.Color = new Color(0.4f, 0.42f, 0.62f);

			var scrollBox = new GuiScrollableArea((int)window.ContentsFrame.width, (int)window.ContentsFrame.height - 40, ScrollMode.VerticalOnly);
			window.Add(scrollBox);

			var label = new GuiLabel(0, 20, AboutString, (int)window.ContentsBounds.width - 30);
			label.WordWrap = true;
			label.TextAlign = TextAnchor.UpperCenter;
			scrollBox.Add(label);

			scrollBox.FitToChildren();
		}

	}



}