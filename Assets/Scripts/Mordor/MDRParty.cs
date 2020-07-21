using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

using Data;

using UnityEngine;

namespace Mordor
{
	 
	[DataObject("PartyLibrary")]
	public class MDRPartyLibrary : DataLibrary<MDRParty>
	{
	}

	/** 
	 * Records party information.  
	 * Contains logic to move the party's characters around the map.
	 */
	[DataObject("Party", false)]
	public class MDRParty : NamedDataObject, IEnumerable
	{
		public event GameTrigger OnSelectedChanged;
		public event GameTrigger OnPartyMembersChanged;
		public event GameTrigger OnMoved;
		public event GameTrigger OnTurned;
		public event GameTrigger OnDepthChanged;

		/** The maximum number of characters that can be in a party */
		public static int MAX_PARTY_SIZE = 4;

		private int _selectedCharacterIndex = 0;

		public int SelectedCharacterIndex { get { return _selectedCharacterIndex; } set { setSelectedCharacter(value); } }

		/** The currently selected character in the party */
		public MDRCharacter Selected { get { return _selectedCharacterIndex == -1 ? null : characters[_selectedCharacterIndex]; } set { setSelectedCharacter(value); } }

		/** The leader of the party (this is the first valid character in the party) */
		public MDRCharacter Leader { get { return characters[0] ?? characters[1] ?? characters[2] ?? characters[3]; } }

		// -----------------------------------------------------------------------------------

		/** List of all party members */
		private MDRCharacter[] characters = new MDRCharacter[4];

		public float CameraHeight = 0.0f;

		// -----------------------------------------------------------------------------------

		/** Reference to the dungeon this party is currently in */
		public MDRDungeon Dungeon { get { return CoM.Dungeon; } }

		/** Reference to a copy of the dungeon the party is in, as explored.  I.e. partys of it may be missing, or even
		 * incorrect */
		public MDRDungeon ExploredDungeon { get { return CoM.ExploredDungeon; } }

		public MDRMap Map { get { return Dungeon.Floor[Depth]; } }

		public MDRMap ExploredMap { get { return ExploredDungeon == null ? null : ExploredDungeon.Floor[Depth]; } }

		// -----------------------------------------------------------------------------------

		/** The parties current x location. */
		public int LocationX { get { return _locationX; } set { SetLocation(value, LocationY, Depth, Facing); } }

		/** The parties current y location. */
		public int LocationY { get { return _locationY; } set { SetLocation(LocationX, value, Depth, Facing); } }

		/** The parties current depth. */
		public int Depth { get { return _depth; } set { SetLocation(LocationX, LocationY, value, Facing); } }

		/** The parties current facing. */
		public Direction Facing { get { return _facing; } set { SetLocation(LocationX, LocationY, Depth, value); } }

		private int _locationX;
		private int _locationY;
		private int _depth;
		private Direction _facing;

		// -----------------------------------------------------------------------------------
		// Constructor
		// -----------------------------------------------------------------------------------
		
		/**
		 * Creates a party using given character as a leader.  Adds party to party list.
		 */
		public static MDRParty Create()
		{
			MDRParty result = new MDRParty();
			result.ID = CoM.PartyList.NextID();
			CoM.PartyList.Add(result);
			return result;
		}

		/** 
		 * Creates a new empty party, object.  This is a default constructor, please call MDRParty.Create to create parties.  
		 */
		public MDRParty()
			: base()
		{ 
			Name = "Party";
			characters = new MDRCharacter[MAX_PARTY_SIZE];
			_locationX = 11;
			_locationY = 9;
			_depth = 0;
			_facing = Direction.NORTH;
		}
				
		// --------------------------------------------------
		// List implementation
		// --------------------------------------------------

		/** Access to characters by position in party, [0..3] */
		public MDRCharacter this [int index] {
			get { 
				if (index < 0 || index >= characters.Length)
					throw new Exception(string.Format("Character index {0} out of bounds, must be between 0 and {1}", index, characters.Length));
				return characters[index];
			}
		}

		/** The number of valid members in this party */
		public int MemberCount {
			get { 
				int result = 0;
				for (int lp = 0; lp < 4; lp++)
					if (characters[lp] != null)
						result++;
				return result;
			}
		}

