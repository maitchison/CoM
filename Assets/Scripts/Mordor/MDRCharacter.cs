
using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Data;
using Mordor;

using UnityEngine;

namespace Mordor
{
	/** A singleton library containing information about each Character */
	[DataObject("CharacterLibrary")]
	public class MDRCharacterLibrary : DataLibrary<MDRCharacter>
	{
	}

	/** Stores characters items */

	/// <summary>
	/// Records information about a Mordor Character
	/// </summary>
	[DataObject("Character")]
	public class MDRCharacter : MDRActor
	{
		public static int INVENTORY_SLOTS = 42;

		/** Occurs when any of the characters attributes change (i.e. stats, gold, level etc).  Position and depth are excluded however. */
		public event GameTrigger OnChanged;

		/** The characters race */
		public MDRRace Race;
        
		/** The characters gender */
		public MDRGender Gender;

		/** Membership information for the currently joined guild */
		public MDRGuildMembership CurrentMembership { get { return Membership.Current; } }

		/** Guild information for the currently joined guild */
		public MDRGuild CurrentGuild { get { return Membership.CurrentGuild; } }

		/** The membership this character has in each guild */
		public MDRGuildMembershipLibrary Membership;

		/** The maximum base value for each stat */
		public MDRStats MaxStats { get { return Race.MaxStats + new MDRStats(5); } }

		/** Characters natural stats (i.e. without equipment) */
		public MDRStats BaseStats;

		public string PronounThirdPerson { get { return Gender == MDRGender.Female ? "her" : "him"; } }

		public string Pronoun { get { return Gender == MDRGender.Female ? "he" : "she"; } }

		// ----------------------------------
		// these varaibles are calculated via RecalculateStats
		// ----------------------------------

		// Current skill levels arranged by ID.
		public float[] SkillLevel;

		/** Bonus to stats from items */
		public MDRStats ItemStats;

		/** Total armour received by equiped items. */
		public int ItemArmour;

		/** Bonus to hit chance from equiped items. */
		public int ItemHitBonus;

		/** If the character is levitating or not */
		public bool Levitating;

		/** Playtime in seconds */
		public int PlayTime;

		public int MonstersKilled;
		public int Deaths;

		/** Number of physical swings, from abilities and from weapon */
		public int Swings {
			get {
				return (int)GameRules.AdditonalSwings(this) + Equiped.Weapon.Swings;
			}
		}

		// ----------------------------------

		public MDRInventory Inventory;

		/** Items stored in the bank */
		public MDRInventory BankItems;

		/** List of 10 user defined actions player can perform */
		public MDRActionList Buffers;

		public MDREquiped Equiped;

		/** This characters currently selected action, to be peformed next round. */
		public MDRAction CurrentAction;

		public int TotalXP { get { return Membership.TotalXP; } }

		public double Age;

		public CharacterStatus Status;

		public float PoisonLevel;
		public float DiseaseLevel;
		public float ParalysisLevel;
		        
		public int GoldInHand;
		public int GoldInBank;

		//STUB: todo, return the real party
		[FieldAttrAttribute(true)]
		internal MDRParty Party;

		/** Characters current location.*/
		public int LocationX { get { return Party == null ? 0 : Party.LocationX; } }

		/** Characters current location.*/
		public int LocationY { get { return Party == null ? 0 : Party.LocationY; } }

		/** Characters current depth.*/
		public int Depth { get { return Party == null ? 0 : Party.Depth; } }

		/** Characters current facing direction.*/
		public Direction Facing { get { return Party == null ? Direction.NORTH : Party.Facing; } }

		public float TimeInWater;

		private int _depth;

		/** The index to the stack of monsters this character is targeting */
		public int target;

		public int actionsThisRound;

		/** Index of portrait to use for this character */
		public int PortraitID;

		/** If true notifications on this characters death will be ignored. */
		public bool disableDeathMessages = false;

		// -----------------------------------------------------------------------
		// DEPRECIATED

		/** DEPRECIATED, use Party.LocationX */
		public int _LocationX;
		/** DEPRECIATED, use Party.LocationY */
		public int _LocationY;
		/** DEPRECIATED, use Party.Depth */
		public int _Depth;
		/** DEPRECIATED, use Party.Facing */
		public Direction _Facing;

		/** DEPRECIATED A list of IDs for character currently following this character */
		[FieldAttrAttribute("Followers")]
		public int[] __Followers;

