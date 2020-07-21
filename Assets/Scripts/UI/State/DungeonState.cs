using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Mordor;
using Engines;
using Data;
using UI.Generic;
using AlpacaSound;
using State;

namespace UI
{
	public enum DungeonGuiMode
	{
		/** Nothing, shown */
		None,
		/** Player, map, messages all visible */
		Normal
	}

	/** UI and Logic for dungeon state */
	public class DungeonState : GuiState
	{
		// Time between game ticks.  Ticks update things like poison
		private static float TICK_TIME = 2.0f;

		/** The games party */
		public MDRParty Party { get { return CoM.Party; } }

		public GuiMessageBox CombatMessageBox;
		public GuiMessageBox MessageBox;
		public GuiMap AutoMap;

		private GuiCharacterTabControl CharacterInfo;
		private GuiItemInventory ItemInventory;
		private GuiPartyInfo PartyInfo;
		private GuiActionBar ActionBar;

		private MDRArea lastArea;

		private bool mouseLookActive = false;
		private float dizzyLevel = 0;

		/** Displays different sets of gui objects depending on the state */
		private DungeonGuiMode _guiMode = DungeonGuiMode.None;

		public DungeonGuiMode GuiMode { get { return _guiMode; } set { setGuiMode(value); } }

		//todo:
		public bool InCombat { get { return false; } }

		public GuiComponent itemTrash;

		public GuiLabel DebugOSD;

		public DungeonState()
			: base("DungeonState")
		{
			CreateUIComponents();

			GuiMode = DungeonGuiMode.Normal;

			DebugOSD = new GuiLabel("");
			DebugOSD.FontColor = Colors.FourNines;
			DebugOSD.DropShadow = true;
			this.Add(DebugOSD, 10, 200);

			// Setup floor map
			CoM.Instance.FloorMap.GetComponent<MeshRenderer>().material.mainTexture = AutoMap.MapCanvas;

		}

		internal override void onResolutionChange()
		{
			base.onResolutionChange();
			this.Clear();
			CreateUIComponents();
		}

		/** Processes some odd jobs in the dungeon */
		private void updateParty()
		{
			// Check drowning 
			if (Party.CurrentTile.Water) {
				foreach (MDRCharacter character in Party) {
					if (character.Levitating)
						continue;
					if (character.IsDead)
						continue;

					character.TimeInWater += TICK_TIME;

					int difficulty = Util.ClampInt((int)character.TimeInWater / 2, 0, 25);

					if (Util.Roll(difficulty) < (character.Stats.Dex - character.Depth))
						continue;

					character.ReceiveDamage(Util.Roll(2) + 3);
					CoM.PostMessage("{0} is drowning!", CoM.Format(character));
					SoundManager.Play("OLDFX_DROWN");
				}
			}
				
			// Check players health and update status effects.
			bool partyMemberHasLowHits = false;
			foreach (MDRCharacter character in Party) {
				character.Update();
				int lowHits = Util.ClampInt(character.MaxHits / 10, 5, 25);
				if ((character.Hits < lowHits) && (!character.IsDead))
					partyMemberHasLowHits = true;
			}

			if (partyMemberHasLowHits) {
				//SoundManager.Play("HELP");				
			}

		}

		/** Processes character actions when not in combat.  So long as they are non combat actions. */
		private void ProcessNonCombatActions()
		{
			if (Party.InCombat)
				return;
			
			for (int lp = 0; lp < 4; lp++) {
				var character = Party[lp];
				if (character == null || character.IsDead)
					continue;

				var action = character.CurrentAction;
				if (action.Spell != null) {
					
					if (action.Spell.CombatSpell) {			
						character.CurrentAction = MDRAction.Empty;
						continue;
					}

					if (action.Spell.NeedsTargetSelection && action.SpecifiedTarget == null) {
						// no problem, just waiting on a target.
						continue;
					}

					action.Execute(character, Party);

					character.CurrentAction = MDRAction.Empty;
				}

			}
		}

		/** Called when the level changes.*/
		private void LevelChange()
		{						
			// save the game
			CoM.SaveGame();

			// camera might move (i.e. because of stairs) so sync it to stop and animation.
			PartyController.Instance.SyncCamera();
					
			// same with environemnt
			EnvironmentManager.Sync();

			// make sure we have our door opener scripts working
			PartyController.Instance.EnableDoorOpener = true;

			// We double check character records when changing levels.  This is needed as recods are ignored during character creation and will need to be
			// reapplied here.  Also if the character record is changed outside of the game this will catch it.
			CoM.UpdateCharacteRecords();

			orientateToStairsDirection();
		}