		/** 
		 * Returns if this party contains the given character or not.
		 */
		public bool Contains(MDRCharacter character)
		{
			return characters.Contains(character);
		}

		// --------------------------------------------------
		// Public
		// --------------------------------------------------

		/* The tile the character is currently standing on. */
		public FieldRecord CurrentTile { get { return Map.GetField(LocationX, LocationY); } }

		/* The area the character is currently in on. */
		public MDRArea Area {
			get {
				if (CurrentTile != null)
					return CurrentTile.Area;
				else
					return null;
			}
		}

		/** Total number of hits party currently has. */
		public int TotalHP {
			get {
				int result = 0;
				for (int lp = 0; lp < MAX_PARTY_SIZE; lp++)
					if (this[lp] != null)
						result += this[lp].Hits;						
				return result;
			}
		}

		/** 
		 * The perceived tile the character is currently standing on. 
		 * This tile represents what how the party memembers perceive their location.  
		 * At times this may be null.  It may also contain limited or invalid information.
		 */
		public FieldRecord PerceivedTile { get { return ExploredMap.GetField(LocationX, LocationY); } }

		/** The field the character would move into if they moved foward.  Null if invalid (off map) */
		public FieldRecord DestinationField {
			get { return Map.GetField(LocationX + Facing.DX, LocationY + Facing.DY); }
		}

		/** The perceived field the character would move into if they moved foward.  Null if invalid (off map) */
		public FieldRecord PerceivedDestinationField {
			get { return ExploredMap.GetField(LocationX + Facing.DX, LocationY + Facing.DY); }
		}

		/** The wall the player is currently facing */
		public WallRecord FacingWall { get { return  CurrentTile.getWallRecord(Facing.Sector); } }

		/** Removes all characters from party, and dispands it. */
		public void Dispand()
		{
			RemoveAll();
			CoM.CleanParties();
		}

		/** Removes all characters from party.  But will not actually remove it from the party list until CoM.CleanParties is called. */
		public void RemoveAll()
		{
			for (int lp = 3; lp >= 0; lp--) {
				RemoveCharacter(this[lp]);
			}
		}

		/** Returns the next avaliable slot for a character, or -1 for full. */
		private int nextFreeSlot {
			get {
				for (int lp = 0; lp < 4; lp++)
					if (this[lp] == null)
						return lp;
				return -1;
			}
		}

		/** 
		 * Adds given character to the party.  
		 * Character must not be null, not already be in the party, and there must be a free slot availiable.
		 * Character will be added to the end of the party 
		 * 
		 * @param silent, if true disables any messages triggered by adding this character.
		 * 
		 */
		public void AddCharacter(MDRCharacter character)
		{
			if (character == null)
				throw new Exception("Tried adding null character to party.");
			
			if (Contains(character))
				throw new Exception("Tried adding " + character + " to party, but they are already in this party.");
			
			if (MemberCount >= MAX_PARTY_SIZE)
				throw new Exception("Too many characters in party");

			int slot = nextFreeSlot;
			characters[slot] = character;
			character.Party = this;

			// auto select first character added.
			if (_selectedCharacterIndex == -1)
				_selectedCharacterIndex = slot;
		
			if (OnPartyMembersChanged != null)
				OnPartyMembersChanged();		
		}

		/** 
		 * Adds given character to the party.  
		 * 
		 * Character must not be null, and not already be in the party.
		 * 
		 * @param silent, if true disables any messages triggered by adding this character.
		 * @param position, the position to add the character at [0..3], if a character already existst at specified position 
		 * 		the previous character will b e removed.
		 * 
		 */
		public void PlaceCharacter(MDRCharacter character, int position)
		{			
			if (character == null)
				throw new Exception("Tried adding null character to party.");
			
			if (Contains(character))
				throw new Exception("Tried adding " + character + " to party, but they are already in this party.");

			if (position < 0 || position >= MAX_PARTY_SIZE) {
				throw new Exception(string.Format("Invalid position {0} to add party member {1}", position, character));
			}

			Trace.LogDebug("Placing {0} at {1}", character, position);

			// Remove previous character at location.
			if (characters[position] != null) {
				RemoveCharacter(characters[position]);
			}

			// Insert new character.
			characters[position] = character;
			character.Party = this;

			// Auto select first character added.
			if (_selectedCharacterIndex == -1)
				_selectedCharacterIndex = position;

			if (OnPartyMembersChanged != null)
				OnPartyMembersChanged();		
		}