		// -----------------------------------------------------------------------
		// Actor overrides

		override public Sprite Portrait { get { return (PortraitID >= 0) ? CoM.Instance.Portraits.ByID(PortraitID).Sprite : null; } }

		override public float NominalDamage { get { return Equiped.Weapon.Damage; } }

		override public int CombatLevel { get { return Membership.MaxLevel; } }

		override public int TotalArmour { get { return ItemArmour; } }

		override public float TotalCriticalHitChance { get { return CriticalHitSkill + Equiped.Weapon.CriticalModifier; } }

		override public float TotalBackstabChance {
			get { 
				float dexMod = Stats.Dex - 12;
				if (dexMod < 0)
					dexMod *= 0.25f;
				if (dexMod > BackstabSkill / 2f)
					dexMod = BackstabSkill / 2f;
				float chance = BackstabSkill + dexMod;  
				return Util.Clamp(chance, 0f, 99f) + Equiped.Weapon.BackstabModifier;
			} 
		}

		override public ActionType ActionThisRound { get { return CurrentAction.Type; } }

		override public float TotalArmourPierce { get { return Equiped.Weapon.Pierce; } }

		override public float TotalHitBonus { get { return ItemHitBonus; } }

		// -----------------------------------------------------------------------

		/** Creates a new character with some base stats, adds them to the character list */
		public static MDRCharacter Create(string name, bool autoAdd = false)
		{
			MDRGuild guild = CoM.Guilds[0];
			if (guild == null)
				throw new Exception("Guilds must be loaded before a character can be created.");

			MDRCharacter result = new MDRCharacter();

			result.Name = name;
			result.ID = CoM.CharacterList.NextID();

			// some basic stats, these should be overwritten later on 
			result.MaxHits = result.Hits = 15;
			result.MaxSpells = result.Spells = 100;

			result.BaseStats.Str = 12;
			result.BaseStats.Con = 10;
			result.BaseStats.Dex = 12;
			result.BaseStats.Int = 10;
			result.BaseStats.Wis = 10;
			result.BaseStats.Chr = 8;
			
			result.MaxStats.Str = 20;
			result.MaxStats.Con = 20;
			result.MaxStats.Dex = 20;
			result.MaxStats.Int = 20;
			result.MaxStats.Wis = 20;
			result.MaxStats.Chr = 20;

			result.Buffers.Reset();

			// just for now a random portrait
			result.PortraitID = Util.Roll(10);

			result.ApplyChanges();

			result.Membership[guild].Join();
			result.Membership.CurrentGuild = guild;

			if (autoAdd)
				CoM.CharacterList.Add(result);

			return result;
		}

		/** Creates a new character with the given name.  Stats will be all 0 */
		public MDRCharacter()
		{
			SkillLevel = new float[32];

			Race = CoM.Races["Human"];

			BaseStats = new MDRStats();
			Stats = new MDRStats();

			Inventory = new MDRInventory(INVENTORY_SLOTS, this);
			Equiped = MDREquiped.Create(this);
			BankItems = new MDRInventory(INVENTORY_SLOTS, this);

			CurrentAction = MDRAction.Empty;
		
			Membership = new MDRGuildMembershipLibrary();

			__Followers = new int[]{ };
			Buffers = new MDRActionList(10);

			Resistance = new MDRResistance();

			KnownSpells = new List<MDRSpell>();

			ApplyChanges();
		}

		/** Returns character skill by name */
		public float getSkill(string name)
		{
			var skill = CoM.Skills[name];
			if (skill == null)
				throw new Exception(string.Format("No skill named '{0}'", name));
			return SkillLevel[skill.ID];
		}

		public float CriticalHitSkill {
			get { return SkillLevel[16]; }
		}

		public float BackstabSkill {
			get { return SkillLevel[15]; }
		}

		public float MultipleSwingsSkill {
			get { return SkillLevel[17]; }
		}

		public float PerceptionSkill {
			get { return SkillLevel[29]; }
		}

		/** returns if this character is skilled enough to use given item */
		public bool HasSkillToUseItem(MDRItem item)
		{
			if (item.Type.SkillRequired == null)
				return true;
			return (SkillLevel[item.Type.SkillRequired.ID] >= item.SkillLevel);
		}

		/** Sets current aciton to default action. */
		public void DefaultAction()
		{
			CurrentAction = Buffers.Default;
		}

