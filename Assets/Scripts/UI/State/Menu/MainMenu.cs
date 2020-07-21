using UnityEngine;
using UI.Generic;
using System.Collections;
using System;

namespace UI.State.Menu
{
	/** UI for main menu */
	public class MainMenuState : GuiState
	{
		/** The distance we can see during the menu screen.  High values will be slower. */
		private const float VIEW_DISTANCE = 4f;

		private GuiWindow window;

		private GuiLabel VersionLabel;

		/** [0..1] how much to black out background.  1 = completely black. */
		private float blackOutLevel = 1f;

		public MainMenuState()
			: base("Main Menu")
		{
			CoM.EnableCamera = true;

			window = new GuiWindow(350, 280); 
			window.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			window.Background.Color = Colors.BackgroundRed;
				
			Add(window, 0, 50);

			GuiLabel title = new GuiLabel(0, 0, "ENDURANCE");
			title.Font = CoM.Instance.TextFont;
			title.FontSize = 48;
			title.FontColor = new Color(1, 1, 1, 0.5f);
			title.DropShadow = true;
			title.ShadowDistance = 2;
			title.Interactive = true;
			window.Add(title, 0, 22);

			int currentButtonPosition = 100;
			const int buttonSpacing = 50;
        
			bool needsExitButton = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer;
			bool needsDebugButton = Settings.Advanced.PowerMode;
            
			// ----------------------------------------

			// ----------------------------------------

			GuiButton PlayButton = new GuiButton("Play");
			window.PositionComponent(PlayButton, 0, currentButtonPosition);
			window.Add(PlayButton);
			currentButtonPosition += buttonSpacing;

			GuiButton SettingsButtton = new GuiButton("Settings");
			window.PositionComponent(SettingsButtton, 0, currentButtonPosition);
			window.Add(SettingsButtton);
			currentButtonPosition += buttonSpacing;

			GuiButton AboutButton = new GuiButton("About");
			window.PositionComponent(AboutButton, 0, currentButtonPosition);
			window.Add(AboutButton);
			currentButtonPosition += buttonSpacing;

			GuiButton ExitButton = new GuiButton("Exit");
            
			if (needsExitButton) {
				window.PositionComponent(ExitButton, 0, currentButtonPosition);
				window.Add(ExitButton);
				currentButtonPosition += buttonSpacing;
				window.Height += buttonSpacing;
			}
				
			GuiButton DebugButton = new GuiButton("Debug", 140);
			DebugButton.ColorTransform = ColorTransform.BlackAndWhite;
			DebugButton.Color = new Color(0.6f, 1.0f, 0.7f);

			if (needsDebugButton) {
				window.PositionComponent(DebugButton, 0, currentButtonPosition);
				window.Add(DebugButton);
				window.Height += buttonSpacing;
			}

			title.OnDoubleClicked += delegate {
				if (Settings.Advanced.PowerMode == false) {
					Settings.Advanced.PowerMode = true;
					window.Add(DebugButton, 0, currentButtonPosition + (4 * buttonSpacing));
					window.Height += buttonSpacing;
				}
			};
				
			// ----------------------------------------

			if (Engine.isWeb) {
				GuiWindow WebWarningWindow = new GuiWindow(500, 90);
				WebWarningWindow.Add(
					new GuiLabel(Util.Colorise("Browser preview.\nFor the best experience download the standalone version.", new Color(1f, 0.75f, 0.45f))) 
				{ TextAlign = TextAnchor.MiddleCenter },
					0, 0);
				Add(WebWarningWindow, 0, -50);
			}

			// ----------------------------------------

			VersionLabel = new GuiLabel("");
			VersionLabel.FontColor = Color.yellow;
			Add(VersionLabel, -20, 10, true);

			PlayButton.OnMouseClicked += delegate {
				Engine.PushState(new SelectPartyState(true));
			};
			SettingsButtton.OnMouseClicked += delegate {
				Engine.PushState(new SettingsMenuState());
			};
			AboutButton.OnMouseClicked += delegate {
				Engine.PushState(new AboutMenuState());
			};
			ExitButton.OnMouseClicked += delegate {
				Application.Quit();
			};
			DebugButton.OnMouseClicked += delegate {
				Engine.PushState(new DebugMenuState());
			};
			OnStateShow += delegate {
				SoundManager.PlayMusicPlaylist("Intro");
			};				

			DefaultControl = PlayButton;

			CoM.Instance.StartCoroutine(updateFadeIn(2f));
		}