		/**
		 * Swaps the locations of two characters 
		 */
		public void SwapCharacters(MDRCharacter c1, MDRCharacter c2)
		{			
			int index1 = characters.IndexOf(c1);
			int index2 = characters.IndexOf(c2);
			characters[index1] = c2;
			characters[index2] = c1;
			if (OnSelectedChanged != null)
				OnSelectedChanged();
		}

		/** 
		 * Removes given character from the party.  		 
		 */
		public void RemoveCharacter(MDRCharacter character)
		{
			if (character == null)
				return;

			if (!Contains(character))
				throw new Exception("Tried removing " + character + " from party, but they are not in this party.");

			int index = characters.IndexOf(character);

			characters[index] = null;
			character.Party = null;

			// try to make sure we have a valid selected character.
			while (Selected == null && _selectedCharacterIndex > 0)
				_selectedCharacterIndex--;			
			
			if (OnPartyMembersChanged != null)
				OnPartyMembersChanged();
		}

		/** Moves party forward one square in the direction they are facing, if not blocked by a wall */
		public void Move(Direction direction)
		{
			if (Selected.Paralized)
				return;

			// open a door if we move through it.
			var door = getDoor(LocationX, LocationY, direction);
			if (door != null)
				openDoor(door);

			var perceivedWallWeWillMoveThrough = PerceivedTile.getWallRecord(direction);
			var actualWallWeWillMoveThrough = CurrentTile.getWallRecord(direction);

			if (perceivedWallWeWillMoveThrough != actualWallWeWillMoveThrough) {
				PerceivedTile.setWallRecord(direction.Sector, actualWallWeWillMoveThrough);
				CoM.Instance.RefreshMapTile(LocationX, LocationY);
			}
				
			if (CanMove(direction)) {
				SetLocation(LocationX + direction.DX, LocationY + direction.DY);
			}
		}

		/** Turns members of the party left (anti clockwise) */
		public void TurnLeft()
		{
			if (Selected.Paralized)
				return;
			Facing -= 90;
		}

		/** Turns members of the party right (clockwise) */
		public void TurnRight()
		{
			if (Selected.Paralized)
				return;
			Facing += 90;
		}

		/** Turns the members of the party around 180 degrees */
		public void TurnAround()
		{
			if (Selected.Paralized)
				return;
			Facing += 180;
		}

		/**
		 * Takes the stairs up or down (depending on what you are standing on).  
		 * If the character is not on stairs nothing will happen.
		 */
		public void TakeStairs()
		{
			if (CurrentTile.StairsDown)
				Depth += 1;
			else if (CurrentTile.StairsUp)
				Depth -= 1;
		}

		/** Checks if party can move in given direction. */
		public bool CanMove(Direction direction)
		{
			var newField = Map.GetField(LocationX + direction.DX, LocationY + direction.DY); 

			// don't allow character to move off map or into rock
			if (newField == null || newField.Rock)
				return false;

			// check for wall crossing
			if (CurrentTile.getWallRecord(direction).Wall)
				return false;

			return true;
		}

		/** Returns a non sparse list of all members in the party in order. */
		public List<MDRCharacter> GetMembers()
		{
			var result = new List<MDRCharacter>();
			for (int lp = 0; lp < 4; lp++) {
				if (this[lp] != null)
					result.Add(this[lp]);
			}
			return result;
		}

		/** Returns a random member of the party. */
		public MDRCharacter RandomMember()
		{
			return GetMembers()[Util.SystemRoll(MemberCount) - 1];
		}

		/** 
		 * Gives a new item to party.
		 *		 
		 * @sourceMonster The monster that dropped this item.
		 * @giveToCharacter The character that should receive the item, if null a random character will be selected.
		 * 
		 * @returns the item instance created.
		 * 
		 */
		public MDRItemInstance ReceiveLoot(MDRItem item, MDRMonster sourceMonster = null)
		{
			MDRCharacter characterReceiving = null;
			return ReceiveLoot(item, sourceMonster, ref characterReceiving);
		}

