using System;
using UI.DragDrop;
using UnityEngine;

namespace UI
{
	/** A slot containing a character which can be dragged around. */
	public class GuiCharacterSlot : DDContainer
	{
		public GuiCharacterPortrait CharacterPortrait { get { return (DDContent as GuiCharacterPortrait); } set { DDContent = value; } }

		private GuiLabel nameTag;
		private GuiLabel guildTag;

		public GuiCharacterSlot()
			: base(0, 0)
		{
			Style = Engine.GetStyleCopy("Frame");
			InnerShadow = true;
			OuterShadow = true;
			OuterShadowSprite = ResourceManager.GetSprite("Icons/OuterEdge");
			SizeForContent(58, 64);

			CaptionDropShadow = true;
			FontSize = 18;
			TextAlign = TextAnchor.MiddleCenter;

			nameTag = new GuiLabel("", 110);
			nameTag.FontSize = 16;
			nameTag.FontColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
			nameTag.DropShadow = true;
			nameTag.TextAlign = TextAnchor.MiddleCenter;
			nameTag.EnableBackground = true;
			nameTag.Color = new Color(0.2f, 0.2f, 0.2f);

			guildTag = new GuiLabel("", 110, 25);
			guildTag.FontColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
			guildTag.FontSize = 14;
			guildTag.DropShadow = true;
			guildTag.TextAlign = TextAnchor.LowerCenter;
			guildTag.EnableBackground = true;
			guildTag.Color = new Color(0.1f, 0.1f, 0.1f);

			Apply();
		}

		public override void Draw()
		{
			base.Draw();

			if (!IsEmpty) {
				guildTag.X = (int)this.Bounds.center.x - (guildTag.Width / 2);
				guildTag.Y = Y + Height + 0;
				guildTag.Draw();

				nameTag.X = (int)this.Bounds.center.x - (nameTag.Width / 2);
				nameTag.Y = Y + Height - 15;
				nameTag.Draw();
			}
		}

		public override void DDContentChanged()
		{			
			base.DDContentChanged();
			Apply();
		}

		/** Applies character changes to UI elements. */
		public void Apply()
		{
			if (IsEmpty)
				return;

			nameTag.Caption = "<B>" + CharacterPortrait.Character.Name + "</B>";
			guildTag.Caption = CharacterPortrait.Character.CurrentGuild + " lv" + CharacterPortrait.Character.CurrentLevel;
		}

		public override bool CanReceive(GuiComponent value)
		{
			return value == null || value is GuiCharacterPortrait;
		}
	}
}