		/** Sets the characters max hits and spells to the default amount for their race and stats. */
		public void SetBaseHitsAndSpells()
		{
			MaxHits = Race.StartingHP + Util.Roll(5, true) + Util.ClampInt((Stats.Con - 14) * 2, 0, 99);
			MaxSpells = Race.StartingSP + Util.Roll(5, true) + Util.ClampInt((Stats.Int - 14) * 2, 0, 99);
			FullRestore();
		}

		/** Cures player of poison */
		public void CurePoison()
		{
			PoisonLevel = 0;
			if ((!Poisoned) && (!Diseased))
				SoundManager.Play("OLDFX_BETTER");
		}

		/** Curses player of disease */
		public void CureDisease()
		{
			DiseaseLevel = 0;
			if ((!Poisoned) && (!Diseased))
				SoundManager.Play("OLDFX_BETTER");
		}

		/** Updates effects on character, such as poison and disease */
		public void Update()
		{
			if (IsDead)
				return;

			float reduction = (1f + Stats.Con / 10f) * 0.25f;

			if (Poisoned) {
				PoisonLevel = Math.Max(PoisonLevel - reduction, 0f);
				ReceiveDamage(1 + (int)(Math.Log(1f + PoisonLevel) / 6f));
				if (PoisonLevel <= 0)
					CurePoison();
			}
			if (Diseased) {
				DiseaseLevel = Math.Max(DiseaseLevel - reduction, 0f);
				ReceiveDamage(1 + (int)(Math.Log(1f + DiseaseLevel) / 30f));
				if (DiseaseLevel <= 0)
					CureDisease();
			}
			if (Paralized) {
				ParalysisLevel = Math.Max(ParalysisLevel - reduction, 0f);
			}
		}

		public void ModifyStats(int statToDecease, int mod)
		{
			var statMod = new MDRStats(0);
			statMod[statToDecease] = mod;
			ModifyStats(statMod);
		}

		/** Modifies player stats by given amount.  Clamps stats to min/max and creates notifications. */
		public void ModifyStats(MDRStats statMod)
		{
			for (int lp = 0; lp < 6; lp++) {
				int previousStat = BaseStats[lp];
				BaseStats[lp] = Util.ClampInt(BaseStats[lp] + statMod[lp], 3, MaxStats[lp]);
				if (BaseStats[lp] > previousStat)
					Engine.PostNotification(
						string.Format("{0} increased to {1}!", Util.Colorise(MDRStats.LONG_STAT_NAME[lp], Color.yellow), Util.Colorise(BaseStats[lp], new Color(0.5f, 1f, 0.5f))), Portrait);
				if (BaseStats[lp] < previousStat)
					Engine.PostNotification(
						string.Format("{0} decreased to {1}!", Util.Colorise(MDRStats.LONG_STAT_NAME[lp], Color.yellow), Util.Colorise(BaseStats[lp], new Color(1f, 0.6f, 0.4f))), Portrait, false);
			}
			ApplyChanges();
		}

		/** Applies poison to character, will be accumulated with current level */
		public void ApplyPoison(int level)
		{
			PoisonLevel = (float)Math.Sqrt(PoisonLevel * PoisonLevel + level * level);
		}

		/** Applies disease to character, will be accumulated with current level */
		public void ApplyDisease(int level)
		{
			DiseaseLevel = (float)Math.Sqrt(DiseaseLevel * DiseaseLevel + level * level);
		}

		/** Applies paralysis to character, will be accumulated with current level */
		public void ApplyParalysis(int level)
		{
			ParalysisLevel = (float)Math.Sqrt(ParalysisLevel * ParalysisLevel + level * level);
		}

		/** Checks if character has the two basic actions (fight, and defend).  If they don't have them we create them. */
		public void ConfirmBasicActions()
		{
			// confirm we have the two basic actions, fight and defend
			if (!Buffers.Contains(MDRAction.Fight, true) || !Buffers.Contains(MDRAction.Defend, true))
				Buffers.Reset();
		}

		// ----------------------------------------
		// Status
		// ----------------------------------------

		public bool Paralized 
		{ get { return ParalysisLevel > 0; } }

		public bool Poisoned
		{ get { return PoisonLevel > 0; } }

		public bool Diseased
		{ get { return DiseaseLevel > 0; } }

		// ----------------------------------------