		/** Makes party face the direction of the stairs we are on (if we are on stairs) */
		private void orientateToStairsDirection()
		{
			var stairsName = DungeonBuilder.NameFromProperites(Party.LocationX, Party.LocationY, "Center", "Stairs");
			var stairsObject = GameObject.Find(stairsName);
			if (stairsObject == null)
				return;
			var reference = stairsObject.GetComponent<GridReference>();
			if (reference == null) {
				Trace.LogWarning("Stairs is missing grid reference.  This is needed for auto player orientation.");
				return;
			}
			var stairsDirection = reference.Direction;
			Party.Facing = stairsDirection;
		}

		/** Update game and ui */
		public override void Update()
		{		
			EnvironmentManager.NearView();

			CoM.State.Update();

			if (Party.CurrentTile == null) {
				Trace.LogWarning("Party has no tile!");
				return;
			}
				
			// check for level change
			if ((Party != null) && (Party.Depth != CoM.Instance.CurrentDungeonLevel)) {
				if (Party.Depth > CoM.Dungeon.Floors) {
					CoM.PostMessage("Sorry, only " + CoM.Dungeon.Floors + " floors are included in this version of Endurance.");
					Party.Depth = CoM.Dungeon.Floors;
				}			
				CoM.LoadDungeonLevel(Party.Depth);
				LevelChange();
			}

			// Update everything else.
			EnvironmentManager.StandardFog();
	
			UpdateMouseControls();

			ProcessPlayerKeys();

			if (!Party.InCombat)
				ProcessNonCombatActions();

			UpdateDizzy();

			UpdateUI();

			// check if all members are dead
			if ((Party.LivingMembers == 0) && !(Engine.CurrentState is ModalState) && !(Party.IsInTown)) {

				Engine.PushState(new AllPartyMembersDeadState(Party));
			}				

			base.Update();

		}

		public void ProcessPlayerKeys()
		{			
			if (PartyController.Instance.FreeMove)
				return;

			if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
				Party.Move(Party.Facing);

			if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
				Party.Move(Party.Facing + 180);
		
			if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
				Party.TurnLeft();
		
			if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
				Party.TurnRight();

			//todo: move these to area info

			if (Input.GetKeyUp(KeyCode.T))
				Party.TakeStairs();
			
			if (Input.GetKeyUp(KeyCode.F))
				Party.Fight();
		}

		/**
		 * Allows player to drag with the mouse to rotate the parties view, and swipe to turn, tap to move etc. 
		 */
		private void UpdateMouseControls()
		{
			// update mouse look
			if (Input.GetMouseButton(0) && (CoM.ComponentClickedDownOn == this)) {
				mouseLookActive = true;
				PartyController.Instance.LookOffset = Quaternion.Euler(Mouse.ClickTravel.y / 5, Mouse.ClickTravel.x / 5, Mouse.ClickTravel.x / 50);
			} else if (mouseLookActive && !Input.GetMouseButton(0)) {
				mouseLookActive = false;
				PartyController.Instance.LookOffset = Quaternion.Euler(0, 0, 0);
			}

			PartyController.Instance.EaseIn = !mouseLookActive;

			// update gestures
			if (Engine.ComponentClickedDownOn == this && Mouse.CursorMode == CursorType.Standard) {
				if (Mouse.Tap)
					Party.Move(Party.Facing);
				if (Mouse.SwipeLeft)
					Party.TurnLeft();
				if (Mouse.SwipeRight)
					Party.TurnRight();
				if (Mouse.SwipeDown)
					Party.Move(Party.Facing + 180);
			}
		}

		/** Updates ui elements to refelect gamestate */
		private void UpdateUI()
		{
			CharacterInfo.Character = Party.Selected;
			ItemInventory.Source = Party.Selected.Inventory;
			ActionBar.Character = Party.Selected;

			ItemInventory.Visible = CharacterInfo.Visible && CharacterInfo.SelectedIndex == 0;

		}

