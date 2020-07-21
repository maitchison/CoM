using System;
using Mordor;
using UnityEngine;

namespace UI
{
	/** Lists party memebers horizontally */
	public class GuiPartySpan : GuiPanel
	{
		public static int WIDTH = 600;
		public static int HEIGHT = 122;

		private static int BLOCK_WIDTH = 125;
		private static int PORTAIT_WIDTH = 58 + 4;

		public MDRParty Party { get { return _party; } }

		/** If true an edit button will be added. */
		public bool Editable = true;

		private GuiImage[] portraits;
		private GuiLabel[] names;
		private MDRParty _party;
		private GuiLabel locationLabel;

		private GuiButton editButton;

		public GuiPartySpan(MDRParty party)
			: base(WIDTH, HEIGHT)
		{
			DepressedOffset = 1;

			Style = Engine.GetStyleCopy("SmallButton");
			Style.padding = new RectOffset(10, 12, 1, 3);

			portraits = new GuiImage[4];
			names = new GuiLabel[4];

			TextAlign = TextAnchor.MiddleCenter;
			FontSize = 22;
			CaptionDropShadow = true;

			editButton = new GuiButton("Edit", 80, 30);
			editButton.ColorTransform = ColorTransform.Saturation(0.5f);
			Add(editButton, -5, 30, true);
			editButton.Visible = false;

			locationLabel = new GuiLabel("", Width, 23);
			locationLabel.FontSize = 16;
			locationLabel.FontColor = Color.yellow;
			locationLabel.Color = Color.black.Faded(0.6f);
			locationLabel.EnableBackground = true;
			locationLabel.TextAlign = TextAnchor.MiddleCenter;
			locationLabel.Align = GuiAlignment.Bottom;

			Add(locationLabel);

			editButton.OnMouseClicked += delegate {		
				if (Party != null) {
					Engine.PushState(new EditPartyState(Party));
				}
			};

			this._party = party;

			for (int lp = 0; lp < 4; lp++) {
				portraits[lp] = new GuiImage(lp * BLOCK_WIDTH + ((BLOCK_WIDTH - PORTAIT_WIDTH) / 2), 7);	
				portraits[lp].FrameStyle = Engine.GetStyleCopy("Frame");
				portraits[lp].Framed = true;
				portraits[lp].InnerShadow = true;
				portraits[lp].OuterShadow = true;
				portraits[lp].OuterShadowSprite = ResourceManager.GetSprite("Icons/OuterEdge");
				portraits[lp].OuterShadowColor = Color.black.Faded(0.25f);
				Add(portraits[lp]);

				names[lp] = new GuiLabel("");
				names[lp].FontColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
				names[lp].DropShadow = true;
				names[lp].TextAlign = TextAnchor.MiddleCenter;
				names[lp].X = 15 + lp * 125;
				names[lp].Y = 70;
				names[lp].Width = BLOCK_WIDTH - 30;
				names[lp].EnableBackground = true;
				names[lp].Color = new Color(0.2f, 0.2f, 0.2f);
				Add(names[lp]);
			}

			apply();
		}

		public override void Update()
		{
			base.Update();

			editButton.Visible = Editable;
			editButton.SelfEnabled = Party != null && Party.IsInTown;
		}

		public override void Draw()
		{
			CompositeColorTransform = ColorTransform.Identity;

			if (IsMouseOver && MouseOverComponent != editButton)
				CompositeColorTransform = ColorTransform.Multiply(new Color(1.3f, 1.3f, 1.2f));

			if (Depressed)
				CompositeColorTransform = ColorTransform.Multiply(new Color(0.8f, 0.8f, 0.8f));

			base.Draw();

		}

		private void apply()
		{
			for (int lp = 0; lp < 4; lp++) {
				portraits[lp].Sprite = null;
				portraits[lp].Visible = false;
				names[lp].Caption = "";
				names[lp].Visible = false;
			}

			if (Party == null) {
				editButton.SelfEnabled = false;
				locationLabel.Caption = "";
				return;
			}

			editButton.SelfEnabled = true;
			locationLabel.Caption = Party.DepthDescription;

			for (int lp = 0; lp < 4; lp++) {
				if (_party[lp] == null)
					continue;
				
				portraits[lp].Visible = true;
				portraits[lp].Sprite = _party[lp].Portrait;
				names[lp].Caption = _party[lp].Name;
				names[lp].Visible = true;

				if (_party[lp].IsDead) {
					portraits[lp].ImageColorTransform = ColorTransform.BlackAndWhite;
					portraits[lp].ImageColor = new Color(0.5f, 0.3f, 0.3f);
				} else {
					portraits[lp].ImageColorTransform = ColorTransform.Identity;
					portraits[lp].ImageColor = Color.white;
				}
			}
		}
	}
}