		/** 
		 * Causes character to recieve the given item into their inventory, returns false if free slots are avalaible 
		 */
		public bool GiveItem(MDRItemInstance item)
		{
			var slot = Inventory.ReceiveItem(item);
			if (slot != null) {
				IdentifyItem(item);
				if ((item.Cursed) && (item.Item.CurseType == ItemCurseType.AutoEquipCursed)) {
					var message = EquipItem(slot) ? "{1} attaches itself to {0}!" : "{1} tries to attach itself to {0} but fails.";
					CoM.PostMessage(message, this, item);					
				} else
					CoM.PostMessage("{0} receives {1}.", this, item);
				return true;
			} else
				return false;
		}

		/** 
		 * Unequips the given item.  If item is cursed unequip will fail.
		 * 
		 * @param Force If true item will be unequiped even if it is cursed, and even if there is no space for in 
		 * in the inventory.
		 */
		public bool UnequipItem(MDRItemSlot slot, bool force = false)
		{
			if (slot.IsEmpty)
				return true;

			if (slot.ItemInstance.Cursed && !force)
				return false;

			var freeSlot = Inventory.NextFreeSlot();

			if (freeSlot == null && !force)
				return false;

			if (freeSlot != null)
				freeSlot.ItemInstance = slot.ItemInstance;

			slot.ItemInstance = null;

			return true;
		}

		/** 
		 * Causes character to equip the item contained in the source contianer. 
		 * If there is a currently equiped item it will be returned to the inventory. 
		 * Will fail if item can not be equiped by this character.
		 * 		 
		 * @param source the container containing the item to equip
		 * @returns true if the item was equiped, false if it wasn't.
		 */
		public bool EquipItem(MDRItemSlot source)
		{
			MDRItemSlot equipSlot = null;

			// make sure we can use it
			if (!source.ItemInstance.CanBeEquipedBy(this))
				return false;

			if (!source.ItemInstance.IDLevel.CanUse)
				return false;

			var location = source.ItemInstance.Item.Type.TypeClass.Location;

			if (equipSlot == null)
				equipSlot = Equiped.NextFreeSlot(location);

			if (equipSlot == null)
				equipSlot = Equiped.AvailableSlot(location);
				
			// no free slot 
			if (equipSlot == null)
				return false;

			// perform the swap
			MDRItemInstance previousItem = equipSlot.ItemInstance;
			equipSlot.ItemInstance = source.ItemInstance;
			source.ItemInstance = previousItem;

			// update cursed
			equipSlot.ItemInstance.KnownToBeCursed = (equipSlot.ItemInstance.Item.CurseType != ItemCurseType.None);

			CoM.PostMessage(this + " equiped " + equipSlot.ItemInstance.Item.Name);

			return true;

		}

		private void RecalculateSkills()
		{			
			for (int lp = 0; lp < SkillLevel.Length; lp++) {
				var skill = CoM.Skills.ByID(lp);
				if (skill != null)
					SkillLevel[lp] = GameRules.CharacterGeneralAbility(this, skill);
			}
		}

		/** Calculates characters resistances */
		private void RecalculateResistances()
		{
			for (int lp = 0; lp < MDRResistance.Count; lp++)
				Resistance[lp] = (Race == null) ? 0 : Race.Resistance[lp];

			foreach (MDRItemSlot slot in Equiped) {
				if (!slot.IsEmpty)
					Resistance.Combine(slot.ItemInstance.Item.Resistance);
			}

		}

		/** 
		 * Recalculates Attack/Defense and stats gained from equiped items 
		 */
		private void RecalculateItemStats()
		{
			// add equipment bonus
			int itemArmour = 0;
			int itemBonusHit = 0;
			Levitating = false;
			MDRStats itemStats = new MDRStats();
			foreach (MDRItemSlot equiped in Equiped) {
				if (equiped.ItemInstance != null) {
					itemStats += equiped.ItemInstance.Item.StatsMod;
					itemArmour += equiped.ItemInstance.Item.Armour;
					itemBonusHit += equiped.ItemInstance.Item.Hit;
					Levitating = (Levitating || equiped.ItemInstance.Item.Abilities.Levitate);
				}
			}
			ItemArmour = itemArmour;
			ItemHitBonus = itemBonusHit;
			ItemStats = itemStats;
		}