		public MDRItemInstance ReceiveLoot(MDRItem item, MDRMonster sourceMonster, ref MDRCharacter giveToCharacter)
		{
			var instance = MDRItemInstance.Create(item, IdentificationLevel.Auto);

			giveToCharacter = giveToCharacter ?? RandomMember();

			giveToCharacter.GiveItem(instance);

			if (sourceMonster != null)
				CoM.GameStats.RegisterItemFound(giveToCharacter, instance, sourceMonster);

			return instance;
		}

		/** Causes party to receive given loot.  Loot will be distributed randomly amoung party memembers */
		public void ReceiveLoot(MDRItem[] itemList, MDRMonster sourceMonster = null)
		{
			if (itemList == null)
				return;
			foreach (MDRItem item in itemList) {
				ReceiveLoot(item, sourceMonster);
			}
		}

		/** The total gold of all players in this party. */
		public int Gold {
			get {
				int result = 0;
				for (int lp = 0; lp < 4; lp++) {
					if (this[lp] != null)
						result += this[lp].PersonalGold;
				}
				return result;
			}
		}

		/** 
		 * Takes money away from party memmbers as evenly as possiable. 
		 * If party doesn't have enough gold returns false and doesn't take any money away.
		 * 
		 * @param memeber If not null this memeber will pay first and party gold will only be used if they don't have enough.
		 * 
		 */
		public bool DebitGold(int amount, MDRCharacter primaryMember = null)
		{
			if (amount == 0)
				return true;

			if (MemberCount == 0)
				throw new Exception("Can not debit gold from party with no members.");

			if (amount < 0)
				throw new Exception("Can not debit negative gold.");

			if (Gold < amount)
				return false;

			if (primaryMember != null) {
				if (primaryMember.PersonalGold >= amount) {
					primaryMember.DebitPersonalGold(amount);
					return true;
				}

				amount -= primaryMember.PersonalGold;
				primaryMember.DebitPersonalGold(primaryMember.PersonalGold);	
			}

			// Random round robin for small amounts.
			if (amount <= 4) {
				int member = Util.Roll(4) - 1;
				while (amount > 0) {

					if (this[member] != null && this[member].PersonalGold >= 1) {
						this[member].DebitPersonalGold(1);
						amount--;
					}						
					member = (member + 1) % 4;					
				}
				return true;
			}

			int goldDebited = 0;

			int membersWithGold = 0;
			for (int lp = 0; lp < 4; lp++) {
				if (characters != null && characters[lp].PersonalGold > 0)
					membersWithGold++;
			}

			int averageShare = (int)Mathf.Floor(amount / membersWithGold);

			// take share away from each person with cash.
			for (int lp = 0; lp < 4; lp++) {
				var character = characters[lp];
				if (character == null)
					continue;
				if (character.PersonalGold > 0) {
					int thisShare = averageShare;
					if (character.PersonalGold < averageShare)
						thisShare = character.PersonalGold;
					character.DebitPersonalGold(thisShare);
					goldDebited += thisShare;
				}
			}

			// try again with the remaining members.
			if (goldDebited < amount)
				DebitGold(amount - goldDebited);

			return true;
		}

		/** Causes party to receive gold */
		public void ReceiveGold(int gold)
		{
			int goldPerMember = (int)(gold / MemberCount);
			if (goldPerMember == 0)
				return;

			foreach (MDRCharacter character in GetMembers())
				character.CreditGold(goldPerMember);
		}

		/** 
		 * Causes party to attack monsters in area.  Members will use default actions, however if selected characters default is defend, they will be switched to attack.
		 */
		public void Fight()
		{			
			DefaultActions();
			if (Selected.CurrentAction.Type == ActionType.Defend) {
				Selected.CurrentAction = MDRAction.Fight;
			}
		}

		/** 
		 * Sets all characters to use their default actions. 
		 * 
		 * @param force If true then characters actions will be forced to default even if another action is currently specificed 
		 */
		public void DefaultActions(bool force = true)
		{
			foreach (MDRCharacter character in GetMembers()) {
				if (character.CurrentAction.IsEmpty || force)
					character.DefaultAction();
			}
		}

