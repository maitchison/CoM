using System;
using System.Collections;
using System.Collections.Generic;

using Mordor;
using Data;

using UnityEngine;
using UI;

namespace Engines
{
	//	public enum CombatSoundEffect
	//	{
	//		Backstab,
	//		CriticalHit,
	//		Hit
	//	}
	//
	//	/** Statistics for combat during simulation mode. */
	//	public struct CombatStatsRecord
	//	{
	//		/** Number of sucessful party hits. */
	//		public int PartyHits;
	//		/** Number of party misses. */
	//		public int PartyMisses;
	//		/** Number of sucessful monster hits. */
	//		public int MonsterHits;
	//		/** Number of monster misses. */
	//		public int MonsterMisses;
	//
	//		/** Maximum amount of damage monster dealt to player. */
	//		public int MonsterMaxDamage;
	//		/** Maximum amount of damage player dealt to monster. */
	//		public int PartyMaxDamage;
	//
	//		public int Rounds;
	//
	//		public void Reset()
	//		{
	//			PartyHits = 0;
	//			PartyMisses = 0;
	//			MonsterHits = 0;
	//			MonsterMisses = 0;
	//			MonsterMaxDamage = 0;
	//			PartyMaxDamage = 0;
	//			Rounds = 0;
	//		}
	//
	//		public void RegisterPartyHit(int damage = 0)
	//		{
	//			if (damage > PartyMaxDamage)
	//				PartyMaxDamage = damage;
	//			PartyHits++;
	//		}
	//
	//		public void RegisterPartyMiss()
	//		{
	//			PartyMisses++;
	//		}
	//
	//		public void RegisterMonsterHit(int damage = 0)
	//		{
	//			if (damage > MonsterMaxDamage)
	//				MonsterMaxDamage = damage;
	//			MonsterHits++;
	//		}
	//
	//		public void RegisterMonsterMiss()
	//		{
	//			MonsterMisses++;
	//		}
	//
	//	}
	//
	//	/**
	//	 * Class to handle combat
	//  	 */
	//	public class CombatEngine
	//	{
	//		const float EXTRA_DELAY_BETWEEN_SELECTED_PARTY_MEMEBER_TURN = 0.75f;
	//		const float EXTRA_DELAY_BETWEEN_PARTY_MEMEBERS_TURN = 0.25f;
	//		const float DELAY_BETWEEN_SWINGS = 0.25f;
	//
	//		const float DELAY_BETWEEN_MONSTERS_ATTACK = 0.5f;
	//
	//		/** Maximum number of rounds during simulation before a draw is reached. */
	//		const int MAX_SIMULATION_ROUNDS = 100;
	//
	//		/** The most recent combat instance to be initialized. */
	//		public static CombatEngine CurrentInstance { get { return _currentInstance; } }
	//
	//		private static Color MONSTER_DAMAGE_COLOR = Color.red;
	//
	//		/** The combat log.  Lists player and monsters attacks etc. */
	//		public static List<MessageEntry> CombatLog = new List<MessageEntry>();
	//
	//		public static int CurrentID = 0;
	//
	//		public bool Active { get { return (Area != null); } }
	//
	//		/** IF true combat will run in simulation mode.  No XP gains, no combat delays, minimial messages */
	//		protected bool simulationMode = false;
	//
	//		protected MDRArea Area;
	//		protected MDRParty Party;
	//		private bool playerInitiative;
	//
	//		private int id;
	//		private int round;
	//		private int initialTarget;
	//
	//		/** The last party message to be posted. */
	//		public static MessageEntry LastPartyCombatMessage;
	//		/** The last monster message to be posted. */
	//		public static MessageEntry LastMonsterCombatMessage;
	//		/** The last notice to be posted. */
	//		public static MessageEntry LastCombatNotice;
	//
	//		/** Stats from simulated combat.*/
	//		public CombatStatsRecord CombatStats;
	//
	//		/**
	//		 * Defines the seed used for this combat.
	//		 * Just used for simulation at the moment.
	//		 */
	//		public int RandomSeed;
	//
	//		private static CombatEngine _currentInstance;
	//
	//		public int Round { get { return round; } }
	//
	//		protected void initalizeEngine(MDRParty newParty)
	//		{
	//			Party = newParty;
	//
	//			ClearCombat();
	//
	//			id = ++CurrentID;
	//
	//			_currentInstance = this;
	//
	//			newParty.ClearActions();
	//		}
	//
	//		/** If this combat is the current one or not. */
	//		public bool isCurrent
	//		{ get { return (Party.LivingMembers > 0) && (CurrentID == id); } }
	//
	//		/**
	//		 * Initializes combat between a party of heros and a group of monsters.
	//		 * Returns CombatEngine instance.
	//		 */
	//		public static CombatEngine StartCombat(MDRParty party, MDRArea area)
	//		{
	//			var instance = new CombatEngine();
	//
	//			instance.initalizeEngine(party);
	//
	//			if (area == null)
	//				return null;
	//
	//			if (area.Aggressive)
	//				party.DefaultActions();
	//
	//			// base monster identification
	//			foreach (var stack in area.Stack)
	//				party.Leader.IdentifyMonster(stack, 0.5f);
	//
	//			CoM.Instance.StartCoroutine(instance.processCombat(area));
	//
	//			return instance;
	//		}
	//
	//		/** To be called when party leaves combat, allows monsters a final strike in certian situations */
	//		public void LeaveCombat()
	//		{
	//			if ((Area != null) && (Area.Aggressive) && (Util.Roll(6) < 3))
	//				processMonsterGroup(Area);
	//
	//			ClearCombat();
	//		}
	//
	//		/** Initializes combat between a party of heros and a group of monsters. */
	//		public void SimulateCombat(MDRParty party, MDRArea area)
	//		{
	//			PostCombatDebugMessage("Simulation begins with seed  = " + RandomSeed);
	//
	//			initalizeEngine(party);
	//
	//			if (area == null)
	//				return;
	//
	//			if (area.Aggressive)
	//				party.DefaultActions();
	//
	//			int itterations = 0;
	//
	//			simulationMode = true;
	//			CoM.DisableAnalytics = true;
	//			CoM.GameStats.Enabled = false;
	//
	//			initializeCombat(area);
	//
	//			area.Aggressive = true;
	//
	//			while (party.TotalHP > 0 && area.TotalHP > 0 && itterations < MAX_SIMULATION_ROUNDS) {
	//				simulateRound();
	//				itterations++;
	//			}
	//
	//			CombatStats.Rounds = itterations;
	//
	//			CoM.GameStats.Enabled = true;
	//			CoM.DisableAnalytics = false;
	//			simulationMode = false;
	//		}
	//
	//		#region Messages and Formatting
	//
	//		/** Posts a combat message to the log such as "monster attacks" */
	//		private void PostCombatMessage(string message, params object[] args)
	//		{
	//			var messageEntry = new MessageEntry(String.Format(message, args));
	//			CombatLog.Add(messageEntry);
	//			LastPartyCombatMessage = messageEntry;
	//		}
	//
	//		/** Posts notice to the log, such as "party finds gold".  These are overlayed over the main window. */
	//		public void PostCombatNotice(string message, params object[] args)
	//		{
	//			var messageEntry = new MessageEntry(String.Format(message, args));
	//			CombatLog.Add(messageEntry);
	//			LastCombatNotice = messageEntry;
	//		}
	//
	//		/** Posts an informational message to the log.  These won't come up in area window. */
	//		private void PostInfoMessage(string message, params object[] args)
	//		{
	//			var messageEntry = new MessageEntry(String.Format(message, args));
	//			CombatLog.Add(messageEntry);
	//		}
	//
	//		/** Posts a combat debug message to the log.  These are grayed out */
	//		private void PostCombatDebugMessage(string message, params object[] args)
	//		{
	//			CombatLog.Add(new MessageEntry(String.Format(message, args)) { Color = new Color(0.5f, 0.5f, 0.5f) });
	//		}
	//
	//		/** Posts a player message to the log */
	//		public static void PostPlayerMessage(string message, params object[] args)
	//		{
	//			var messageEntry = new MessageEntry(String.Format(message, args));
	//			CombatLog.Add(messageEntry);
	//			LastPartyCombatMessage = messageEntry;
	//		}
	//
	//		/** Posts a monster message to the log */
	//		private void PostMonsterMessage(string message, params object[] args)
	//		{
	//			var messageEntry = new MessageEntry(String.Format(message, args)) { Color = new Color(1, 0.5f, 0.7f) };
	//			CombatLog.Add(messageEntry);
	//			LastMonsterCombatMessage = messageEntry;
	//		}
	//
	//		public static string FormatPlayerDamage(int damage)
	//		{
	//			return CoM.Format(damage);
	//		}
	//
	//		public static string FormatMonsterDamage(int damage)
	//		{
	//			return Util.Colorise(damage.ToString(), MONSTER_DAMAGE_COLOR);
	//		}
	//
	//		public static string Format(object value, int count = 1)
	//		{
	//			if (value is MDRMonster)
	//				return CoM.Format((MDRMonster)value, count);
	//			if (value is MDRMonsterStack)
	//				return CoM.Format((MDRMonsterStack)value, count);
	//			if (value is MDRCharacter)
	//				return CoM.Format((MDRCharacter)value);
	//			if (value is MDRSpell)
	//				return CoM.Format((MDRSpell)value);
	//			return value.ToString();
	//		}
	//
	//		#endregion
	//
	//		#region Combat
	//
	//		/** Sets up for combat */
	//		private void initializeCombat(MDRArea initialArea)
	//		{
	//			Util.Assert(initialArea != null, "Combat should not be initialized with a null area.");
	//
	//			Area = initialArea;
	//
	//			if (Area == null)
	//				return;
	//
	//			if (!Area.AreMonsters)
	//				return;
	//
	//			foreach (MDRCharacter character in Party) {
	//				character.target = -1;
	//			}
	//
	//			if (Area.Aggressive)
	//				Party.DefaultActions(false);
	//
	//			foreach (MDRCharacter character in Party) {
	//				character.target = 0;
	//			}
	//
	//			CombatStats.Reset();
	//
	//			UnityEngine.Random.seed = generateSeedForRound(-1);
	//
	//			playerInitiative = GameRules.CalculateInititive(Area.Stack[0].Monster, Party.Selected);
	//
	//		}
	//
	//		private int generateSeedForRound(int roundNumber)
	//		{
	//			return (int)(((long)RandomSeed + (roundNumber + 7) * 123) % int.MaxValue);
	//		}
	//
	//		/** Simulates a round of combat between party and monsters. */
	//		private void simulateRound()
	//		{
	//			UnityEngine.Random.seed = generateSeedForRound(round);
	//
	//			if (!playerInitiative)
	//				for (var lp = 0; lp < 4; lp++)
	//					if ((Area.Stack[lp] != null) && (!Area.Stack[lp].IsEliminated)) {
	//						processMonsterStack(Area.Stack[lp]);
	//					}
	//
	//			playerInitiative = false;
	//
	//			foreach (MDRCharacter character in Party) {
	//				character.actionsThisRound = 0;
	//				initialTarget = character.target;
	//				while (processCharactersAction(character)) {
	//
	//				}
	//				character.target = initialTarget;
	//				updateCharacterTarget(character, Area);
	//			}
	//			round++;
	//		}
	//
	//		/**
	//		 * Processes combat.  Unfortualy the stopcoroutine in Unity doesn't unless you run them from the monobehaviour
	//		 * with a string name.  Because of this I need to check if we changed areas, and if so exit the coroutine as
	//		 * a new one will already be spawned
	//		 */
	//		private IEnumerator processCombat(MDRArea initialArea)
	//		{
	//			initializeCombat(initialArea);
	//
	//			var combatDelay = 1.0f;
	//
	//			// If monsters are passive check if any characters have agressive actions.
	//			while (!Area.Aggressive) {
	//				if (Area.AreMonsters) {
	//					foreach (MDRCharacter character in Party)
	//						if (character.CurrentAction.IsAggressive) {
	//							combatDelay = 0.0f;
	//							playerInitiative = true;
	//							Area.Aggressive = true;
	//						}
	//				}
	//				yield return delayCombat();
	//				if (!isCurrent)
	//					yield break;
	//			}
	//
	//			yield return delayCombat(combatDelay);
	//
	//			PostInfoMessage("-------------------");
	//
	//			if ((Area == null) || (!Area.AreMonsters))
	//				yield break;
	//
	//			if (!isCurrent)
	//				yield break;
	//
	//			// Process the combat rounds.
	//			while (Area.AreMonsters) {
	//
	//				if (!playerInitiative)
	//					for (var lp = 0; lp < 4; lp++)
	//						if ((Area.Stack[lp] != null) && (!Area.Stack[lp].IsEliminated)) {
	//							processMonsterStack(Area.Stack[lp]);
	//							yield return delayCombat(DELAY_BETWEEN_MONSTERS_ATTACK);
	//							if (!isCurrent)
	//								yield break;
	//						}
	//
	//				playerInitiative = false;
	//
	//				foreach (MDRCharacter character in Party) {
	//					character.actionsThisRound = 0;
	//					initialTarget = character.target;
	//					while (processCharactersAction(character)) {
	//						yield return delayCombat(DELAY_BETWEEN_SWINGS);
	//						if (!isCurrent)
	//							yield break;
	//					}
	//					character.target = initialTarget;
	//					updateCharacterTarget(character, Area);
	//					yield return delayCombat(Party.Selected == character ? EXTRA_DELAY_BETWEEN_SELECTED_PARTY_MEMEBER_TURN : EXTRA_DELAY_BETWEEN_PARTY_MEMEBERS_TURN);
	//					if (!isCurrent)
	//						yield break;
	//				}
	//
	//				round++;
	//
	//				if (!isCurrent)
	//					yield break;
	//			}
	//
	//			CombatComplete();
	//
	//		}
	//
	//		/** Stops combat */
	//		public void ClearCombat()
	//		{
	//			Party.ClearActions();
	//
	//			Area = null;
	//
	//			CurrentID++;
	//		}
	//
	//		/** Returns true if we are currently in combat */
	//		public bool IsInCombat {
	//			get {
	//				if (Area == null)
	//					return false;
	//				return (Area.Aggressive && Area.AreMonsters);
	//			}
	//		}
	//
	//		/** Occurs when all monsters have been defeated - give players gold */
	//		private void CombatComplete()
	//		{
	//			if (Area.TreasureContainerType == MDRTreasureContainerType.None) {
	//				if (Area.gold > 0) {
	//					Party.ReceiveGold(Area.gold);
	//					PostCombatNotice("Party receives {0}.", CoM.CoinsAmount(Area.gold));
	//					Area.gold = 0;
	//				}
	//			}
	//
	//			Party.ClearActions();
	//
	//			Area.Aggressive = false;
	//
	//			ClearCombat();
	//		}
	//
	//		#endregion
	//
	//
	//		#region Character Actions
	//
	//		/**
	//		 * Peforms the given characters currently selected action.
	//		 * Returns true if character wants another action, false if they don't */
	//		private bool processCharactersAction(MDRCharacter character)
	//		{
	//			if ((character == null) || (character.IsDead))
	//				return false;
	//
	//			character.actionsThisRound++;
	//
	//			// select a target
	//			if (!(updateCharacterTarget(character, Area)))
	//				return false;
	//
	//			// process action
	//			switch (character.CurrentAction.Type) {
	//				case ActionType.Fight:
	//					return characterFight(character);
	//				case ActionType.Spell:
	//					character.CurrentAction.Execute(character, Party);
	//					return false;
	//				case ActionType.Empty:
	//					PostPlayerMessage("{0} does nothing.", Format(character));
	//					return false;
	//				case ActionType.Defend:
	//					PostPlayerMessage("{0} defends.", Format(character));
	//					return false;
	//			}
	//
	//			return false;
	//		}
	//
	//		private void playSoundEffect(string soundEffectName, float volume = 1f, float delay = 0f)
	//		{
	//			if (simulationMode)
	//				return;
	//
	//			SoundManager.Play(soundEffectName, volume, delay);
	//		}
	//
	//		private void playSoundEffect(CombatSoundEffect soundEffect, float volume = 1f, float delay = 0f)
	//		{
	//			if (simulationMode)
	//				return;
	//
	//			switch (soundEffect) {
	//				case CombatSoundEffect.Backstab:
	//					SoundManager.Play("OLDFX_BSTAB", volume, delay);
	//					break;
	//				case CombatSoundEffect.CriticalHit:
	//					SoundManager.Play("OLDFX_CHIT", volume, delay);
	//					break;
	//				case CombatSoundEffect.Hit:
	//					SoundManager.Play("OLDFX_HIT", volume, delay);
	//					break;
	//			}
	//		}
	//
	//		private void characterMisses(MDRCharacter character, MDRMonsterStack target)
	//		{
	//			playSoundEffect(character.AttackSFXName);
	//			PostPlayerMessage("{0} misses {1}", Format(character), Format(target));
	//			target.WasMissed(character);
	//		}
	//
	//
	//		/** Processes damage to monster. Returns if character kills the monster or not */
	//		private bool characterHits(MDRCharacter character, MDRMonsterStack target, int damage, int mitigation, bool didCritical = false, bool didBackstab = false)
	//		{
	//			if (damage == 0) {
	//				PostPlayerMessage("{0} attacks {1} but fails to cause any damage ({2} exorbed).", Format(character), Format(target), Format(mitigation));
	//				return false;
	//			}
	//
	//			bool didKill = false;
	//
	//			// Calculate the correct verb and audio sound.
	//			string verb;
	//			if (didCritical && didBackstab) {
	//				verb = "<B><I>EXECUTES</I></B>";
	//				playSoundEffect(CombatSoundEffect.Backstab);
	//				playSoundEffect(CombatSoundEffect.CriticalHit, 1f, 0.2f);
	//			} else if (didCritical) {
	//				verb = "<B><I>critical hits</I></B>";
	//				playSoundEffect(CombatSoundEffect.CriticalHit);
	//			} else if (didBackstab) {
	//				verb = "<B><I>backstabs</I></B>";
	//				playSoundEffect(CombatSoundEffect.Backstab);
	//			} else {
	//				verb = "attacks";
	//				playSoundEffect(character.AttackSFXName);
	//			}
	//
	//			// damage the stack
	//			if (target.Hits <= damage) {
	//				playSoundEffect(CombatSoundEffect.Hit, 1f, 0.12f);
	//				playSoundEffect(target.DieSFXName, 1f, 0.13f);
	//				PostPlayerMessage("{0} {1} {2} and kills it.", Format(character), verb, Format(target.Monster));
	//				didKill = true;
	//			} else {
	//				playSoundEffect(CombatSoundEffect.Hit, 1f, 0.12f);
	//				string exorbNote = (mitigation > 0) ? Util.Colorise(" ({4} exorbed)", Color.gray) : "";
	//				PostPlayerMessage("{0} {1} {2} for {3}." + exorbNote, Format(character), verb, Format(target.Monster), FormatPlayerDamage(damage), Format(mitigation));
	//			}
	//
	//			damage = target.ReceiveDamage(new DamageInfo(damage));
	//			GainXP(character, GameRules.CalculateXP(character, target.Monster, damage));
	//
	//			return didKill;
	//		}
	//
	//		/**
	//		 * Processes given character inflicting a hit on given monster.
	//		 * Returns true if character should get another action or not.
	//		 */
	//		private bool characterFight(MDRCharacter character)
	//		{
	//			var target = Area.Stack[character.target];
	//
	//			bool didKill = false;
	//			bool didHit = false;
	//			int damage = 0;
	//
	//			didHit = UnityEngine.Random.value <= GameRules.CalculateChanceToHit(character, target);
	//
	//			// check if we can hit it
	//			if (didHit) {
	//				bool didCritical = UnityEngine.Random.value <= GameRules.CriticalHitChance(character, target);
	//				bool didBackstab = UnityEngine.Random.value <= GameRules.BackstabChance(character, target);
	//				int mitigation = 0;
	//				damage = GameRules.CalculateDamage(character, target, didCritical, didBackstab, out mitigation);
	//				didKill = characterHits(character, target, damage, mitigation, didCritical, didBackstab);
	//			} else
	//				characterMisses(character, target);
	//
	//			if (didKill)
	//				CoM.GameStats.RegisterMonsterKilled(character, target.Monster, 1);
	//
	//			if (didHit)
	//				CombatStats.RegisterPartyHit(damage);
	//			else
	//				CombatStats.RegisterPartyMiss();
	//
	//
	//			return (didKill) && (character.actionsThisRound < character.Swings);
	//		}
	//
	//		/**
	//		 * Updates target for given character.  If current target is still active target will remain on them,
	//		 * otherwise the next availaible target will be selected.
	//		 * Returns if a suitable target was found.
	//		 */
	//		private bool updateCharacterTarget(MDRCharacter character, MDRArea area)
	//		{
	//			int target = Util.ClampInt(character.target, 0, 3);
	//
	//			int attempts = 0;
	//
	//			while ((area.Stack[target] == null) || (area.Stack[target].IsEliminated)) {
	//				target++;
	//				attempts++;
	//				if (target == 4)
	//					target = 0;
	//				if (attempts > 4)
	//					return false;
	//			}
	//			character.target = target;
	//			return true;
	//		}
	//
	//		/** Performs the actions for each stack of monsters in the group */
	//		private void processMonsterGroup(MDRArea group)
	//		{
	//			for (var lp = 0; lp < 4; lp++)
	//				if (group.Stack[lp] != null)
	//					processMonsterStack(group.Stack[lp]);
	//		}
	//
	//		/** Performs the given monsters action */
	//		private void processMonsterStack(MDRMonsterStack stack)
	//		{
	//			if (stack.IsEliminated)
	//				return;
	//
	//			// create a weighted list of potential targets, where targets at the front of the party have a higher chance of being selected
	//			List<MDRCharacter> potentialTargets = new List<MDRCharacter>();
	//			for (int partyPosition = 0; partyPosition < 4; partyPosition++) {
	//				MDRCharacter character = Party[partyPosition];
	//				if ((character != null) && (!character.IsDead))
	//					for (int lp = 0; lp < (4 - partyPosition); lp++)
	//						potentialTargets.Add(character);
	//			}
	//
	//			if (potentialTargets.Count >= 1) {
	//				MDRCharacter monsterTarget = potentialTargets[Util.Roll(potentialTargets.Count) - 1];
	//				processMonsterAttack(stack, monsterTarget);
	//			}
	//		}
	//
	//		/** Doesn't do anything right now except mark the functions where companions need to be damaged */
	//		private void damageCompanions()
	//		{
	//
	//		}
	//
	//		/** Processes a monsters special attack. */
	//		private void monsterSpecialAttack(MDRMonsterStack stack, MDRCharacter target, string attackString, float resistance, string sfxName, bool alternativeMode)
	//		{
	//			float factor = alternativeMode ? 1.1375f : 4.55f;
	//			int chanceSplit = alternativeMode ? 20 : 10;
	//			float damageRange = alternativeMode ? 2f : 1f;
	//			float mitigationFactor = alternativeMode ? 0.007f : 0.07f;
	//			bool didDamageCompanions = alternativeMode ? true : false;
	//
	//			// If this is highest level or current guild I have no idea.
	//			float gLvl = target.CurrentLevel;
	//			float mLvl = stack.Monster.GuildLevel;
	//
	//			// note: this differs from the http://dejenol.com/index.php?title=Monster_Attack_Formulas refernce
	//			// which seems quite wrong.  Accoding to the page if our guild level is higher than the monsters the
	//			// monster has a 90% chance of acid attack.
	//			int chance = 50;
	//			if (gLvl > mLvl * 1.25f)
	//				chance = chanceSplit;
	//			if (gLvl < mLvl / 1.25f)
	//				chance = 100 - chanceSplit;
	//
	//			if (Util.Roll(100) > chance)
	//				return;
	//
	//			if (!String.IsNullOrEmpty(sfxName))
	//				playSoundEffect(sfxName);
	//
	//			float levelMod = alternativeMode ? mLvl * (float)Math.Log(mLvl) : mLvl;
	//			float minimum = (int)((Math.Log(levelMod) + levelMod / 15f) * factor);
	//			float baseDamage = minimum + UnityEngine.Random.value * minimum * damageRange;
	//			float mitigation = gLvl * mitigationFactor;
	//			int resistedAmount = (int)((baseDamage - mitigation) * (resistance / 200f));
	//			int finalDamage = (int)((baseDamage - mitigation) - resistedAmount);
	//
	//			target.ReceiveDamage(new DamageInfo(finalDamage));
	//
	//			string resistedPart = (resistedAmount == 0) ? "" : " ({3} resisted)";
	//
	//			PostMonsterMessage(attackString + resistedPart, Format(stack.Monster), Format(target), FormatMonsterDamage(finalDamage), resistedAmount);
	//
	//			if (didDamageCompanions)
	//				damageCompanions();
	//
	//		}
	//
	//		/**
	//		 * Performs a monsters spell attack against given target.
	//		 */
	//		private void monsterSpell(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			var spells = stack.Monster.KnownSpells;
	//
	//			if (spells.Count == 0)
	//				return;
	//
	//			var spell = spells[Util.Roll(spells.Count) - 1];
	//
	//			if (Party.CurrentTile.Antimagic) {
	//				PostMonsterMessage("{0} tries to casts {1} but it fizzels.", Format(stack), Format(spell), Format(target));
	//				return;
	//			}
	//
	//			int resisted = 0;
	//			int damage = spell.CalculateSpellDamage(stack, target, out resisted);
	//
	//			bool killed = (target.ReceiveDamage(new DamageInfo(damage)) > 0) && target.IsDead;
	//
	//			string resistedPart = (resisted > 0) ? " ({4} resisted)" : "";
	//
	//			string formatString = "";
	//			if (damage == 0)
	//				formatString = "{0} casts {1} on {2}, but fails.";
	//			else if (killed)
	//				formatString = "{0} casts {1} on {2} and kills them!";
	//			else
	//				formatString = "{0} casts {1} on {2} and damages for {3}";
	//
	//			PostMonsterMessage(formatString + resistedPart, Format(stack), Format(spell), Format(target), FormatMonsterDamage(damage), resisted);
	//		}
	//
	//		/**
	//		 * Performs a physcial attack against given target.
	//		 * Returns if there was a sucessful hit or not.
	//		 */
	//		private bool processMonsterAttackPhysical(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			var monster = stack.Monster;
	//
	//			float hitChance = GameRules.CalculateChanceToHit(stack, target);
	//
	//			int hits = 0;
	//			float totalDamage = 0;
	//
	//			bool didAnyMonsterCrit = false;
	//			bool didAnyMonsterBackstab = false;
	//			bool significantHit = false;
	//
	//			playSoundEffect(stack.AttackSFXName, 0.6f);
	//
	//			// Calculate the damage of each monster, reducing how much damage a monster can do for each one that hits.
	//
	//			float damageModifier = 1f;
	//			int totalMitigation = 0;
	//			for (int lp = 0; lp < stack.Count; lp++) {
	//				if (UnityEngine.Random.value <= hitChance) {
	//					hits++;
	//					int mitigation = 0;
	//
	//					bool didCrit = UnityEngine.Random.value <= GameRules.CriticalHitChance(stack, target);
	//					bool didBackstab = UnityEngine.Random.value <= GameRules.BackstabChance(stack, target);
	//
	//					totalDamage += damageModifier * GameRules.CalculateDamage(stack, target, didCrit, didBackstab, out mitigation);
	//
	//					didAnyMonsterCrit = didAnyMonsterCrit || didCrit;
	//					didAnyMonsterBackstab = didAnyMonsterBackstab || didBackstab;
	//
	//					totalMitigation += mitigation;
	//					damageModifier *= 0.75f;
	//				}
	//			}
	//
	//			if (hits == 0) {
	//				PostMonsterMessage("{0} misses {1}.", Format(monster), Format(target));
	//				stack.ShortStatus = "Misses.";
	//			} else {
	//
	//				string plural = (stack.Count > 1) ? "s" : "";
	//				string inversePlural = (stack.Count > 1) ? "" : "s";
	//
	//				string verb = "";
	//				if (didAnyMonsterBackstab && didAnyMonsterCrit)
	//					verb = " <B><I>EXECUTE" + inversePlural.ToUpper() + "</I></B> ";
	//				else if (didAnyMonsterCrit)
	//					verb = " <B><I>critical hit" + inversePlural + "</I></B> ";
	//				else if (didAnyMonsterBackstab)
	//					verb = " <B><I>backstab" + inversePlural + "</I></B> ";
	//				else
	//					verb = " hit" + inversePlural;
	//
	//				string hitsPart = (stack.Count == 1) ? "{0}" + verb : "{3} {0}" + plural + verb;
	//				string exorbedPart = (totalMitigation > 0) ? Util.Colorise(" ({4} exorbed)", Color.gray) : "";
	//				string forPart = " {1} for {2}!";
	//				string message = hitsPart + forPart + exorbedPart;
	//
	//				PostMonsterMessage(message, Format(monster), Format(target), FormatMonsterDamage((int)totalDamage), hits, Format(totalMitigation));
	//
	//				string shortStatus = (hits > 1) ? "{1} hit for {0}." : "Hit for {0}.";
	//				stack.ShortStatus = string.Format(shortStatus, FormatMonsterDamage((int)totalDamage), hits);
	//
	//				target.ReceiveDamage(new DamageInfo((int)totalDamage, stack));
	//
	//				if (totalDamage >= target.MaxHits * 0.25f)
	//					significantHit = true;
	//			}
	//
	//			if (hits >= 1)
	//				CombatStats.RegisterMonsterHit((int)totalDamage);
	//			else {
	//				target.WasMissed(stack);
	//				CombatStats.RegisterMonsterMiss();
	//			}
	//
	//			if (hits >= 1) {
	//				playSoundEffect(target.HurtSFXName, significantHit ? 2f : 0.75f);
	//			}
	//
	//			return (hits >= 1);
	//		}
	//
	//		private void processAgeAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			float resistance = (100f + stack.Monster.AppearsOnLevel * 2) - (target.Stats.Con * 2.5f);
	//			if (resistanceRoll(resistance)) {
	//				PostMonsterMessage("{1} resists {1} attempt to age.", Format(stack), Format(target));
	//				return;
	//			}
	//			int aging = Util.Roll((int)Math.Min(1825, stack.Monster.GuildLevel * 7.3f));
	//			target.Age += aging;
	//			PostMonsterMessage("{0} causes {1} to age {2}", Format(stack), Format(target), Util.FormatDays(aging));
	//		}
	//
	//		/** Tests if character passes or fails a resistance roll.  Returns if the character resists or not */
	//		private bool resistanceRoll(float resistance)
	//		{
	//			return (Util.Roll(100) <= resistance);
	//		}
	//
	//		private void processDrainAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			float chance = (100 + stack.Monster.AppearsOnLevel * 2) - (target.Stats.Con * 2.5f);
	//			if (Util.Roll(100) > chance)
	//				return;
	//
	//			if (resistanceRoll(target.Resistance["Drain"])) {
	//				PostMonsterMessage("{0} resisted being dained by {1}", Format(target), Format(stack));
	//				return;
	//			}
	//
	//			int attempts = Util.Roll((int)Math.Ceiling(stack.Monster.AppearsOnLevel / 2f + 0.5f));
	//
	//			for (int lp = 0; lp < attempts; lp++) {
	//				int statToDrain = Util.Roll(10) - 1;
	//				if (statToDrain < 6) {
	//					target.BaseStats[statToDrain]--;
	//					target.ApplyChanges();
	//					PostMonsterMessage("{0} has {2} drained by {1}.", Format(target), Format(stack), MDRStats.LONG_STAT_NAME[statToDrain]);
	//				} else {
	//					target.MaxHits -= 2;
	//					if (target.MaxHits < 1)
	//						target.MaxHits = 1;
	//					if (target.Hits > target.MaxHits)
	//						target.Hits = target.MaxHits;
	//					target.ApplyChanges();
	//					PostMonsterMessage("{0} was drained 2hp by {1}.", Format(target), Format(stack));
	//				}
	//			}
	//		}
	//
	//		private void processStoneAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			float chance = Util.Clamp(100 + stack.Monster.AppearsOnLevel - (target.Stats.Con * (float)Math.Log(target.Stats.Con) * 1.09f), 5f, 100f);
	//			if (Util.Roll(100) > chance)
	//				return;
	//
	//			if (resistanceRoll(target.Resistance["Stone"])) {
	//				PostMonsterMessage("{0} resisted being turned to stone by {1}", Format(target), Format(stack));
	//				return;
	//			}
	//
	//			target.Hits = 0;
	//			target.Status = CharacterStatus.Stoned;
	//			PostMonsterMessage("{0} was turned to stone by {1}", Format(target), Format(stack));
	//		}
	//
	//		private void processPoisonAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			if (resistanceRoll(target.Resistance["Poison"])) {
	//				PostMonsterMessage("{0} resisted being poisoned by {1}", Format(target), Format(stack));
	//				return;
	//			}
	//
	//			int poisonLevel = (int)(Math.Sqrt(stack.Monster.AppearsOnLevel) * 5);
	//			target.ApplyPoison(poisonLevel);
	//			PostMonsterMessage("{0} was poisoned by {1}.", Format(target), Format(stack));
	//		}
	//
	//		private void processDiseaseAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			if (resistanceRoll(target.Resistance["Disease"])) {
	//				PostMonsterMessage("{0} resisted being diseased by {1}", Format(target), Format(stack));
	//				return;
	//			}
	//
	//			int diseaseLevel = (int)(Math.Sqrt(stack.Monster.AppearsOnLevel) * 30);
	//			target.ApplyPoison(diseaseLevel);
	//			PostMonsterMessage("{0} was diseased by {1}.", Format(target), Format(stack));
	//		}
	//
	//		private void processParalyzeAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			float chance = Util.Clamp(target.Resistance["Paralysis"] + target.Stats.Con, 5f, 100f);
	//
	//			if (Util.Roll(100) <= chance) {
	//				PostMonsterMessage("{0} resisted being paralysis by {1}", Format(target), Format(stack));
	//				return;
	//			}
	//			target.ApplyParalysis(stack.Monster.AppearsOnLevel);
	//		}
	//
	//		private void processStealAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			PostMonsterMessage("Monster steals from you, but nothing... lucky you.");
	//		}
	//
	//		private void processDestroyItemAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			PostMonsterMessage("Monster destroys one of your items, but I haven't coded this yet... lucky you.");
	//		}
	//
	//		/**
	//		 * Causes monster to attack given target and deal damage.
	//		 */
	//		private void processMonsterAttack(MDRMonsterStack stack, MDRCharacter target)
	//		{
	//			if (target == null)
	//				return;
	//
	//			var monster = stack.Monster;
	//
	//			//1. Pre attack phase (acid and breath)
	//			if (monster.Abilities.Acid)
	//				monsterSpecialAttack(stack, target, "{0} splits acid on {1} for {2}.", target.Resistance["Acid"], "", false);
	//			if (monster.Abilities.BreathFire)
	//				monsterSpecialAttack(stack, target, "{0} breaths fire on {1} for {2}.", target.Resistance["Fire"], "FIRE", true);
	//			if (monster.Abilities.BreathCold)
	//				monsterSpecialAttack(stack, target, "{0} breaths cold on {1} for {2}.", target.Resistance["Cold"], "COLD", true);
	//
	//			//2. Attack (spell or attack)
	//			if ((monster.CanCastSpells) && (Util.FlipCoin)) {
	//				monsterSpell(stack, target);
	//				return;
	//			}
	//
	//			bool didHit = processMonsterAttackPhysical(stack, target);
	//
	//			//3. Post attack ability
	//			if (monster.Abilities.Electrocute)
	//				monsterSpecialAttack(stack, target, "{0} electrocutes {1} for {2}.", target.Resistance["Electrial"], "SHOCK", false);
	//
	//			if (didHit) {
	//				if (monster.Abilities.Age)
	//					processAgeAttack(stack, target);
	//				if (monster.Abilities.Drain)
	//					processDrainAttack(stack, target);
	//				if (monster.Abilities.Stone)
	//					processStoneAttack(stack, target);
	//				if (monster.Abilities.Poison)
	//					processPoisonAttack(stack, target);
	//				if (monster.Abilities.Disease)
	//					processDiseaseAttack(stack, target);
	//				if (monster.Abilities.Paralyze)
	//					processParalyzeAttack(stack, target);
	//				if (monster.Abilities.Steal)
	//					processStealAttack(stack, target);
	//				if (monster.Abilities.DestroyItem)
	//					processDestroyItemAttack(stack, target);
	//			}
	//		}
	//
	//		#endregion
	//
	//		/** Waits for given number of seconds. */
	//		private YieldInstruction delayCombat(float time)
	//		{
	//			return new WaitForSeconds(time);
	//		}
	//
	//		/** Waits until next frame. */
	//		private YieldInstruction delayCombat()
	//		{
	//			return new WaitForEndOfFrame();
	//		}
	//
	//
	//		/** Given character gains XP */
	//		private void GainXP(MDRCharacter character, int xp)
	//		{
	//			if (simulationMode)
	//				return;
	//			character.GainXP(xp);
	//
	//		}
	//
	//	}
}