		/** Calculates the characters stats based on equiped items, guild etc. This also includes MaxSpells. */
		public void ApplyChanges()
		{			
			RecalculateItemStats();
			RecalculateResistances();
			RecalculateSkills();

			Stats = BaseStats + ItemStats;

			CoM.GameStats.CheckCharacterRecords(this);

			if (OnChanged != null)
				OnChanged();
		}

		/**
		 * Assigns character the spells based on their current guild levels 
		 */
		public void RecalculateSpells()
		{
			KnownSpells = CalculateKnownSpells();
		}

		/**
		 * Returns if this character can cast given spell or not 
		 */
		override public bool CanCast(MDRSpell spell)
		{
			return ((Settings.Advanced.WhiteWizard) || (SkillLevel[spell.SpellClass.Skill.ID] >= spell.SpellLevel));
		}

		/**
		 * Returns if this character can cast any spells from given class or not 
		 */
		public bool KnowsAnySpellsFromSpellClass(MDRSpellClass spellclass)
		{
			foreach (MDRSpell spell in CoM.Spells) {
				if ((spell.SpellClass == spellclass) && (CanCast(spell)))
					return true;
			}	
			return false;
		}

		/** Gives gold to player */
		public void CreditGold(int amount, bool directToBank = false)
		{
			if (directToBank)
				GoldInBank += amount;
			else
				GoldInHand += amount;
			if (OnChanged != null)
				OnChanged();
		}

		/** Removes gold from player, or players bank.  Returns true if sucessful. */
		public bool DebitPersonalGold(int amount)
		{
			if (PersonalGold >= amount) {
				int cashAmount = (int)Math.Min(amount, GoldInHand);
				amount -= cashAmount;
				GoldInHand -= cashAmount;

				GoldInBank -= amount;

				if (OnChanged != null)
					OnChanged();

				return true;
			}

			return false;
		}

		/** Players level in their current guild. */
		public int CurrentLevel {
			get { return CurrentMembership.CurrentLevel; }
		}

		/** Highest level of any guilds. */
		public int HighestLevel {
			get {
				int highestLevel = 0;
				foreach (MDRGuild guild in CoM.Guilds) {
					if (Membership[guild].IsMember && Membership[guild].CurrentLevel > highestLevel)
						highestLevel = Membership[guild].CurrentLevel;
				}
				return highestLevel;
			} 
		}

		/** Clears any negative effects, and restores health and spells to maximum */
		public void FullRestore()
		{
			PoisonLevel = 0;
			DiseaseLevel = 0;
			ParalysisLevel = 0;

			Hits = MaxHits;
			Spells = MaxSpells;
		}

		public override void GainXP(int xp)
		{
			if (!CurrentMembership.IsPinned) {
				CurrentMembership.XP += xp;
				if (CurrentMembership.IsPinned)
					Engine.PostNotification(Name + " is pinned", Portrait);
			}
		}

		/** 
		 * Causes character to gain a level in their current guild. 
		 * Character must have the necessary XP
		 */
		public void GainLevel()
		{
			if (CurrentMembership.ReqXP > 0)
				return;

			// increase MaxHP
			if (CurrentMembership.CurrentLevel == Membership.MaxLevel) {
				int gainedHP;
				int gainedSP;
				if (CurrentMembership.CurrentLevel <= Membership.CurrentGuild.MaxLevel) {
					int conBonus = Math.Max(BaseStats.Con - 16, 0);
					int intBonus = Math.Max(BaseStats.Int - 16, 0);
					gainedHP = (Membership.CurrentGuild.AvHitsPerLevel / 2) + Util.Roll(Membership.CurrentGuild.AvHitsPerLevel) + conBonus;
					gainedSP = (Membership.CurrentGuild.AvSpellsPerLevel / 2) + Util.Roll(Membership.CurrentGuild.AvSpellsPerLevel) + intBonus;
				} else {
					gainedHP = Membership.CurrentGuild.HitsAtMaxLevel;
					gainedSP = Membership.CurrentGuild.SpellsAtMaxLevel;
				}
				MaxHits += gainedHP;

				MaxSpells += gainedSP;
			} 

			FullRestore();

			// level in guild
			CurrentMembership.CurrentLevel++;

			ApplyChanges();

			CheckForNewSpells();

			CoM.PostMessage("Welcome to level " + CurrentMembership.CurrentLevel + " " + Util.Colorise(Name, Color.green));
		}