		public override void Show()
		{
			base.Show();
			CoM.Instance.CurrentDungeonLevel = -1;

			if (Settings.Advanced.RunAutoQualityTest && !CoM.BenchmarkModeEnabled)
				CoM.Instance.StartCoroutine(updateAutoQualityTest());

			if (!CoM.BenchmarkModeEnabled && Settings.Advanced.LastVersion != VersionInfo.AsString) {
				if (!string.IsNullOrEmpty(VersionInfo.Changes))
					Engine.ShowLargeModal("New Version",
						"<Size=20><B>New Version: " + VersionInfo.Number + " (" + VersionInfo.Date.ToString("dd/MMM/yy") + ")" + "</B></Size>" +
						"\n\nChanges\n\n" +
						Util.Colorise(VersionInfo.Changes, new Color(0.5f, 0.9f, 0.6f, 0.9f)) +
						"\n\n-VirtualHat\n");
				Settings.Advanced.LastVersion = VersionInfo.AsString;
			}

			if (Settings.Advanced.FirstRun && !CoM.BenchmarkModeEnabled) {
				WebServer.PostEvent("FirstRun");
				Engine.ShowLargeModal("Welcome to the Endurance Beta.",					
					"\nEndurance is in public " + Util.Colorise("beta", Color.yellow) + "." +
					"\nThings aren't finished, there are balancing issues, and there will be <B>bugs</B>." +
					"\n" +
					"\nThis is a project I've been working on during my holidays." +
					"\nI hope you enjoy playing it as much as I’ve enjoyed making it!" +
					"\n" +
					"\nPlease send any feedback or issues to admin@playendurance.com" +
					"\n" +
					"\n-VirtualHat\n");
				Settings.Advanced.FirstRun = false;
			}				
		}

		public override void Update()
		{
			base.Update();

			VersionLabel.Caption = Settings.Advanced.PowerMode ? VersionInfo.AsString : VersionInfo.Tag + " " + VersionInfo.Number;
		}

		/** Fade background in over time. */
		private IEnumerator updateFadeIn(float fadeInTime = 4f)
		{
			while (true) {			
				
				blackOutLevel -= (Time.deltaTime / fadeInTime);

				EnvironmentManager.SetFogAndDistance(VIEW_DISTANCE);

				if (blackOutLevel <= 0 || (Engine.CurrentState == CoM.Instance.DungeonState)) {

					blackOutLevel = 0f;
					yield break;
				}

				yield return new WaitForEndOfFrame();
			}				
		}

		/** Run game blanked out for 1 second and get FPS. */
		private IEnumerator updateAutoQualityTest()
		{
			Trace.Log("Starting auto quality benchmark:");
			Settings.General.HighQualityLighting = true;
			Settings.General.EnableSSAO = true;
			Settings.General.EnableAntiAliasing = true;
			Settings.General.EnableAnisotropicFiltering = true;
			Settings.General.EnablevSnyc = false;
			Settings.Advanced.TargetFrameRate = -1;
			EnvironmentManager.SetFogAndDistance(3f);

			int startFrameCount = Time.frameCount;		

			blackOutLevel = 1f;

			yield return new WaitForSeconds(2f);		

			int endFrameCount = Time.frameCount;

			float averageFPS = (endFrameCount - startFrameCount) / 2f;

			Trace.Log(" - completed: Scored an averrage FPS of {0:0.0}", averageFPS);

			WebServer.PostEvent(
				"Benchmark",
				string.Format("Score={0}", averageFPS)
			);

			Settings.General.EnablevSnyc = true;

			// take off AA and SSAO
			if (averageFPS < 45) {				
				Settings.General.EnableSSAO = false;
				Settings.General.EnableAntiAliasing = false;
				Trace.Log(" - turning off post processing effects.", averageFPS);
			}

			// low quality settigns
			if (averageFPS < 25) {				
				Settings.General.HighQualityLighting = false;
				Trace.Log(" - turning off high quality lighting.", averageFPS);
			}	

			Settings.Advanced.RunAutoQualityTest = false;

			EnvironmentManager.SetFogAndDistance(VIEW_DISTANCE);

			CoM.Instance.StartCoroutine(updateFadeIn(1f));
		}

		public override void DrawContents()
		{
			if (blackOutLevel > 0) {
				SmartUI.DrawFillRect(Frame, Color.black.Faded(blackOutLevel));
			}
			
			base.DrawContents();
		}
	
	}
}