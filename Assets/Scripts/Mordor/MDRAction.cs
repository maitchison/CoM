
using UnityEngine;

using System;
using System.Collections;
using System.Xml.Linq;

using Mordor;

using UI;
using System.Collections.Generic;
using Engines;

namespace Data
{
	public enum ActionType
	{
		Empty,
		Fight,
		Defend,
		Spell,
		Item
	}

	/**
	 * Stores a list of actions that the player can perform 
	 */
	public class MDRActionList : DataObject
	{
		/** The default action to perform when in combat */
		public MDRAction Default { get { return Slot[0]; } }

		protected MDRAction[] Slot;

		/** 
		 * Creates an action list with given number of slots 
		 * @param length Number of slots to create
		 */
		public MDRActionList(int length)
		{
			Slot = new MDRAction[length];
			for (int lp = 0; lp < length; lp++) {
				Slot[lp] = new MDRAction(ActionType.Empty);
			}
		}

		/** If this list contains given action */
		public bool Contains(MDRAction action, bool deepCompare = false)
		{
			foreach (MDRAction slot in Slot) {
				if (deepCompare)
				if (slot.CompareTo(action))
					return true;
				else if (slot == action)
					return true;
			}
			return false;
		}

		/** Clears all buffer slots and sets first two actions to fight and defend */
		public void Reset()
		{
			Clear();
			Slot[0] = MDRAction.Fight.Clone();
			Slot[1] = MDRAction.Defend.Clone();
		}

		/** Sets all buffer actions to empty */
		public void Clear()
		{
			for (int lp = 0; lp < Slot.Length; lp++) {
				Slot[lp] = MDRAction.Empty.Clone();
			}
		}

		public int Count {
			get { return Slot.Length; } 
		}

		/** Access to slots */
		public MDRAction this [int index] {
			get { return Slot[index]; }
			set { Slot[index] = value; }
		}

		/** Enumerator for data */
		public IEnumerator GetEnumerator()
		{
			return Slot.GetEnumerator();
		}


		#region implemented members of DataObject

		public override void WriteNode(XElement node)
		{
			foreach (MDRAction slot in Slot)
				WriteValue(node, "Action", slot);
		}

		public override void ReadNode(XElement node)
		{
			int index = 0;
			foreach (XElement subNode in node.Elements("Action")) {
				Slot[index].ReadNode(subNode);
				index++;
			}
		}

		#endregion
	}

	/** 
	 * Defines a characters action.  A character may execute this action. 
	 */
	public class MDRAction : DataObject
	{
		public static MDRAction Empty = new MDRAction(ActionType.Empty);
		public static MDRAction Fight = new MDRAction(ActionType.Fight);
		public static MDRAction Defend = new MDRAction(ActionType.Defend);

		public ActionType Type { get { return _type; } }

		private ActionType _type;

		/** The spell this action will cast, or null if this is not a spell action */
		public MDRSpell Spell { get { return getSpell(); } }

		public int Parameter { get { return _parameter; } }

		private int _parameter;

		/** The intended target of this action, if required. */
		public MDRActor SpecifiedTarget;

		/** Returns the sprite used for this action */
		public Sprite Icon {
			get { 
				switch (Type) {
					case ActionType.Defend:
						return CoM.Instance.IconSprites["Action_Defend"];
					case ActionType.Fight:
						return CoM.Instance.IconSprites["Action_Fight"];
					case ActionType.Item:
						return null;
					case ActionType.Spell:
						return Spell.Icon;
				}

				return null;
			}
		}

		/** If true this action requires the user to manually select a target. */
		public bool NeedsTargetSelection { 
			get {
				if (Spell != null)
					return Spell.NeedsTargetSelection;
				return false;
			}
		}

		/** 
		 * Creates a new character action of the given type
		 * 
		 * @param action The action to perform
		 * @param targetID The parameter for the target.  I.e. for a spell action this is the ID of the spell.
		 * 
		 */
		public MDRAction(ActionType actionType, int actionParameter = 0)
		{
			_type = actionType;
			_parameter = actionParameter;
		}