		/** 
		 * Updates the players list of known spells based on guild levels.
		 * Also notifies player of any new spells added.
		 * Returns true if new spells where discovered, false if not */
		public bool CheckForNewSpells()
		{
			bool newSpell = false;
			List<MDRSpell> newSpells = CalculateKnownSpells();
			foreach (MDRSpell spell in newSpells) {
				if (!KnownSpells.Contains(spell)) {
					newSpell = true;
					KnownSpells.Add(spell);
					CoM.PostMessage(this + " learned " + Util.Colorise(spell.ToString(), Color.green) + " in the school " + Util.Colorise(spell.SpellClass.ToString(), Color.cyan));
					CoM.PostNotification(this + " learned " + Util.Colorise(spell.ToString(), Color.green) + " in the school " + Util.Colorise(spell.SpellClass.ToString(), Color.cyan), this.Portrait);
				}
			}
			return newSpell;
		}

		/** 
		 * Attempts to have player join given guild. 
		 * 
		 * @param guild The guild to join
		 * @param error A string describing why the player was not able to join the guild, if the join was unsucessful
		 * @return If this guild was sucessfuly joined 
		 */
		public bool JoinGuild(MDRGuild guild)
		{
			if (!guild.CanAccept(this))
				return false;

			// set guild
			Membership[guild].Join();
			Membership.CurrentGuild = guild;

			// unequip
			UnequipInvalidItems();

			if (OnChanged != null)
				OnChanged();

			CheckForNewSpells();

			return true;
		}

		/** Looks for and removes and usable items with no remaining charges */
		public void RemoveUsedItems()
		{
			for (int lp = 0; lp < Inventory.Count; lp++) {
				MDRItemSlot slot = Inventory[lp];
				if ((slot.ItemInstance != null) && slot.ItemInstance.Item.Usable && (slot.ItemInstance.RemainingCharges == 0)) {
					slot.ItemInstance = null;
				}
			}

		}

		/** Unequips any items where requirements are no longer met, returns a list of the items unequiped. */
		public List<MDRItem> UnequipInvalidItems()
		{
			List<MDRItem> unequipedItems = new List<MDRItem>();

			for (int lp = 0; lp < Equiped.Count; lp++) {
				MDRItemSlot slot = Equiped[lp];
				if ((slot.ItemInstance != null) && !slot.ItemInstance.CanBeEquipedBy(this)) {
					unequipedItems.Add(slot.ItemInstance.Item);

					// find free slot
					MDRItemSlot freeSlot = Inventory.NextFreeSlot();
					if (freeSlot == null) {
						CoM.PostMessage("No free inventory slots to unequip item.");
						return unequipedItems;
					}
					freeSlot.ItemInstance = slot.ItemInstance;
					slot.ItemInstance = null;
					CoM.PostMessage(this + " can no longer use " + freeSlot.ItemInstance.Name + ".");
				}
			}


			if ((unequipedItems.Count > 0) && (OnChanged != null)) {
				ApplyChanges();
				OnChanged();
			}

			return unequipedItems;
		}

		/** Damages the actor given amount.  Caps at 0 health.  Returns number of actual hits done. */
		override public int ReceiveDamage(DamageInfo damage)
		{
			var damageReceived = base.ReceiveDamage(damage);

			if (damageReceived == 0)
				return 0;

			if (Hits == 0)
			//if (damage.Source is MDRMonsterStack)
			//	CoM.GameStats.RegisterCharacterKilledByMonster((damage.Source as MDRMonsterStack).Monster, this);

			if (OnChanged != null)
				OnChanged();

			return damageReceived;
		}

		/** True if this character is currently in combat */
		public bool InCombat {
			get {
				//todo: surely there is a cleaner way of doing this 
				return CoM.Instance.DungeonState.InCombat;
			}
		}

		/**
		 * Causes player to die 
		 */
		override public void Die()
		{
			base.Die();
			if (!disableDeathMessages) {
				SoundManager.Play(DieSFXName, 4f);
				CoM.PostMessage(this + " has died.");
				CoM.GameStats.RegisterCharactersDeath(this);
			}
		}

		/** 
		 * If this player is in the town or not 
		 */
		public bool IsInTown {
			get { return Party.IsInTown; }
		}

