using UnityEngine;
using Mordor;
using UI;
using UI.Generic;
using Data;

namespace UI.State.Town
{
	/** The state to handle the town guild */
	public class GuildState : TownBuildingState
	{
		private GuiContainer GuildListSection;
		private GuiContainer GuildInfoSection;
		private GuiLabel GuildInfo;
		private GuiLabel GuildTitle;
		private GuiListBox<MDRGuild> GuildList;
		private GuiButton JoinButton;
		private GuiButton LevelButton;

		private MDRGuild SelectedGuild { get { return GuildList.Selected; } }

		public GuildState()
			: base("Guild")
		{
			Util.Assert(CoM.AllDataLoaded, "Data must be loaded before GuildState can be created.");

			MainWindow.Width = 600;
			MainWindow.Height = 450;

			// --------------------------------------

			MainWindow.InnerShadow = true;
			MainWindow.Background.Align = GuiAlignment.None;
			MainWindow.Background.Sprite = ResourceManager.GetSprite("Backgrounds/TownGuild");
			MainWindow.Background.Color = Color.white;
			MainWindow.Background.BestFit(MainWindow.ContentsFrame, true);
			MainWindow.PositionComponent(MainWindow.Background, 0, 0);

			GuildInfoSection = new GuiContainer((int)MainWindow.ContentsBounds.width - 200, (int)MainWindow.ContentsBounds.height);
			MainWindow.Add(GuildInfoSection, -1, 0);

			GuildListSection = new GuiContainer(205, (int)MainWindow.ContentsBounds.height + 10);
			GuildListSection.X = -5;
			GuildListSection.Y = -5;
			GuildListSection.Style = Engine.GetStyleCopy("Frame");
			GuildListSection.EnableBackground = true;
			GuildListSection.Color = new Color(1f, 1f, 1f, 0.75f);
			MainWindow.Add(GuildListSection);

			GuildList = new GuiListBox<MDRGuild>(0, 0, 180, 300);
			GuildListSection.Add(GuildList, 15, 15);

			GuildTitle = new GuiLabel(-5, -5, "", GuildInfoSection.Width, 50);
			GuildTitle.Align = GuiAlignment.Top;
			GuildTitle.TextAlign = TextAnchor.MiddleCenter;
			GuildTitle.FontSize = 22;
			GuildInfoSection.Add(GuildTitle);

			GuildInfo = new GuiLabel(10, 50, "", (int)GuildInfoSection.ContentsFrame.width - 10);
			GuildInfo.WordWrap = true;
			GuildInfoSection.Add(GuildInfo);

			// --------------------------------------

			JoinButton = new GuiButton("Join", 120);
			GuildInfoSection.Add(JoinButton, 0, -20);
			
			LevelButton = new GuiButton("Level", 120);
			GuildInfoSection.Add(LevelButton, 0, -20);

			GuildList.OnSelectedChanged += delegate {
				updateGuildInfo();
			};
			JoinButton.OnMouseClicked += delegate {
				JoinGuild();
			};
			LevelButton.OnMouseClicked += delegate {
				LevelUp();
			};

			GuildList.DoConvertItemToString = delegate(MDRGuild guild) {

				bool hasPrereq = (guild.RequiredGuild != null);
				bool couldJoin = guild.CanAccept(Character);
				bool isMemeber = Character.Membership[guild].IsMember;
				bool currentMember = (guild == Character.CurrentGuild);

				string levelInfo = isMemeber ? "(" + Character.Membership[guild].CurrentLevel + ")" : ""; 

				string guildDescription = guild.Name + levelInfo;

				if (hasPrereq)
					guildDescription = "  " + guildDescription;

				if (currentMember)
					guildDescription = Util.Colorise(guildDescription, Color.yellow, true);
				else if (isMemeber)
					guildDescription = Util.Colorise(guildDescription, new Color(0.9f, 0.9f, 0.9f));
				else if (couldJoin)
					guildDescription = Util.Colorise(guildDescription, new Color(0.9f, 0.9f, 0.9f));
				else
					guildDescription = Util.Colorise(guildDescription, new Color(0.7f, 0.7f, 0.7f));
				
				return guildDescription;
			};

			CoM.Party.OnSelectedChanged += delegate {
				RefreshUI();
			};

			RepositionControls();
			RefreshUI();

		}

		/** Trys to let the current character join the currently selected guild */
		private void JoinGuild()
		{
			MDRGuild guild = SelectedGuild;

			if (guild == null)
				return;
			if (Character == null)
				return;

			if (Character.CurrentGuild == guild) {
				Engine.ShowModal("Can not join guild", "This is already your current guild.");
				return;
			}

			if (Character.Membership[guild].IsMember) {
				Character.Membership.CurrentGuild = guild;
				RefreshUI();
				Engine.ShowModal("Welcome", "Welcome back to the " + guild.Name + " guild.");
				return;
			}

			if (guild.CanAccept(Character)) {
				if (Character.Membership.JoinedGuilds >= 2) {
					if (CoM.Party.TotalHP >= SelectedGuild.JoinCost) {
						CoM.Party.DebitGold(SelectedGuild.JoinCost, Character);
					} else {
						Engine.ShowModal("Can not join guild", "Not enough gold.");
						return;
					}
				}

				Character.JoinGuild(guild);
				RefreshUI();
				Engine.ShowModal("Guild Joined", "Welcome to the " + guild.Name + " guild!");
			} else
				Engine.ShowModal("Can not join guild", "");

		}

