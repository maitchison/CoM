/**
 * 2014 Matthew Aitchison
 * Last reviewed: Review Needed
 */

using UnityEngine;
using Mordor;
using Data;
using UI;
using UI.Generic;

namespace UI.State
{

	/** UI for creating a character */
	public class CreateCharacterState : GuiState
	{
		private GuiStatList statList;
		private GuiListBox<MDRRace> raceList;
		private GuiListBox<MDRGender> genderList;
		private GuiTextField nameInput;
		private GuiPictureSeletor portraitSelector;
		private GuiLabel characterInfoText;
		private GuiToggleButton allRacesToggle;

		private MDRCharacter character;

		public CreateCharacterState()
			: base("CharacterCreationState")
		{
			character = MDRCharacter.Create("");
			giveDefault();
			CreateUIComponents();
		}

		public override void Show()
		{
			base.Show();
			CoM.GameStats.Enabled = false;
		}

		public override void Hide()
		{
			base.Hide();
			CoM.GameStats.Enabled = true;
		}

		private void saveAndClose()
		{
			Trace.LogDebug("Saving character");
			character = GetCharacter();
			CoM.CharacterList.Add(character);
			CoM.State.SaveCharacters();
			Close();
		}

		/** Creates the UI componenets required to display the gui */
		private void CreateUIComponents()
		{
			var window = new GuiWindow(800, 560);
			window.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			window.Background.Color = new Color(0.5f, 0.5f, 0.5f);
			PositionComponent(window, 0, 0);
			Add(window);

			// ------------------

			nameInput = new GuiTextField(0, 0, 200);
			nameInput.Value = CharacterNameGenerator.GenerateName();
			nameInput.LabelText = "Name";
			nameInput.LabelPosition = LabelPosition.Left;
			window.Add(nameInput, 0, 20);

			var randomButton = new GuiButton("random", -1, 20);
			window.Add(randomButton, (int)nameInput.Bounds.xMax + 10, 25);

			// ------------------

			var genderListFrame = new FramedListBox<MDRGender>(200, 100, "Gender");
			window.Add(genderListFrame, 260, 100);
		
			portraitSelector = new GuiPictureSeletor();
			portraitSelector.Pictures = CoM.Instance.Portraits.GetEntries().ToArray();
			window.Add(portraitSelector, 460, 70 + 18 + 20);

			allRacesToggle = new GuiToggleButton();
			allRacesToggle.LabelText = "Show All";
			allRacesToggle.LabelPosition = LabelPosition.Right;
			allRacesToggle.X = (int)portraitSelector.Bounds.xMax + 10;
			allRacesToggle.Y = (int)portraitSelector.Bounds.y + 30;
			allRacesToggle.Value = false;
			allRacesToggle.OnValueChanged += delegate {
				updatePortraits(); 
			};
			window.Add(allRacesToggle);

			// ------------------
			
			var raceListFrame = new FramedListBox<MDRRace>(200, 240, "Race");
			window.Add(raceListFrame, 20, 240);
			
			statList = new GuiStatList(new MDRStats());
			window.Add(statList, 220 + 20, 240);
			
			var characterInfo = new GuiWindow(250, 240, "Info");
			window.Add(characterInfo, 470 + 40, 240);

			characterInfoText = new GuiLabel(0, 0, "");
			characterInfoText.Align = GuiAlignment.Full;
			characterInfoText.WordWrap = true;
			characterInfo.Add(characterInfoText);

			// ------------------

			// ------------------
			
			GuiButton cancelButton = new GuiButton("Cancel", 100);
			window.Add(cancelButton, 20, -20);
			
			GuiButton doneButton = new GuiButton("Save", 100);
			window.Add(doneButton, -20, -20);
			
			
			raceList = raceListFrame.ListBox;
			foreach (MDRRace race in CoM.Races)
				raceList.Add(race);
			
			genderList = genderListFrame.ListBox;
			genderList.Add(MDRGender.Male);
			genderList.Add(MDRGender.Female);
			
			genderList.OnSelectedChanged += delegate {
				updateGender();
			};
			raceList.OnSelectedChanged += delegate {
				updateRace();
			};

			doneButton.OnMouseClicked += delegate {
				
				if (statList.FreePoints != 0) {
					Engine.ConfirmAction(saveAndClose, "This character still has " + Util.Colorise(statList.FreePoints, Color.green) + " stat points left to spend.\nAre you sure you want to save the character without spending them?");
				} else {
					saveAndClose();
				}
			};	

			cancelButton.OnMouseClicked += delegate {
				Engine.PopState();
			};
			randomButton.OnMouseClicked += delegate {
				nameInput.Value = CharacterNameGenerator.GenerateName();
			};

			updateRace();	
			updateGender();
		}

