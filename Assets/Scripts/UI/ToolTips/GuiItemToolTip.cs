
using UnityEngine;

using Mordor;
using Data;
using UI.Generic;

namespace UI
{
	/** Displays information about an item.  Autosizes, and is invisibile if no item is present. */
	public class GuiItemToolTip : GuiToolTip<MDRItemInstance>
	{
		/** The item to display information about */
		public MDRItemInstance ItemInstance { get { return Data; } set { Data = value; } }

		/** If true everything will be shown about this item, regardless of ID level, useful for stores.  
  			Also no message will be presented telling the player how well they have IDed the item. */
		public bool ShowAllInfo;

		/** Will size the info panel to a sensiable size. */
		public bool AutoFit = true;

		private MDRItemInstance _item;

		public GuiItemToolTip()
			: base()
		{
			IconSprite.AlphaBlend = true;
			IconSprite.X = 31;
			IconSprite.Y = 26;
		}

		private string FormatStatMod(int value, string name)
		{
			if (value < 0)
				return "<color=#ffafaf>" + value + " " + name + "</color>" + " ";
			if (value > 0)
				return "<color=#afffaf>" + "+" + value + " " + name + "</color>" + " ";
			return "";
		}

		private string FormatStatReq(int value, string name)
		{
			if (value > 0)
				return name + " " + value + " ";
			return "";
		}

		/** Updates labels accoding to item */
		override public void UpdateToolTip()
		{
			if (ItemInstance == null)
				return;

			string attributes = "";

			MDRItem item = ItemInstance.Item;
			MDRItemType type = item.Type;
			IdentificationLevel idLevel = ShowAllInfo ? IdentificationLevel.Full : ItemInstance.IDLevel;

			// stub: getting character from party selected
			MDRCharacter character = CoM.Party.Selected;
			
			IconSprite.Sprite = CoM.Instance.ItemIconSprites[item.IconID];

			if (idLevel >= IdentificationLevel.Mostly)
				HeaderLabel.Caption = FormatHeader(ItemInstance.Name + "\n(" + type.Name + ")");
			else
				HeaderLabel.Caption = FormatHeader(ItemInstance.Name);

			// Usability
			Color skillRequirementColor = Color.yellow;
			Color guildRequirementColor = Color.yellow;

			if (character != null) {
				skillRequirementColor = (character.HasSkillToUseItem(item) ? Color.green : Color.red);
				guildRequirementColor = (item.GuildCanUseItem(character.CurrentGuild) ? Color.green : Color.red);
			}

			// --------------------------------------------
			// Skill requirement  

			if (item.Type.SkillRequired != null && item.SkillLevel > 0)
				attributes += string.Format("Requires {0} - {1}\n", 
					Util.Colorise("[" + item.Type.SkillRequired.Name + "]", skillRequirementColor),
					Util.Colorise(SkillLevelRank.GetSkillRank(item.SkillLevel), skillRequirementColor)
				);

			// --------------------------------------------
			// Guild restrictions

			if (idLevel >= IdentificationLevel.Mostly && item.GuildRestricted) {
				attributes += "Restricted ";

				foreach (MDRGuild guild in CoM.Guilds) {
					if (item.GuildCanUseItem(guild))
						attributes += Util.Colorise(string.Format("[{0}] ", guild), guildRequirementColor);
				}
				attributes += "\n";
			} 

			// Stats

			string statsReq = "";
			
			for (int lp = 0; lp < 6; lp++) {
				if (item.StatsReq[lp] == 0)
					continue;

				string amount = item.StatsReq[lp].ToString();
				
				if ((character != null) && (item.StatsReq[lp] > character.BaseStats[lp]))
					amount = Util.Colorise(amount, Color.red);
				
				statsReq += MDRStats.SHORT_STAT_NAME[lp] + " " + amount + " ";
			}
			
			if (statsReq != "")
				statsReq.Remove(statsReq.Length - 1);
			
			string statsMod =
				FormatStatMod(item.StatsMod.Str, "Str") +
				FormatStatMod(item.StatsMod.Dex, "Dex") +
				FormatStatMod(item.StatsMod.Int, "Int") +
				FormatStatMod(item.StatsMod.Wis, "Wis") +
				FormatStatMod(item.StatsMod.Chr, "Chr") +
				FormatStatMod(item.StatsMod.Con, "Con");
			if (statsMod != "")
				statsMod.Remove(statsMod.Length - 1);
			
			// --------------------------------------------
			// cursed

			if (idLevel == IdentificationLevel.Full) {
				string curseString = "";

				if (item.CurseType == ItemCurseType.Cursed)
					curseString = "[CURSED]\n";
				if (item.CurseType == ItemCurseType.AutoEquipCursed)
					curseString = "[AUTO EQUIP CURSED]\n";

				if ((curseString != "") && (!ItemInstance.Cursed))
					curseString = "[UNCURSED]\n";

				if (curseString != "")
					attributes += Util.Colorise(curseString + "\n", Color.red);

			}

			// --------------------------------------------
			// Hands

			if (item.Hands == 2)
				attributes += FormatNormal("Two Handed") + "\n";			

			attributes += "<Size=6>\n</Size>";

			// --------------------------------------------
			// Attack / Defense

			if (idLevel >= IdentificationLevel.Partial) {
				if (item.isWeapon)
					attributes += "Damage " + FormatHilight((int)item.Damage) + "\n";				
				if (item.Armour > 0)
					attributes += "Armour " + FormatNormal(item.Armour) + "\n";				
			}				

			// --------------------------------------------
			// general information 

			if (idLevel >= IdentificationLevel.Mostly) {


				if (item.Hit != 0) {
					attributes += string.Format("Hit {0}\n", FormatHilight(item.Hit));

				}
				if (item.Pierce != 0) {
					attributes += string.Format("Pierce {0}\n", FormatHilight(item.Pierce));
				}		

				if (item.isWeapon) {										
					if (item.CriticalModifier != 0)
						attributes += FormatHilight("Crit") + Util.Colorise(" " + item.CriticalModifier.ToString("0.0") + "%", new Color(1f, 0.9f, 0.5f));
					if (item.BackstabModifier != 0)
						attributes += FormatHilight("Backstab") + Util.Colorise(" " + item.BackstabModifier.ToString("0.0") + "%", new Color(1f, 0.9f, 0.5f));
					if ((item.BackstabModifier != 0) || (item.CriticalModifier != 0))
						attributes += "\n";
				}						
			}
				
			if (idLevel >= IdentificationLevel.Partial) {
				if (statsReq != "")
					attributes += FormatHilight("Stat Req") + " " + FormatNormal(statsReq) + "\n";
			}
				
			if (idLevel >= IdentificationLevel.Full) {
				if (statsMod != "")
					attributes += FormatHilight("Stat Mod") + " " + FormatNormal(statsMod) + "\n";
			}

			// --------------------------------------------
			// resistances information 

			if (idLevel == IdentificationLevel.Full) {

				string resistanceString = item.Resistance.ToString();
				if (resistanceString != "")
					attributes += string.Format("{0} [{1}]", FormatHilight("Resistances"), resistanceString) + "\n";						
			}

			attributes += "<Size=6>\n</Size>";

			if (!ShowAllInfo)
				attributes += string.Format("You know {0} about this item.", idLevel.Description);

			InfoLabel.Caption = attributes;	

			if (AutoFit) {
				FitToChildren();
				Width += 10;
				Height += 10;
			}

		}
		
	}
	
	
}