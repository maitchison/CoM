
using System;
using Mordor;
using UnityEngine;

namespace UI
{
	/** List of adjustable stats */
	public class GuiStatList : GuiWindow
	{
		private GuiStatAdjustment[] statAdjuster;
		public int FreePoints = 0;

		public GuiStatList(MDRStats stats) : base(250, 240)
		{
			WindowStyle = GuiWindowStyle.Titled;
			Title = "Stats";
			statAdjuster = new GuiStatAdjustment[6];
			int yPos = 0;
			int increment = GuiStatAdjustment.HEIGHT + 5;
			yPos -= increment;
			statAdjuster[0] = new GuiStatAdjustment(0, yPos += increment, "Strength", stats.Str);
			statAdjuster[1] = new GuiStatAdjustment(0, yPos += increment, "Intelligence", stats.Int);
			statAdjuster[2] = new GuiStatAdjustment(0, yPos += increment, "Wisdom", stats.Wis);
			statAdjuster[3] = new GuiStatAdjustment(0, yPos += increment, "Constitution", stats.Con);
			statAdjuster[4] = new GuiStatAdjustment(0, yPos += increment, "Charasma", stats.Chr);
			statAdjuster[5] = new GuiStatAdjustment(0, yPos += increment, "Dexterity", stats.Dex);

			for (int lp = 0; lp < 6; lp++)
				Add(statAdjuster[lp]);
		}

		public override void Update()
		{
			base.Update();

			if (Settings.Advanced.UnlimitedCharacterPoints)
				FreePoints = 99;
		}

		/** Apples current values to given character */
		public void ApplyToCharacter(MDRCharacter character)
		{
			for (int lp = 0; lp < 6; lp++) {
				character.BaseStats[lp] = statAdjuster[lp].Value;
				character.MaxStats[lp] = statAdjuster[lp].MaxValue;
				character.ApplyChanges();
			}
		}

		/** Sets the stat defaults and limits based on a given races*/
		public void SetRace(MDRRace race)
		{
			for (int lp = 0; lp < 6; lp++) {
				statAdjuster[lp].MinValue = race.MinStats[lp];
				statAdjuster[lp].MaxValue = race.MaxStats[lp];
				statAdjuster[lp].Value = race.DefaultStats[lp];
			}
			FreePoints = race.BonusPoints;
		}
	}

	/** A an adjustable stat with min / max */
	public class GuiStatAdjustment : GuiPanel
	{
		/** Fixed height of the control */
		public static int HEIGHT = 26;

		/** The stats value */
		public int Value;

		/** The max value of the stat */
		public int MaxValue;

		/** The minimum value of the stat */
		public int MinValue;

		/** If true the minumum and maximum values will be shown */
		public bool ShowMinMax = false;

		private GuiButton decButton;
		private GuiButton incButton;
		private GuiLabel nameLabel;
		private GuiLabel valueLabel;
		private string statName;

		public GuiStatAdjustment(int x, int y, string statName, int startingValue) : base(250, HEIGHT)
		{
			X = x;
			Y = y;
			PanelMode = GuiPanelMode.Square;

			Color = new Color(0.25f, 0.25f, 0.25f, 0.0f);

			this.Value = startingValue;
			this.statName = statName;
			this.MinValue = 5;
			this.MaxValue = 10;

			nameLabel = new GuiLabel(0, 0, "");
			nameLabel.FontColor = Color.white;
			nameLabel.TextAlign = TextAnchor.MiddleLeft;
			Add(nameLabel, 4, 0);

			valueLabel = new GuiLabel(0, 0, "", 20, 16);
			valueLabel.TextAlign = TextAnchor.MiddleCenter;
			Add(valueLabel);

			decButton = new GuiButton("<", 20, 17);
			Add(decButton, Width - 62, 1);

			incButton = new GuiButton(">", 20, 17);
			Add(incButton, Width - 21, 1);

			incButton.OnMouseClicked += delegate {
				if ((Value < MaxValue) && (FreePoints > 0)) {
					FreePoints--;
					Value++;
					Refresh();
				}
			};

			decButton.OnMouseClicked += delegate {
				if (Value > MinValue) {
					FreePoints++;
					Value--;
					Refresh();
				}
			};

		}

		/** Access to parents FreePoints property.  Parent must be a GuiStatList */
		public int FreePoints {
			get {
				if (!(Parent is GuiStatList))
					throw new Exception("Parent is not a GuiStatList");
				return (Parent as GuiStatList).FreePoints;
			}
			set {
				if (!(Parent is GuiStatList))
					throw new Exception("Parent is not a GuiStatList");
				(Parent as GuiStatList).FreePoints = value;
			}

		}

		/** Updates the stat adjustment text */
		public override void Update()
		{
			base.Update();
			Refresh();
		}

		/** Updates hte contents of the controls */
		private void Refresh()
		{
			valueLabel.Caption = Value.ToString();
			if (!ShowMinMax)
				nameLabel.Caption = statName;
			else
				nameLabel.Caption = statName + " (" + MinValue + "-" + MaxValue + ")";
			
			PositionComponent(decButton, -60, 0);
			PositionComponent(valueLabel, -40, 0);
			PositionComponent(incButton, -20, 0);
			incButton.Y--;
			decButton.Y--; //positioning is a little off (maybe because of the shadow?)
		}


	}
}