		/** Attempts to let the current character level up in their current guild */
		private void LevelUp()
		{
			// make sure we are a current member of this guild.
			if (Character.CurrentMembership.Guild != SelectedGuild) {
				Engine.ShowModal("Not able to level", "You are not currently a member of this guild.");
				return;
			}
			
			// check for death
			if (Character.IsDead) {
				Engine.ShowModal("Not able to level", "This character is dead.");
				return;
			}

			// check for xp
			if (Character.CurrentMembership.ReqXP > 0) {
				Engine.ShowModal("Not able to level", "Not enough experiance.");
				return;
			}

			// check for gold
			if (CoM.Party.Gold < Character.CurrentMembership.RequiredGold) {
				Engine.ShowModal("Not able to level", "Not enough gold.");
				return;
			}

			// gain level
			Character.GainLevel();

			// pay gold
			CoM.Party.DebitGold(Character.CurrentMembership.RequiredGold, Character);

			Engine.ShowModal("Leveled Up!", "Welcome to level " + Character.CurrentMembership.CurrentLevel);

			SoundManager.Play("OLDFX_MAKELEV");

			RefreshUI();
		}

		public override void Update()
		{
			base.Update();
		}

		/** 
		 * Populates the guild list with guilds this character is able to join 
		 */
		private void updateGuildList()
		{
			GuildList.Clear();
			foreach (MDRGuild guild in CoM.Guilds) {
				if (guild.CanAcceptRace(Character.Race))
					GuildList.Add(guild);
			}

			GuildList.Selected = Character.CurrentGuild;
		}

		/** Refreshes UI controls */
		private void RefreshUI()
		{	
			updateGuildList();
			updateGuildInfo();
		}

		private bool selectedGuildIsCurrent {
			get { return ((Character != null) && (SelectedGuild != null)) && (Character.Membership.CurrentGuild == SelectedGuild); }
		}

		private bool canLevel()
		{
			if ((SelectedGuild == null) || (Character == null))
				return false;
			if (!Character.Membership[SelectedGuild].IsMember)
				return false;
			if (Character.CurrentGuild != SelectedGuild)
				return false;
			return (Character.CurrentMembership.ReqXP <= 0);
		}

		private bool canJoinSelectedGuild()
		{
			if ((SelectedGuild == null) || (Character == null))
				return false;
			if (Character.Membership[SelectedGuild].IsMember)
				return true;

			return (Character.BaseStats >= SelectedGuild.RequiredStats) && SelectedGuild.MeetsLevelRequiredments(Character);
		}

		/** Refreshs the guild info text as per the selected guild */
		private void updateGuildInfo()
		{

			JoinButton.SelfEnabled = canJoinSelectedGuild();
			LevelButton.SelfEnabled = canLevel();

			JoinButton.Visible = !selectedGuildIsCurrent;
			LevelButton.Visible = selectedGuildIsCurrent;

			if ((SelectedGuild == null) || (Character == null))
				return;

			JoinButton.Caption = (Character.Membership[SelectedGuild].IsMember) ? "Reacquaint" : "Join";

			MDRGuildMembership membership = Character.Membership[SelectedGuild];	

			string requiredStatsString = "";
			for (int lp = 0; lp <= MDRStats.SHORT_STAT_NAME.Length; lp++) {
				int requirement = SelectedGuild.RequiredStats[lp];
				if (requirement != 0)
					requiredStatsString += Util.Colorise(MDRStats.SHORT_STAT_NAME[lp] + ":" + requirement + " ", Character.Stats[lp] >= requirement ? Color.green : Color.red);
			}

			GuildTitle.Caption = "<B>" + SelectedGuild.Name + "</B>";

			string text = "";

			if (requiredStatsString != "")
				text += "Required Stats: \n " + requiredStatsString + "\n\n";

			if (membership.IsMember) {
				text += "You are a member of this guild. \n";
				if (membership.ReqXP > 0) {
					text += "You require " + Util.Colorise(Util.Comma(membership.ReqXP), Color.green) + " more XP to obtain level " + Util.Colorise((membership.CurrentLevel + 1).ToString(), Color.green) + "\n";
				} else {
					text += "You are ready for level " + Util.Colorise((membership.CurrentLevel + 1).ToString(), Color.green) + "\n";
				}
				text += "\nGaining this level will cost " + CoM.CoinsAmount(membership.RequiredGold) + ".";

			} else {
				
				text += "You are not a member of this guild. \n";
				if (Character.BaseStats >= SelectedGuild.RequiredStats)
					text += "You " + Util.Colorise("meet the stats requirements", Color.green) + " for this guild. \n";
				else
					text += "You do not meet stats requirements for this guild. \n\n";

				if (membership.Guild.RequiredGuild != null)
					text += "You must obtain level " + Util.Colorise(membership.Guild.RequiredGuildRequiredLevel, Color.green) + " with " + Util.Colorise(membership.Guild.RequiredGuild.Name, Color.green) + " first before joining this guild.\n\n";
				
				if (Character.Membership.JoinedGuilds <= 1)
					text += "As this is your second guild, you may join for free. \n\n";
				else
					text += "You require " + Util.Colorise(Util.Comma(SelectedGuild.JoinCost), Color.yellow) + " gold to join. \n\n";
			}

			GuildInfo.Caption = text;
			
		}
	}
}