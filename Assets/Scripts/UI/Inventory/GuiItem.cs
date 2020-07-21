using UnityEngine;

using System;

using Mordor;

namespace UI
{
	/** UI componenet that displays a single item.  Can be draged between item containers. */
	public class GuiItem : GuiComponent
	{
		/** The item that this component refers to */
		public MDRItemInstance ItemInstance;

		/** Create a new item control */
		public GuiItem()
			: base(42, 42)
		{
		}

		override public bool IsEmpty
		{ get { return (ItemInstance == null) || (ItemInstance.Item == null); } }

		/** 
		 * Draws this item's representation 
		 */
		public override void Draw()
		{
			Engine.PerformanceStatsInProgress.GuiUpdates++;

			if (ItemInstance != null) {
				var item = ItemInstance.Item;
				Sprite icon = CoM.Instance.ItemIconSprites[item.IconID];
				DrawParameters dp = DrawParameters.Default;

				int offsetX = (int)((Width) - icon.rect.width) / 2;
				int offsetY = (int)((Height) - icon.rect.height) / 2;

				SmartUI.Color = Color;
				dp.Transform = ColorTransform;

				if (BeingDragged || !Enabled) {
					SmartUI.Color = Color.gray;
					dp = DrawParameters.BlackAndWhite;
				}

				SmartUI.Draw(X + offsetX, Y + offsetY, icon, dp);
				SmartUI.Color = Color.white;

				if (ItemInstance.Item.Usable) {
					GUIStyle myStyle = CoM.SubtextStyle;
					myStyle.alignment = TextAnchor.LowerLeft;
					SmartUI.TextWithShadow(new Rect(X + offsetX + 2, Y + offsetY, 40, 35), ItemInstance.RemainingCharges.ToString(), myStyle, 1);
				}

			}
		}
	}
}

