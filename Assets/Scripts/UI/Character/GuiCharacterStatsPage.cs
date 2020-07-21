using UnityEngine;
using Mordor;
using Data;
using UI.Generic;

namespace UI
{
	/** Displays a characters stats */
	public class GuiCharacterStatsPage : GuiCharacterPage
	{
		// ------------------------
		// stats
		private GuiLabel stats;
		private GuiLabel statsValue;
		private GuiLabel statsBonus;

		private GuiLabel otherStats;

		private GuiLabel resistanceInfo;

		// ------------------------
		// skills
		private GuiLabel skillNames;
		private GuiLabel skillValues;
		private GuiProgressBar[] skillProgressBar;

		// ------------------------
		// guilds
		private GuiLabel guildInfo;
		private GuiLabel guildLevels;

		private GuiLabel playStatsInfo;

		private Color baseColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
		private Color boldColor = Color.white;
		private Color normalColor = new Color(0.7f, 0.7f, 0.7f);


		/** Creates a new gui character stats page.*/
		public GuiCharacterStatsPage()
		{			
			var headerShadow = new GuiFillRect(0, 0, 0, 30, Color.black.Faded(0.5f));
			headerShadow.Align = GuiAlignment.Top;
			//Add(headerShadow);

			var mainArea = new GuiContainer(WIDTH, HEIGHT - 20);
			mainArea.Y = 35;
			Add(mainArea);

			var statsArea = createStatsArea();
			mainArea.Add(statsArea);

			var skillsArea = createSkillsArea();
			mainArea.Add(skillsArea);

			var guildsArea = createGuildsArea();
			mainArea.Add(guildsArea);

			var buttonGroup = new GuiRadioButtonGroup();

			buttonGroup.OnValueChanged += delegate {
				statsArea.Visible = buttonGroup.SelectedIndex == 0;
				skillsArea.Visible = buttonGroup.SelectedIndex == 1;
				guildsArea.Visible = buttonGroup.SelectedIndex == 2;
			};
				
			buttonGroup.EnableBackground = false;
			buttonGroup.ButtonStyle = Engine.GetStyleCopy("SmallButton");
			buttonGroup.ButtonSize = new Vector2(80, 24);
			buttonGroup.ButtonSpacing = 5;
			buttonGroup.AddItem("Stats");
			buttonGroup.AddItem("Skills");
			buttonGroup.AddItem("Guilds");
			buttonGroup.Y = -5;
			Add(buttonGroup, 0);
		}

		private GuiContainer createStatsArea()
		{
			var container = new GuiContainer(WIDTH, HEIGHT);

			stats = new GuiLabel(10, 0, "");
			stats.FontColor = baseColor;
			container.Add(stats);

			statsValue = new GuiLabel(60, 0, "");
			statsValue.FontColor = baseColor;
			container.Add(statsValue);

			statsBonus = new GuiLabel(90, 0, "");
			container.Add(statsBonus);

			otherStats = new GuiLabel(10, 150, "");
			otherStats.FontSize = 12;
			container.Add(otherStats);

			resistanceInfo = new GuiLabel(130, 0, "");
			resistanceInfo.FontColor = baseColor;
			resistanceInfo.FontSize = 14;
			container.Add(resistanceInfo);

			playStatsInfo = new GuiLabel(10, 200, "");
			playStatsInfo.FontColor = baseColor;
			playStatsInfo.FontSize = 10;
			container.Add(playStatsInfo);				

			return container;
		}

		private GuiContainer createSkillsArea()
		{			
			var container = new GuiContainer(WIDTH, HEIGHT);

			skillNames = new GuiLabel(10, 0, "");
			skillNames.FontColor = baseColor;
			skillNames.FontSize = 14;

			skillValues = new GuiLabel(140, 0, "", 100);
			skillValues.FontColor = baseColor;
			skillValues.TextAlign = TextAnchor.MiddleCenter;
			skillValues.FontSize = 14;

			skillProgressBar = new GuiProgressBar[CoM.Skills.Count];

			for (int lp = 0; lp < CoM.Skills.Count; lp++) {
				var progressBar = new GuiProgressBar(100, 16);
				progressBar.X = 140;
				progressBar.Color = Color.black.Faded(0.25f);
				progressBar.ProgressColor = Color.gray.Faded(0.5f);
				progressBar.EnableBackground = true;
				progressBar.Visible = false;
				skillProgressBar[lp] = progressBar;
				container.Add(progressBar);
			}

			container.Add(skillNames);
			container.Add(skillValues);

			return container;
		}

