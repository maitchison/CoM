
using UnityEngine;

using Data;

namespace UI
{
	public class GuiAction : GuiComponent
	{
		public static int SIZE = 48;

		private static GuiComponent toolTip;

		public MDRAction Action;

		public GuiAction(MDRAction action)
			: base(SIZE, SIZE)
		{
			EnableBackground = false;
			Action = action;
		}

		// ------------------------------------------------------------------
		// Public
		// ------------------------------------------------------------------

		public override void DrawContents()
		{
			Sprite icon = Action == null ? null : Action.Icon;
			if (icon != null) {

				var drawParameters = DrawParameters.Default;
				if (!Enabled) {
					drawParameters.Transform = ColorTransform.BlackAndWhite;
					drawParameters.Transform.ColorOffset = new Color(0.2f, 0.2f, 0.2f, 0f);
				}
				drawParameters.Scale = new Vector2(SIZE / icon.rect.width, SIZE / icon.rect.height);
				SmartUI.Draw(0, 0, icon, drawParameters);
			}

			if (Action != null) {
				if (Action.Type == ActionType.Spell) {
					var style = CoM.SubtextStyle;
					style.alignment = TextAnchor.LowerCenter;
					string costString = Action.Spell.CostFor(CoM.Party.Selected).ToString();
					Rect rect = ContentsFrame;
					rect.height -= 4;
					SmartUI.TextWithShadow(rect, costString, style, 1);
				}

			}
		}

		public override void Update()
		{
			base.Update();

			if (Action != null) {

				switch (Action.Type) {
					case ActionType.Spell:
						int spellCost = Action.Spell.CostFor(CoM.Party.Selected);
						SelfEnabled = CoM.Party.Selected.CanCast(Action.Spell) && (CoM.Party.Selected.Spells >= spellCost);
						break;
				}

			}
		}

		public override bool IsEmpty {
			get { 
				return Action.IsEmpty;
			}
		}

		public override string ToString()
		{
			return Action.ToString();
		}
			
		// ------------------------------------------------------------------
		// Private
		// ------------------------------------------------------------------
		
	}
}