		/**
		 * Causes player to rest till fully healed 
		 */
		protected void RestUp()
		{
			if (IsDead)
				return;
			if (Hits > MaxHits)
				Hits = MaxHits;
			int hitsToHeal = MaxHits - Hits;
			int daysToRest = GameRules.HealingTime(hitsToHeal, this);
			Age += daysToRest / 365f;

			FullRestore();

			if (OnChanged != null)
				OnChanged();

			if (daysToRest >= 1)
				CoM.PostMessage(this + " has rested.");
		}

		/** 
		 * Sets the depth of the character, if set to zero will apply the 'entering town' logic
		 */
		private void setDepth(int value)
		{
			if (_depth == value)
				return;
			_depth = value;
			if (Depth == 0)
				RestUp();
		}

		public override int SpellPower(MDRSpell spell)
		{
			return (int)SkillLevel[spell.SpellClass.Skill.ID];
		}

		/**
		 * Character will attempt to identify the monster.
		 * 
		 * @param identifySkillModifier.  Amount to multiply characters ID skill by.  Defaults to 1.
		 * @returns if the stacks ID level was improved or not 
		 */
		/*
		public bool IdentifyMonster(MDRMonsterInstance stack, float identifySkillModifier = 1.0f)
		{
			if (stack == null || stack.Monster == null)
				return false;

			float identificationlevel = GameRules.CharacterIndentifyingAbility(this);
			float monsterDifficulty = stack.Monster.IdDifficulty;

			IdentificationLevel newLevel = IdentificationLevel.None;

			if (identificationlevel / 2 >= monsterDifficulty / 3 - 0.5f)
				newLevel = IdentificationLevel.Partial;

			if (identificationlevel / 3 >= monsterDifficulty / 3 - 0.25f)
				newLevel = IdentificationLevel.Mostly;

			if (identificationlevel / 4 >= monsterDifficulty / 3)
				newLevel = IdentificationLevel.Full;

			if (newLevel > stack.IDLevel) {
				CoM.GameStats.RegisterMonsterIdentified(stack.Monster, newLevel);

				string idVerb = newLevel.Name;
				var oldLevel = stack.IDLevel;
				stack.IDLevel = newLevel;
				string newMonsterString = CoM.Format(stack);
				if (oldLevel != IdentificationLevel.Auto)
					CoM.PostMessage("{0} {1} identifies {2}", this, idVerb, newMonsterString);
				return true;
			}
			return false;
		}
		*/

		/**
		 * Character will attempt to identify item.  If they sucessed in improving the identification level the
		 * ID level for the given item will be modified, and a MessageLog entry will be generated.
		 * 
		 * Returns if the item's identificaiton was improved or not.
		 */
		public bool IdentifyItem(MDRItemInstance instance)
		{
			if (instance.Item == null)
				return false;

			float identificationlevel = GameRules.CharacterIndentifyingAbility(this);
			float itemDifficulty = instance.Item.IdDifficutly;

			IdentificationLevel newLevel = IdentificationLevel.None;

			if (identificationlevel / 2 >= itemDifficulty / 3 - 0.5f)
				newLevel = IdentificationLevel.Partial;

			if (identificationlevel / 3 >= itemDifficulty / 3 - 0.25f)
				newLevel = IdentificationLevel.Mostly;
		
			if (identificationlevel / 4 >= itemDifficulty / 3)
				newLevel = IdentificationLevel.Full;
				
			if (newLevel > instance.IDLevel) {

				string idVerb = newLevel.Name;

				var oldLevel = instance.IDLevel;
				instance.IDLevel = newLevel;
				string newItemString = CoM.Format(instance);
				if (oldLevel != IdentificationLevel.Auto)
					CoM.PostMessage("{0} {1} identifies {2}", this, idVerb, newItemString);

				CoM.GameStats.RegisterItemIdentified(instance.Item, newLevel);

				return true;
			}
			return false;
		}

		/**
		 * Players total amount of gold (bank + in hand)
		 */
		public int PersonalGold {
			get { return (GoldInHand + GoldInBank); }
		}

		/**
		 * Returns if the character can read or not */
		public bool CanRead {
			get { return (BaseStats.Int >= 10 && BaseStats.Wis >= 10); } 
		}

		public override string HurtSFXName {
			get {
				switch (Gender) {
					case MDRGender.Female:
						return "fpain";
					case MDRGender.Male:
						return "mpain";
					default:
						return "";
				}
			}
		}

		public override string DieSFXName {
			get {
				switch (Gender) {
					case MDRGender.Female:
						return "fdie";
					case MDRGender.Male:
						return "mdie";
					default:
						return "";
				}
			}
		}

