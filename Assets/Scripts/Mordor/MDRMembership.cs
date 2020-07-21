
using System.Collections.Generic;
using System.Xml.Linq;

using Data;
using Mordor;

using System.Collections;
using UnityEngine;

namespace Mordor
{
	/** Membership entry describes a characters current status with given guild */
	public class MDRGuildMembership : DataObject
	{
		/** The guild we are recording membership for */
		public MDRGuild Guild;
		
		/** The level in this guild that we have obtained */
		public int CurrentLevel;

		/** Our current experiance with this guild, capped to pinned xp. */
		public int XP { get { return _xp; } set { setXP(value); } }

		/** True if guild currently requires a quest */
		public bool HasQuest;

		/** If we are a member of this guild or not */
		public bool IsMember { get { return CurrentLevel > 0; } }

		/** Spell level in this guild */
		protected int CommonSpellLevel { get { return (CurrentLevel + 1) / 2; } }

		private int _xp;

		/** 
		 * The maximum amount of experiance we can obtain in this guild before needing to make a level.
		 * Capped to 1 less than two levels past our current level 
		 */
		public int PinnedXP {
			get {
				return Guild.XPForLevel(CurrentLevel + 2) - 1;
			}
		}

		/** 
		 * Returns if the character is currently pinned in this guild or not 
		 */
		public bool IsPinned {
			get {
				return (XPToPin <= 0);
			}
		}

		/** 
		 * The amount of experiance required to obtain this level, regardless of any expreriance gained.
		 */
		public int XPForThisLevel {
			get {
				return Guild.XPForLevel(CurrentLevel + 1) - Guild.XPForLevel(CurrentLevel);
			}
		}

		/** 
		 * The amount of experiance required to obtain the next level, regardless of any expreriance gained.
		 */
		public int XPForNextLevel {
			get {
				return Guild.XPForLevel(CurrentLevel + 2) - Guild.XPForLevel(CurrentLevel + 1);
			}
		}

		/** 
		 * The amount of additional experiance required to obtain the next level.
		 * If XP exceeds requirement a negative value will be returned.
		 */
		public int ReqXP {
			get {
				return Guild.XPForLevel(CurrentLevel + 1) - XP;
			}
		}

		/** 
		 * The amount of XP that can be gained before our experiance is pinned 
		 */
		public int XPToPin {
			get {
				return PinnedXP - XP;
			}
		}

		/** Reset the memebership no membership, no experience */
		public void Reset()
		{
			CurrentLevel = 0;
			XP = 0;
			HasQuest = false;
		}

		/** Grants level 1 membership, no experience */
		public void Join()
		{
			Reset();
			CurrentLevel = 1;
		}

		/** 
		 * Gains given amount of XP as a member of this guild
		 * Experiance is capped to 1 less than two levels past our current level
		 */
		private void setXP(int value)
		{
			_xp = value;
			if (_xp > PinnedXP)
				_xp = PinnedXP;
		}

		/**
		 * The amount of gold required gain the next level
		 */
		public int RequiredGold {
			get {				
				int levelCost = (int)(100f + Mathf.Pow(CurrentLevel, 1f + (Guild.GoldFactor / 10f)) * 100f);
				return levelCost;
			}
		}

		/**
		 * Creates a new guild membership record.
		 * @param guild The guild to record membership information for 
		 */
		public MDRGuildMembership(MDRGuild guild)
		{
			this.Guild = guild;
			XP = 0;
			CurrentLevel = 0;
			HasQuest = false;
		}

		/** 
		 * Returns the spell level obtained by this guild in a given magic class, but capped to the guilds max spell level.
		 */
		public int SpellLevel(MDRSpellClass spellClass)
		{
			//todo: fix these formulas
			var skill = spellClass.Skill;
			return (int)Util.Clamp(Guild.SkillRate[skill.ID] * CommonSpellLevel, 0, Guild.SkillRate[skill.ID] * 4);
		}