		private GuiContainer createGuildsArea()
		{
			var container = new GuiContainer(WIDTH, HEIGHT);

			guildInfo = new GuiLabel(10, 0, "");
			guildInfo.FontColor = baseColor;
			container.Add(guildInfo);

			guildLevels = new GuiLabel(120, 0, "");
			guildLevels.FontColor = new Color(0.7f, 0.7f, 0.7f);
			guildLevels.TextAlign = TextAnchor.MiddleRight;
			container.Add(guildLevels);

			return container;
		}

		private string bonusStat(int value)
		{
			if (value < 0)
				return Util.Colorise(value.ToString(), Color.red);
			if (value > 0)
				return Util.Colorise("+" + value, Color.green);
			return "";
		}

		private void updateStats()
		{			
			stats.Caption = "";
			statsValue.Caption = "";
			statsBonus.Caption = "";
			for (int lp = 0; lp < 6; lp++) {
				bool bold = (Character.BaseStats[lp] == Character.MaxStats[lp]);
				stats.Caption += MDRStats.SHORT_STAT_NAME[lp] + "\n";
				statsValue.Caption += Util.Colorise(Character.BaseStats[lp].ToString(), bold ? boldColor : normalColor, bold) + "\n";
				statsBonus.Caption += bonusStat(Character.ItemStats[lp]) + "\n";
			}

			otherStats.Caption = string.Format("Hit: {0} Pierce: {1}\nCrit: {2} Backstab: {3}", 
				formatPercent(Character.TotalHitBonus), 
				formatValue((int)Character.TotalArmourPierce), 
				formatPercent(Character.TotalCriticalHitChance), 
				formatPercent(Character.TotalBackstabChance)
			);

			string resistanceString = "";
			foreach (string resistanceName in MDRResistance.ResistanceNames)
				if (Character.Resistance[resistanceName] > 0)
					resistanceString += string.Format("{0} {1}\n", resistanceName, Character.Resistance[resistanceName]);

			if (resistanceString == "")
				resistanceString = "No resistances";

			resistanceInfo.Caption = resistanceString;
					
			playStatsInfo.Caption = 
				"Play time: " + (Character.PlayTime / 60f / 60f).ToString("0.0") + " hours" + "\n" +
			"Deaths: " + Util.Comma(Character.Deaths) + "\n" +
			"Monsters Killed:" + Util.Comma(Character.MonstersKilled);
		}

		private void updateSkills()
		{			
			var skillNamesString = "";
			var skillValuesString = "";
			var yPos = 0;

			//stub: debug magic missile
			if (Character.SkillLevel[0] >= 10) {
				var mm = CoM.Spells["Magic Missile"];
				Trace.Log("{0} {1} {2} {3}", Character.SkillLevel[0], mm.SpellLevel, Character.CanCast(mm), Character.SpellPower(mm));
			}

			for (int lp = 0; lp < CoM.Skills.Count; lp++) {

				var skill = CoM.Skills[lp];

				var skillLevel = Character.SkillLevel[skill.ID];

				if (skillLevel < 10) {
					skillProgressBar[lp].Visible = false;
					continue;
				} else {
					skillProgressBar[lp].Visible = true;
				}
					
				MDRGuild fromGuild;
				GameRules.CharacterGeneralAbility(Character, skill, out fromGuild);

				string rankName = SkillLevelRank.GetSkillRank(skillLevel);
				Color rankColor = SkillLevelRank.GetSkillColor(skillLevel);

				skillNamesString += skill.Name + "\n";
				skillValuesString += Util.Colorise(rankName + " (" + skillLevel + ")", rankColor) + "\n";

				skillProgressBar[lp].Y = yPos;
				skillProgressBar[lp].Progress = (skillLevel % 10) / 10f;

				yPos += 17;
			}

			skillNames.Caption = skillNamesString;
			skillValues.Caption = skillValuesString;
		}

		private void updateGuilds()
		{
			var guildString = "";
			var guildLevelString = "";

			foreach (MDRGuildMembership membership in Character.Membership) {
				if (membership.IsMember) {
					guildString += membership.Guild + "\n";
					guildLevelString += "[" + membership.CurrentLevel + "]" + "\n";
				}
			}
			guildInfo.Caption = guildString;
			guildLevels.Caption = guildLevelString;
		}

		override public void Sync()
		{
			if (Character == null) {
				stats.Caption = "";
				return;
			}
				
			updateStats();
			updateSkills();
			updateGuilds();
		}
	}
}		