		/** Returns the character created from the states controls */
		public MDRCharacter GetCharacter()
		{
			applyToCharacter();
			return character;
		}

		/** 
		 * Gives character some default items and attributes 
		 */
		private void giveDefault()
		{
			character.GoldInHand = 1500;
			character.Age = 16;
		}

		/**
		 * Applies state of current control to character stats 
		 */
		private void applyToCharacter()
		{
			character.Name = nameInput.Value;
			character.Gender = (genderList.SelectedIndex == 0) ? MDRGender.Male : MDRGender.Female;
			character.Race = (raceList.Selected);

			if (portraitSelector.Selected != null)
				character.PortraitID = ((SpriteEntry)portraitSelector.Selected).ID;

			statList.ApplyToCharacter(character);

			character.SetBaseHitsAndSpells();
		}

		/** Updates the list of potential portraits for this race and gender. */
		private void updatePortraits()
		{
			System.Predicate<SpriteEntry> allFilter = entry => 
				true;

			System.Predicate<SpriteEntry> genderFilter = entry => 
				entry.Property["Gender"] == genderList.Selected.ToString();

			System.Predicate<SpriteEntry> raceAndGenderFilter = entry => 
				entry.Property["Gender"] == genderList.Selected.ToString() && entry.Property["Race"] == raceList.Selected.ToString();

			SpriteEntry[] portraitsArray;

			if (allRacesToggle.Value)
				portraitsArray = CoM.Instance.Portraits.GetEntries(genderFilter).ToArray();
			else
				portraitsArray = CoM.Instance.Portraits.GetEntries(raceAndGenderFilter).ToArray();

			if (portraitsArray.Length == 0)
				portraitsArray = CoM.Instance.Portraits.GetEntries(allFilter).ToArray();
			
			portraitSelector.Pictures = portraitsArray;
		}

		/** Selects a random portrait using a seed so that the same hero will get the same portrait for each race and gender. */
		private void selectRandomPortrait()
		{	
			var roll = Util.SeededRandom(raceList.Selected, genderList.Selected, character.ID);
			portraitSelector.SelectedIndex = Util.ClampInt((int)(roll * portraitSelector.Pictures.Length), 0, portraitSelector.Pictures.Length - 1);
		}

		/** 
		 * Performs and adjustments that are required when character's race is changed.
		 * Resets the stats to the given race, aswell as the stat limits.
		 */
		private void updateRace()
		{
			statList.SetRace(CoM.Races[raceList.SelectedIndex]);
			updatePortraits();
			selectRandomPortrait();
			portraitSelector.SelectedIndex = Util.SystemRoll(portraitSelector.Pictures.Length) - 1;

		}

		/** 
		 * Performs and adjustments that are required when character's gender is changed.
		 * Updates the portrait list to show only the male or female portraits
		 */
		private void updateGender()
		{
			updatePortraits();
			selectRandomPortrait();
		}

		/**
		 * Updates the information displayed about current character, such as the guilds they can join
		 */
		private void updateCharacterInfo()
		{
			applyToCharacter();
			string info = character.Name + " may join the following guilds:\n\n";
			string guildString = "";
			foreach (MDRGuild guild in CoM.Guilds) {

				if (guild.CanAcceptRace(character.Race)) {
					Color color = guild.CanAccept(character) ? Color.green : Color.gray;
					guildString += (guildString == "" ? "" : ", ") + Util.Colorise(guild.Name, color);
				}
			}
			info += guildString;

			info += "\nYou have " + Util.Colorise(statList.FreePoints.ToString(), Color.white) + " points.";

			characterInfoText.Caption = info;
		}

		/**
		 * Updates the character's stats as player changes controls
		 */
		public override void Update()
		{
			base.Update();
			updateCharacterInfo();
		}
	}
}