		/** Sets all characters to not performing any action */
		public void ClearActions()
		{			
			foreach (MDRCharacter character in GetMembers())
				character.CurrentAction = MDRAction.Empty;
		}

		/**
		 * The number of living memebers in this party 
		 */
		public int LivingMembers {
			get { 
				int livingMembers = 0;
				for (int lp = 0; lp < 4; lp++) {
					var character = this[lp];
					if ((character != null) && (!character.IsDead))
						livingMembers++;
				}
				return livingMembers;
			}
		}

		/** 
		 * Returns party to town 
		 */
		public void ReturnToTown()
		{
			SetLocation(9, 11, 0, Facing);
		}

		/** 
		 * If the party is in the town or not 
		 */
		public bool IsInTown {
			get { return Depth == 0; }
		}

		public bool InCombat {
			get { return CoM.Instance.DungeonState.InCombat; }
		}

		/** Restores all character back to full health, raising them if needed. */
		public void RestoreAll()
		{
			for (int lp = 0; lp < MAX_PARTY_SIZE; lp++) {
				if (this[lp] != null)
					this[lp].FullRestore();
			}
		}

		/** Returns a description of the depth of the party.  I.e. "Dungeon Level 1" */
		public string DepthDescription {
			get { 				
				if (Depth == 0)
					return "In Town";
				else
					return "Dungeon Level " + Depth;
			}
		}

		/** Moves characters in party so that there are no empty gaps. */
		public void CompactParty()
		{
			var list = GetMembers();
			for (int lp = 0; lp < 4; lp++) {
				characters[lp] = (lp >= list.Count) ? null : list[lp];
			}
		}

		// ------------------------------------------------------------------------------------------------------
		// Getters and setters
		// ------------------------------------------------------------------------------------------------------

		/** Sets location, depth and facing at the same time. */
		public void SetLocation(int newX, int newY, int newDepth, Direction newFacing)
		{
			bool depthChanged = (Depth != newDepth);
			bool locationChanged = (LocationX != newX) || (LocationY != newY);
			bool facingChanged = (Facing != newFacing);

			if (!locationChanged && !depthChanged && !facingChanged)
				return;

			_locationX = newX; 
			_locationY = newY; 
			_depth = newDepth;
			_facing = newFacing;

			if (locationChanged)
				processLocationUpdate();			

			if (depthChanged)
				processDepthUpdate();			

			if (facingChanged)
				processFacingUpdate();
		}

		public void SetLocation(int newX, int newY)
		{
			SetLocation(newX, newY, Depth, Facing);
		}

		/** Sets selected character to given value */
		private void setSelectedCharacter(int characterIndex)
		{
			if (characterIndex == _selectedCharacterIndex)
				return;
			Util.Assert((characterIndex >= 0) && (characterIndex < MAX_PARTY_SIZE), "Invalid character index");
			_selectedCharacterIndex = characterIndex;			
			if (OnSelectedChanged != null)
				OnSelectedChanged();
		}

		/** Sets selected character to given value, if character is not found selection remains unchanged */
		private void setSelectedCharacter(MDRCharacter character)
		{
			for (int lp = 0; lp < MAX_PARTY_SIZE; lp++) {
				if (characters[lp] == character) {
					setSelectedCharacter(lp);
					return;
				}
			}
		}
			
		// -------------------------------------------------------------------------------------------------------------
		// PRIVATE
		// -------------------------------------------------------------------------------------------------------------

		/** The character responsabile for detecting traps and dungeon features */
		private MDRCharacter PerceptionCharacter {
			get { 
				float highestSkill = Leader.PerceptionSkill;
				MDRCharacter result = Leader;
				for (int lp = 0; lp < 4; lp++) {					
					if (this[lp] == null)
						continue;
					if (this[lp].PerceptionSkill > highestSkill) {
						highestSkill = this[lp].PerceptionSkill;
						result = this[lp];						
					}
				}					
				return result; 
			}
		}