		/** 
		 * Executes this action with given context. 
		 * Most spells and items can be executed, however some actions, such as fight or defend are handled by 
		 * the combat engine.
		 */
		public void Execute(MDRCharacter character, MDRParty party)
		{
			Trace.LogDebug("EXECUTING {0} on {1}", this, character);
			switch (Type) {
				case ActionType.Spell:
					ExecuteSpell(character, party);
					break;
			}
		}

		/**
		 * Executes a spell action.
		 */
		private void ExecuteSpell(MDRCharacter character, MDRParty party)
		{	
			/*
			if (Spell == null)
				return;
			var area = (party == null) ? null : party.Area;
			var defaultTarget = (area == null) ? null : area.Stack[character.target];

			int spellCost = Spell.CostFor(character);

			// Cheat.
			if (Settings.Advanced.WhiteWizard)
				spellCost = 1;

			if (party.CurrentTile.Antimagic) {
				CombatEngine.PostPlayerMessage("{0} is unable to cast {1}, anti-magic square.", CoM.Format(character), CoM.Format(Spell)); 
				return;
			}

			if (!character.CanCast(Spell)) {
				CombatEngine.PostPlayerMessage("{0} is not able to cast {1}.", CoM.Format(character), CoM.Format(Spell)); 
				return;
			}

			if (character.Spells < spellCost) {
				CombatEngine.PostPlayerMessage("{0} does not have enough spells to cast {1}.", CoM.Format(character), CoM.Format(Spell)); 
				return;
			}

			if (Spell.NeedsTargetSelection && SpecifiedTarget == null) {
				Trace.LogDebug("No target selected in time to cast {0}, will try again next round.", Spell);
				return;
			}
				
			var results = new List<SpellResult>();

			switch (Spell.SpellTarget) {
				case SpellTarget.Area:
					results = Spell.CastArea(character, area);
					break;
				case SpellTarget.Party:
					results = Spell.CastParty(character, party);
					break;
				default:
					var info = Spell.CastTargeted(character, SpecifiedTarget ?? defaultTarget);
					if (info != null && info.DidCast)
						results.Add(info);
					break;
			}

			if (results.Count >= 1) {
				character.Spells -= spellCost;
				SoundManager.Play(Spell.SpellClass.SFXName);
			}

			// reset action after targeted spell
			if (Spell.NeedsTargetSelection) {
				character.DefaultAction();
			}

			//for (int lp = 0; lp < results.Count; lp++)
			//	CombatEngine.PostPlayerMessage(results[lp].ToString());			
			*/
		}

		public override string ToString()
		{
			if (Type == ActionType.Spell) {
				return "Spell " + Spell;
			}
			return Type.ToString();
		}

		/** Copyies another action this this action */
		public void CopyFrom(MDRAction source)
		{
			this._type = source.Type;
			this._parameter = source.Parameter;
		}

		/** 
		 * Returns the spell this action will cast, or null of this action is not a spell action 
		 */
		private MDRSpell getSpell()
		{
			if (Type != ActionType.Spell)
				return null;
			return CoM.Spells.ByID(Parameter);
		}

		public bool IsEmpty {
			get {
				return (Type == ActionType.Empty);
			}
		}

		public bool IsAggressive {
			get { 
				switch (Type) {
					case ActionType.Defend:
					case ActionType.Empty:
						return false;
					case ActionType.Fight:
						return true;					
					case ActionType.Spell:						
						return Spell.CombatSpell;
				}
				return false;
			}
		}

		/** Clears action */
		public void Clear()
		{
			_type = ActionType.Empty;
			_parameter = 0;
		}

		/** Returns true if this action is the same as given action */
		public bool CompareTo(MDRAction otherAction)
		{
			return ((this.Type == otherAction.Type) && (this.Parameter == otherAction.Parameter));
		}

		public MDRAction Clone()
		{
			return (MDRAction)this.MemberwiseClone();
		}

		#region implemented members of DataObject

		public override void WriteNode(XElement node)
		{
			if (IsEmpty)
				return;
			node.Value = Type.ToString();
			if (Parameter != 0)
				WriteAttribute(node, "Parameter", Parameter.ToString());
		}

		public override void ReadNode(XElement node)
		{
			if (node.Value == "") {
				Clear();
				return;
			}
			_type = (ActionType)Enum.Parse(typeof(ActionType), node.Value);
			_parameter = ReadAttributeInt(node, "Parameter", 0);
		}

		#endregion
	}
}