		/**
		 * Returns if the current memebership in this guild allows the casting of given spell or not 
		 */
		public bool CanCastSpell(MDRSpell spell)
		{
			return (SpellLevel(spell.SpellClass) >= spell.SpellLevel);
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{
			WriteAttribute(node, "GuildName", Guild.Name);
			WriteAttribute(node, "XP", XP);
			WriteAttribute(node, "CurrentLevel", CurrentLevel);
			WriteAttribute(node, "HasQuest", HasQuest ? 1 : 0);
		}

		public override void ReadNode(XElement node)
		{
			Guild = CoM.Guilds[ReadAttribute(node, "GuildName")];
			CurrentLevel = ReadAttributeInt(node, "CurrentLevel");
			XP = ReadAttributeInt(node, "XP");
			HasQuest = (ReadAttributeInt(node, "HasQuest") == 1);
		}

		#endregion
	}

	/** Records guild membership for a player for each guild in the game */
	public class MDRGuildMembershipLibrary : DataObject, IEnumerable
	{
		public Dictionary<MDRGuild,MDRGuildMembership> Membership;

		public MDRGuild CurrentGuild { get; set; }

		public MDRGuildMembership Current { 
			get {
				//stub: current guild should never be null!
				if (CurrentGuild == null) {
					Trace.LogWarning("Character has as null current guild.");
					return Membership[CoM.Guilds[0]];
				}
				return Membership[CurrentGuild]; 
			} 
		}

		public  MDRGuildMembershipLibrary()
		{
			Membership = new Dictionary<MDRGuild, MDRGuildMembership>();
			foreach (MDRGuild guild in CoM.Guilds)
				Membership[guild] = new MDRGuildMembership(guild);
		}

		/** Indexer to guild membership by guild */
		public MDRGuildMembership this [MDRGuild guild] { 
			get { return (Membership[guild]); }
		}

		#region IEnumerable implementation

		public IEnumerator GetEnumerator()
		{
			return Membership.Values.GetEnumerator();
		}

		#endregion

		
		/** This characters maximum level in any guild */
		public int MaxLevel {
			get {
				int maxLevel = 0;
				foreach (KeyValuePair<MDRGuild,MDRGuildMembership> entry in Membership)
					if (entry.Value.CurrentLevel > maxLevel)
						maxLevel = entry.Value.CurrentLevel;
				return maxLevel;
			}
		}

		/**
		 * Total amount of experiance this character has obtained in all guilds 
		 */
		public int TotalXP {
			get {
				int totalXP = 0;
				foreach (KeyValuePair<MDRGuild,MDRGuildMembership> entry in Membership)
					totalXP += entry.Value.XP;
				return totalXP;
			}
		}

		/**
		 * The total number of guilds this character is a memeber of 
		 */
		public int JoinedGuilds {
			get {
				int total = 0;
				foreach (KeyValuePair<MDRGuild,MDRGuildMembership> entry in Membership)
					if (entry.Value.IsMember)
						total++;
				return total;
			}
		}

		#region implemented abstract members of DataObject

		public override void WriteNode(XElement node)
		{	
			WriteValue(node, "CurrentGuild", CurrentGuild == null ? CoM.Guilds.Default.Name : CurrentGuild.Name);
			foreach (KeyValuePair<MDRGuild,MDRGuildMembership> entry in Membership) {
				if (entry.Value.IsMember)
					WriteValue(node, "Guild", entry.Value);
			}
		}

		public override void ReadNode(XElement node)
		{
			foreach (MDRGuild guild in CoM.Guilds)
				Membership[guild].Reset();
			
			string guildName = ReadValue(node, "CurrentGuild");
			CurrentGuild = CoM.Guilds.ByName(guildName);
			if (CurrentGuild == null)
				Trace.LogWarning("Could not find guild with name '" + guildName + "' for current guild membership.");
			
			foreach (XElement subNode in node.Elements("Guild")) {
				guildName = subNode.Attribute("GuildName").Value;
				MDRGuild guild = CoM.Guilds.ByName(guildName);
				if (guild != null)
					Membership[guild].ReadNode(subNode);
				else
					Trace.LogWarning("Could not find guild with name '" + guildName + "' for membership.");
			}
			
		}

		#endregion
	}
	

}