		/** 
		 * Causes detials in the perceived tile to be copied across from the actual tile, will also log messages 
		 * decribing the new features discovered.  Returns true if any update occured
		 * 
		 * @returns if any changes where made.
		 */
		private bool detectSquare(FieldRecord perceivedTile, FieldRecord actualTile, string message, MDRCharacter character)
		{
			string detailName = "";

			if (perceivedTile.FaceNorth != actualTile.FaceNorth)
				detailName += "a face north, ";	
			if (perceivedTile.FaceEast != actualTile.FaceEast)
				detailName += "a face east, ";	
			if (perceivedTile.FaceWest != actualTile.FaceWest)
				detailName += "a face west, ";	
			if (perceivedTile.FaceSouth != actualTile.FaceSouth)
				detailName += "a face south, ";	

			if (perceivedTile.Antimagic != actualTile.Antimagic)
				detailName += "an anti-magic, ";
			if (perceivedTile.Rotator != actualTile.Rotator)
				detailName += "a rotator, ";
			if (perceivedTile.Extinguisher != actualTile.Extinguisher)
				detailName += "an extinguisher, ";	
			if (perceivedTile.Stud != actualTile.Stud)
				detailName += "a stud, ";

			if (detailName != "")
				CoM.PostMessage(message, CoM.Format(character), detailName.TrimEnd(new char[]{ ' ', ',' }));	

			perceivedTile.FaceNorth = actualTile.FaceNorth;
			perceivedTile.FaceEast = actualTile.FaceEast;
			perceivedTile.FaceWest = actualTile.FaceWest;
			perceivedTile.FaceSouth = actualTile.FaceSouth;
			perceivedTile.Antimagic = actualTile.Antimagic;
			perceivedTile.Rotator = actualTile.Rotator;
			perceivedTile.Extinguisher = actualTile.Extinguisher;
			perceivedTile.Stud = actualTile.Stud;

			return (detailName != "");
		}

		/** Returns the wall object at given location and direction, or null if not found. */
		private GameObject getDoor(int x, int y, Direction facing)
		{
			string direction = "";

			if (facing.isNorth)
				direction = "North";

			if (facing.isEast)
				direction = "East";

			if (facing.isSouth) {
				direction = "North";
				y--;
			}
			if (facing.isWest) {
				direction = "East";
				x--;
			}
			var doorSearchName = DungeonBuilder.NameFromProperites(x, y, direction, "Door");
			var secretSearchName = DungeonBuilder.NameFromProperites(x, y, direction, "Secret");
			var gateSearchName = DungeonBuilder.NameFromProperites(x, y, direction, "Gate");
			return GameObject.Find(doorSearchName) ?? GameObject.Find(secretSearchName) ?? GameObject.Find(gateSearchName);
		}

		private void openDoor(GameObject door)
		{
			if (door == null)
				return;

			var script = door.GetComponent<DoorScript>();

			if (script != null)
				script.HoldOpen = true;
		}

		/** Updates the explored map to reveal the tile the party is standing on. */
		private void updateExploredMap()
		{
			var origionalValue = PerceivedTile.Value;

			bool secretDiscovered = false;

			if (PerceivedTile != null) {

				var character = PerceptionCharacter;

				PerceivedTile.Explored = true;
				PerceivedTile.Water = CurrentTile.Water;
				PerceivedTile.Pit = CurrentTile.Pit;
				PerceivedTile.StairsUp = CurrentTile.StairsUp;
				PerceivedTile.StairsDown = CurrentTile.StairsDown;
				PerceivedTile.Teleporter = CurrentTile.Teleporter;
				PerceivedTile.Dirt = CurrentTile.Dirt;
				PerceivedTile.Rock = CurrentTile.Rock;
				PerceivedTile.Chute = CurrentTile.Chute;
				PerceivedTile.Grass = CurrentTile.Grass;

				bool blind = false;
				if (!blind) {

					// copy walls, but leave secrets alone
					for (int lp = 0; lp < 4; lp++) {
						WallRecord wall = new WallRecord();
						var actualWallRecord = CurrentTile.getWallRecord(lp);
						var perceivedWallRecord = PerceivedTile.getWallRecord(lp);

						if (actualWallRecord.Secret) {
							wall.Type = perceivedWallRecord.Secret ? WallType.Secret : WallType.Wall; 
						} else {
							wall.Type = actualWallRecord.Type;
						}

						if (actualWallRecord.Secret && !perceivedWallRecord.Secret)
						if (GameRules.PerceptionRoll(character, 15 + (Depth * 5))) {
							CoM.PostMessage("{0} discovered a secret door.", CoM.Format(character));	
							SoundManager.Play("OLDFX_FOUND");
							wall.Type = WallType.Secret;
							secretDiscovered = true;
						} 

						PerceivedTile.setWallRecord(lp, wall);
					}
				}

				if (PerceivedTile.GroundValue != CurrentTile.GroundValue) {
					// roll to see if we detect the difference or not 
					if (GameRules.PerceptionRoll(character, 10 + (Depth * 2))) {
						detectSquare(PerceivedTile, CurrentTile, "{0} detected that the party is standing on {1} square.", character);
					} else {
						if (GameRules.PerceptionRoll(character, (Depth)))
							CoM.PostMessage("{0} detects something strange about this place.", CoM.Format(character));	
					}
				}
			}

			bool tileChanged = origionalValue != PerceivedTile.Value || secretDiscovered;

			if (CoM.GraphicsLoaded && tileChanged)
				CoM.Instance.RefreshMapTile(LocationX, LocationY);
		}

