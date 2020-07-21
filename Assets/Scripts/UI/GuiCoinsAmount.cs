using System;
using UnityEngine;

namespace UI
{
	/** Show amount of 'coin' as Gold Silver and Copper */
	public class GuiCoinAmount : GuiContainer
	{
		private static Sprite GoldCoinSprite;
		private static Sprite SilverCoinSprite;
		private static Sprite CopperCoinSprite;

		private GuiImage goldIcon;
		private GuiImage silverIcon;
		private GuiImage copperIcon;

		private GuiLabel goldAmountLabel;
		private GuiLabel silverAmountLabel;
		private GuiLabel copperAmountLabel;

		/** The amount of coins to display. */
		public int _value;

		public int Value {
			get {
				return _value;
			}
			set {
				if (_value == value)
					return;
				_value = value;
				_dirty = true;
			}
		}

		/** If true gold, silver, and copper amounts are always shown even if they are zero. */
		public bool AlwaysShowAll = false;

		private int gold {
			get { return (int)(_value / 10000); }
		}

		private int silver {
			get { return (int)(_value / 100) % 100; }
		}

		private int copper {
			get { return _value % 100; }
		}

		private bool _dirty = false;

		private static void fetchGraphics()
		{
			if (GoldCoinSprite == null)
				GoldCoinSprite = ResourceManager.GetSprite("Icons/Coin_Gold");
			if (SilverCoinSprite == null)
				SilverCoinSprite = ResourceManager.GetSprite("Icons/Coin_Silver");
			if (CopperCoinSprite == null)
				CopperCoinSprite = ResourceManager.GetSprite("Icons/Coin_Copper");
		}

		private void updateComponents()
		{
			goldAmountLabel.Caption = gold.ToString();
			silverAmountLabel.Caption = silver.ToString();
			copperAmountLabel.Caption = copper.ToString();

			int componentSpacing = 15;
			int iconSize = 16;

			int position = 0;

			bool showGold = gold != 0 || AlwaysShowAll;
			bool showSilver = silver != 0 || AlwaysShowAll;
			bool showCopper = copper != 0 || AlwaysShowAll || Value == 0;

			if (showGold) {
				goldIcon.X = position;
				position += iconSize;
				goldAmountLabel.X = position;
				position += componentSpacing;
			}

			if (showSilver) {
				silverIcon.X = position;
				position += iconSize;
				silverAmountLabel.X = position;
				position += componentSpacing;
			}

			if (showCopper) {
				copperIcon.X = position;
				position += iconSize;
				copperAmountLabel.X = position;
				position += componentSpacing;
			}
				
			goldIcon.Visible = goldAmountLabel.Visible = showGold;
			silverIcon.Visible = silverAmountLabel.Visible = showSilver;
			copperIcon.Visible = copperAmountLabel.Visible = showCopper;

			this.FitToChildren();

			_dirty = false;

			Invalidate();
		}

		public GuiCoinAmount()
			: base(32 * 3, 22)
		{
			fetchGraphics();
			goldIcon = new GuiImage(0, 3, GoldCoinSprite);
			silverIcon = new GuiImage(0, 3, SilverCoinSprite);
			copperIcon = new GuiImage(0, 3, CopperCoinSprite);
			goldAmountLabel = new GuiLabel("");
			silverAmountLabel = new GuiLabel("");
			copperAmountLabel = new GuiLabel("");

			Add(goldIcon);
			Add(silverIcon);
			Add(copperIcon);
			Add(goldAmountLabel);
			Add(silverAmountLabel);
			Add(copperAmountLabel);

			updateComponents();
		}

		public override void Update()
		{
			if (_dirty)
				updateComponents();
			base.Update();
		}
	}
}