		/** Creates the UI componenets required to display the gui */
		private void CreateUIComponents()
		{
			int UI_MARGIN = Engine.SmallScreen ? 10 : 15;
			int BOTTOM_MARGIN = Debug.isDebugBuild ? 10 : 0;

			int messageBoxWidth = Util.ClampInt((Width - GuiActionBar.WIDTH - 100) / 2, 100, 600);
			MessageBox = new GuiMessageBox(messageBoxWidth, 205, true);	
			MessageBox.Messages = CoM.MessageLog;
			MessageBox.AutoScrollToBottom = true;
			MessageBox.AutoHideTimeout = 5f;
			MessageBox.HideStyle = MessageBoxHideStyle.FadingMouse;
			MessageBox.FadeInTime = 0.1f;
			MessageBox.FadeOutTime = 1f;
			MessageBox.EnableBackground = false;
			MessageBox.CompositedFade = false;
			PositionComponent(MessageBox, +UI_MARGIN, -UI_MARGIN);

			CombatMessageBox = new GuiMessageBox(600, 400);	
			//CombatMessageBox.Messages = CombatEngine.CombatLog;
			CombatMessageBox.Style = Engine.GetStyleCopy("Frame");
			CombatMessageBox.Label.TextAlign = TextAnchor.UpperLeft;
			CombatMessageBox.ReversedMessageText = true;
			CombatMessageBox.Label.DropShadow = true;
			CombatMessageBox.Color = Color.white.Faded(0.5f);
			CombatMessageBox.MaxMessages = 30;
			PositionComponent(CombatMessageBox, UI_MARGIN, 0);	

			CharacterInfo = new GuiCharacterTabControl();
			PositionComponent(CharacterInfo, -UI_MARGIN, +UI_MARGIN);
			CharacterInfo.Character = Party.Selected;

			AutoMap = new GuiMap(0, 0, Party);
			PositionComponent(AutoMap, +UI_MARGIN, +UI_MARGIN);
			AutoMap.Mode = MapMode.Small;

			ItemInventory = new GuiItemInventory();
			PositionComponent(ItemInventory, -UI_MARGIN - 10, (int)(CharacterInfo.Height) - 2);

			PartyInfo = new GuiPartyInfo();
			PositionComponent(PartyInfo, -UI_MARGIN, -UI_MARGIN - BOTTOM_MARGIN);
			PartyInfo.Party = Party;

			Add(MessageBox);
			//Add(CombatMessageBox);
			Add(ItemInventory);
			Add(CharacterInfo);
			Add(PartyInfo);
			Add(AutoMap);

			var openCloseCharacterInfoButton = GuiOpenCloseButton.Create(CharacterInfo);

			PartyInfo.OnPartyMemberDoubleClicked += delegate {				
				openCloseCharacterInfoButton.SwitchMode();
			};

			var PartyTimer = new GuiTimer(delegate {
				updateParty();
			}, TICK_TIME);
			Add(PartyTimer);

			ActionBar = new GuiActionBar();
			Add(ActionBar, 0, -UI_MARGIN - 2);

			// Make sure these components don't overlap, as they might in low resulutions.
			ActionBar.InsurePadding(PartyInfo, 30);
		}

		public override void Show()
		{
			base.Show();
			SoundManager.PlayMusicPlaylist("Dungeon");
		}

		private void setVisibleAll(bool visible)
		{
			MessageBox.Visible = visible;
			CharacterInfo.Visible = visible;
			AutoMap.Visible = visible;
			ItemInventory.Visible = visible;
			PartyInfo.Visible = visible;
		}

		/** Hides all gui objects */
		private void hideAll()
		{
			setVisibleAll(false);
		}

		/** Shows all gui objects */
		private void showAll()
		{
			setVisibleAll(true);
		}

		private void UpdateDizzy()
		{
			dizzyLevel -= Time.deltaTime * 2;
			
			if (dizzyLevel < 0)
				dizzyLevel = 0;
			
			if (dizzyLevel > 20) {
				PartyController.Instance.DizzyFactor = 10;
			} else if ((dizzyLevel < 20) && (PartyController.Instance.DizzyFactor != 0)) {
				PartyController.Instance.DizzyFactor = dizzyLevel;
			}
		}

		private void setGuiMode(DungeonGuiMode mode)
		{
			if (_guiMode == mode)
				return;

			_guiMode = mode;

			switch (mode) {
				case DungeonGuiMode.None:

					hideAll();

					break;
				case DungeonGuiMode.Normal:

					hideAll();

					CharacterInfo.Visible = true;
					ItemInventory.Visible = true;
					PartyInfo.Visible = true;

					AutoMap.Visible = true;

					MessageBox.Visible = true;

					break;
			}
		}

	}
}