		public override string AttackSFXName {
			get {
				return "swing";
			}
		}

		/** Read object from XML */
		override public void ReadNode(XElement node)
		{
			base.ReadNode(node);
		
			Race = CoM.Races[ReadValue(node, "Race")];
			Gender = ReadEnum<MDRGender>(node, "Gender", MDRGender.Male);

			BaseStats = ReadDataObject<MDRStats>(node, "BaseStats");

			Age = ReadFloat(node, "Age");
			PlayTime = ReadInt(node, "PlayTime");
			Deaths = ReadInt(node, "Deaths");
			MonstersKilled = ReadInt(node, "MonstersKilled");

			Hits = ReadInt(node, "Hits");
			MaxHits = ReadInt(node, "MaxHits");
			Spells = ReadInt(node, "Spells");
			MaxSpells = ReadInt(node, "MaxSpells");
			Status = ReadEnum<CharacterStatus>(node, "Status", CharacterStatus.Normal);

			PoisonLevel = ReadFloat(node, "PoisonLevel");
			DiseaseLevel = ReadFloat(node, "DiseaseLevel");
			ParalysisLevel = ReadFloat(node, "ParalysisLevel");

			TimeInWater = ReadFloat(node, "TimeInWater");

			GoldInHand = ReadInt(node, "Gold");
			GoldInBank = ReadInt(node, "GoldInBank");

			PortraitID = ReadInt(node, "PortraitID");

			Equiped.ReadNode(node.Element("Equiped"));
			Inventory.ReadNode(node.Element("Inventory"));
			BankItems.ReadNode(GetNode(node, "BankItems"));

			Buffers.ReadNode(GetNode(node, "Buffers"));

			Membership = ReadDataObject<MDRGuildMembershipLibrary>(node, "Membership");

			// Depreciated:
			__Followers = ReadArray<int>(node, "Followers");
			_LocationX = ReadInt(node, "LocationX");
			_LocationY = ReadInt(node, "LocationY");
			_Depth = ReadInt(node, "Depth");
			_Facing = ReadInt(node, "Facing");

			ConfirmBasicActions();

			ApplyChanges();
			UnequipInvalidItems();
			RemoveUsedItems();
			ApplyChanges();
			RecalculateSpells();
		}

		/** Writes object to XML */
		override public void WriteNode(XElement node)
		{			
			base.WriteNode(node);

			UnequipInvalidItems();
			RemoveUsedItems();

			WriteValue(node, "Race", Race.Name);
			WriteValue(node, "Gender", Gender);
			WriteValue(node, "BaseStats", BaseStats);
			WriteValue(node, "TotalXP", Membership.TotalXP);
			WriteValue(node, "Age", Age);
			WriteValue(node, "PlayTime", PlayTime);
			WriteValue(node, "Deaths", Deaths);
			WriteValue(node, "MonstersKilled", MonstersKilled);

			WriteValue(node, "Hits", Hits);
			WriteValue(node, "MaxHits", MaxHits);
			WriteValue(node, "Status", Status);

			WriteValue(node, "PoisonLevel", PoisonLevel);
			WriteValue(node, "DiseaseLevel", DiseaseLevel);
			WriteValue(node, "ParalysisLevel", ParalysisLevel);
			WriteValue(node, "TimeInWater", TimeInWater);

			WriteValue(node, "Spells", Spells);
			WriteValue(node, "MaxSpells", MaxSpells);

			WriteValue(node, "Gold", GoldInHand);
			WriteValue(node, "GoldInBank", GoldInBank);

			WriteValue(node, "PortraitID", PortraitID);

			WriteValue(node, "Buffers", Buffers);

			WriteValue(node, "Equiped", Equiped);
			WriteValue(node, "Inventory", Inventory);
			WriteValue(node, "BankItems", BankItems);

			WriteValue(node, "Membership", Membership);			

			// Depreciated:
			WriteValue(node, "DEPRECIATED", "---------------------");			
			WriteValue(node, "LocationX", _LocationX);
			WriteValue(node, "LocationY", _LocationY);
			WriteValue(node, "Depth", _Depth);
			WriteValue(node, "Facing", (int)_Facing);
			WriteValue(node, "Followers", __Followers);
		}

	}

	public enum CharacterStatus
	{
		Normal,
		Dead,
		Stoned,
		Rocked,
	}
}
