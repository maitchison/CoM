using UnityEngine;

using Mordor;
using System;

namespace UI
{
	/** Displays information about a monster.  Autosizes, and is invisibile if no Monster is present. */
	public class GuiMonsterToolTip : GuiToolTip<MDRMonster>
	{
		/** The Monster to display information about */
		public MDRMonster Monster { get { return Data; } set { Data = value; } }

		/** If true displays how many times this monster has been found */
		public bool ShowEncounterStats = false;

		private string getHeaderText()
		{
			//todo:
			return "";
			//return Monster == null ? "" : FormatHeader(Monster.Name + ((Monster.StackSize < 2) ? "" : " [" + Monster.StackSize + "]"));
		}

		public GuiMonsterToolTip()
			: base()
		{
		}

		/** Updates the text for the label that displays this monsters information */
		override public void UpdateToolTip()
		{
			if (Monster == null) {
				Sprite = null;
				Header = "";
				Info = "";
				return;
			}

			if (Visible) {
				Width = (int)Style.CalcSize(new GUIContent(getHeaderText())).x + 90 + 20;
				if (Width < 400)
					Width = 400;
			}

			Sprite = CoM.Instance.MonsterSprites[Monster.PortraitID];
			Header = getHeaderText();
			var Body = "";

			Body += "Damage: " + Monster.Damage + "\n";		
			Body += "Hit: " + Monster.BonusHit + " Pierce: " + Monster.ArmourPierce + "\n";
			Body += "Average HP: " + Monster.HitPoints + "\n";
			Body += "First appears on level " + Monster.AppearsOnLevel + "\n";
			Body += (Monster.CanCastSpells ? "Can cast: " + Monster.KnownSpells : "Can not cast spells") + "\n";
			Body += Monster.Abilities + "\n" + "\n";
			Body += "Appears " + Util.DescribeChance(Monster.EncounterChance) + " in groups of " + Monster.SpawnCount + "\n";
			if (Monster.Companion != null)
				Body += "May appear with " + Monster.Companion.Name + "\n";

			if (ShowEncounterStats) {
				var record = CoM.GameStats.MonsterStats[Monster];
				if (record != null) {
					if (record.NumberSeen > 0) {

						Body += "\n";

						if (record.CharacterKills > 0)
							Body += "Has been responsible for " + Util.Plural(record.CharacterKills, "death") + "\n";

						Body +=	"Seen " + Util.Plural(record.NumberSeen, "time") + "\n";
						Body +=	"First seen by " + record.FirstSeenBy + "\n";
						Body +=	"Last seen at " + record.LastSeenLocation + " ";

						int deltaDays = (DateTime.Now - record.LastSeenDate).Days;
						if (deltaDays > 0)
							Body += Util.FormatDays(deltaDays) + " ago";
						if (deltaDays == 0)
							Body += "today";
					}
				}
			}

			Info = Body;

		}

		
	}
	
	
}