		/** Checks if party is standing on any traps and applied changes. */
		private void checkTraps()
		{
			if (CurrentTile.Pit) {
				int numberOfCharactersWhoFell = 0;
				foreach (MDRCharacter character in this) {
					if (character.IsDead)
						continue;
					if (GameRules.FallRoll(character)) {
						int damage = GameRules.CalculatePitDamage(character);

						character.ReceiveDamage(damage);

						string message = character.IsDead ? "{0} fell down a pit, receiving {1} damage, and died." : "{0} fell down a pit receiving {1} damage.";
						CoM.PostMessage(message, CoM.Format(character), CoM.Format(damage));
						numberOfCharactersWhoFell++;
					} else {
						CoM.PostMessage("{0} avoided the pit.", CoM.Format(character));
					}
				}
				if (numberOfCharactersWhoFell > 0)
					SoundManager.Play("OLDFX_FALL");
			}

			if (CurrentTile.Rotator) {
				int turns = Util.Roll(4) + 4;

				DelayedDelegates.Add(delegate {
					for (int lp = 0; lp < turns; lp++)
						TurnLeft();
					CoM.PostMessage("Party hit a rotator.");
				}, 0.1f);
			}

			if (CurrentTile.Water) {
				SoundManager.Play("OLDFX_WADE");	
			} else {
				foreach (MDRCharacter character in this)
					character.TimeInWater = 0;
			}

			if (CurrentTile.FaceEast)
				Facing = Direction.EAST;
			if (CurrentTile.FaceWest)
				Facing = Direction.WEST;
			if (CurrentTile.FaceSouth)
				Facing = Direction.SOUTH;
			if (CurrentTile.FaceNorth)
				Facing = Direction.NORTH;

			if (CurrentTile.Chute) {
				ChuteTrapInfo chute = Map.GetChutAt(LocationX, LocationY);
				if (chute == null) {
					Trace.LogWarning("No chute found at party's current location.");
				} else {
					if ((Depth + chute.DropDepth) > Dungeon.Floors)
						CoM.PostMessage("This chute doesn't seem to go anywhere.  You can't help but feel fortunate.");
					else {

						int numberOfCharactersWhoFell = 0;
						foreach (MDRCharacter character in this) {
							if (character.IsDead)
								continue;
							if (GameRules.FallRoll(character))
								numberOfCharactersWhoFell++;
						}

						if (numberOfCharactersWhoFell == LivingMembers) {
							Depth += chute.DropDepth;
							CoM.PostMessage("Party fell down a chute");
							SoundManager.Play("OLDFX_FALL");
						} else {
							CoM.PostMessage("Party avoided falling down a chute");
						}
					}
				}
			}

			if (CurrentTile.Teleporter) {
				TeleportTrapInfo teleport = Map.GetTeleportAt(LocationX, LocationY);
				if (teleport == null) {
					Trace.LogWarning("No teleport found at party's current location.");
				} else {
					if (teleport.DestFloor > Dungeon.Floors) {	
						CoM.PostMessage("The teleport trap fizzes, but doesn't seem to do anything.");
						return;			
					}

					int destinationX = teleport.DestX;
					int destinationY = teleport.DestY;
					int destinationFloor = teleport.DestFloor == 0 ? Depth : teleport.DestFloor;

					if (teleport.IsRandom()) {
						int trys = 0;
						while (true) {
							destinationX = Util.Roll(30);
							destinationY = Util.Roll(30);
							if (!Map.GetField(destinationX, destinationY).Rock)
								break;
							trys++;
							if (trys > 999) {
								Trace.LogWarning("Could not find safe place to teleport character to.");
								return;
							}
						}
					}  

					SoundManager.Play("OLDFX_TELEPORT");

					SetLocation(destinationX, destinationY, destinationFloor, Facing);

					Trace.Log("Teleporting characters to " + destinationX + "," + destinationY);
					CoM.PostMessage("Party hit a teleporter");
				}
			}

		}

		private void processDepthUpdate()
		{
			if (OnDepthChanged != null)
				OnDepthChanged();				
		}

		private void processFacingUpdate()
		{
			bool canSeeTileAhead = FacingWall.IsEmpty;

			if (canSeeTileAhead) {
				if (GameRules.PerceptionRoll(PerceptionCharacter, 12 + (Depth * 3))) {
					if (detectSquare(PerceivedDestinationField, DestinationField, "{0} detected {1} ahead of the party.", PerceptionCharacter))
					if (CoM.GraphicsLoaded)
						CoM.Instance.RefreshMapTile(LocationX, LocationY);
				}
			}

			if (OnTurned != null)
				OnTurned();
		}

		/** This needs to be called whenever the character moves, processes things like traps etc */
		private void processLocationUpdate()
		{
			checkTraps();

			updateExploredMap();

			CameraHeight = CurrentTile.FloorHeight;

			if (OnMoved != null)
				OnMoved();
		}

		#region IEnumerable implementation

		public IEnumerator GetEnumerator()
		{
			return GetMembers().GetEnumerator();
		}

		#endregion

		#region implemented abstract members of DataObject

		/** Read object from XML */
		override public void ReadNode(XElement node)
		{
			base.ReadNode(node);

			Util.Assert(CoM.CharacterList != null, "Characters must be loaded before parties");

			characters = new MDRCharacter[MAX_PARTY_SIZE];

			var loadedCharacters = ReadNamedDataObjectList<MDRCharacter>(node, "Members", FieldReferenceType.ID, CoM.CharacterList);

			if (loadedCharacters.Count > characters.Length)
				throw new Exception(string.Format("Too many characters in party, was expecting a maximum of {0}, but found {1}", characters.Length, loadedCharacters.Count));			

			for (int lp = 0; lp < loadedCharacters.Count; lp++)
				characters[lp] = loadedCharacters[lp];			

			foreach (MDRCharacter character in characters) {
				if (character != null)
					character.Party = this;
			}

			_locationX = ReadInt(node, "LocationX", 11);
			_locationY = ReadInt(node, "LocationY", 7);
			_depth = ReadInt(node, "Depth", 0);
			_facing = ReadInt(node, "Facing", (int)Direction.NORTH);
		}

		/** Writes object to XML */
		override public void WriteNode(XElement node)
		{
			base.WriteNode(node);

			var membersList = new List<MDRCharacter>();
			for (int lp = 0; lp < characters.Length; lp++)
				membersList.Add(characters[lp]);

			WriteValue(node, "Members", membersList, FieldReferenceType.ID);
			WriteValue(node, "LocationX", LocationX);
			WriteValue(node, "LocationY", LocationY);
			WriteValue(node, "Depth", Depth);
			WriteValue(node, "Facing", (int)Facing);
		}


		#endregion

		public override string ToString()
		{
			var names = "";
			for (int lp = 0; lp < MAX_PARTY_SIZE; lp++) {
				if (this[lp] != null)
					names += this[lp].Name + ",";
			}
			names = names.TrimEnd(',');
			return "[" + names + "]";
		}

	}
}