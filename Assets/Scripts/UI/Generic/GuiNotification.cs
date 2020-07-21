using UnityEngine;

using System;
using UI;

namespace UI.Generic
{
	public enum NotificationStage
	{
		AnimateOn,
		Active,
		AnimateOff,
		Destroyed
	}

	public class GuiNotification : GuiPanel
	{
		/** Number of seconds this notification has left in it's current stage */
		protected float stageLife;

		protected static float AnimationDuration = 0.25f;
		protected static float NotificationDuration = 7.0f;

		/** Pixels between stacked notifications */
		protected static int NotificationGap = 15;

		/** The status of this notification */
		public NotificationStage Stage;

		protected GuiLabel text;
		protected GuiImage image;
		protected float pushDownCounter;
		protected float location;

		/** Creates a new notification with given text and optional graphic */
		public GuiNotification(string content, Sprite sprite = null) : base(260, 60)
		{
			Stage = NotificationStage.AnimateOn;
			stageLife = AnimationDuration;
			Color = new Color(0.2f, 0.2f, 0.2f);

			text = new GuiLabel(content);
			text.AutoHeight = false;
			text.AutoWidth = false;
			text.X = 5;
			text.Y = 5;
			text.Width = (int)ContentsBounds.width - 10;
			text.Height = (int)ContentsBounds.height - 10;
			text.FontSize = 12;
			text.WordWrap = true;
			text.TextAlign = TextAnchor.MiddleLeft;
			Add(text);

			OuterShadow = true;

			if (sprite != null) {
				image = new GuiImage(3, 3, sprite);
				image.Scale = (Height - 10) / sprite.rect.height;
				text.X = 60;
				text.Width = (int)ContentsBounds.width - text.X - 5;

				var frame = new GuiFillRect(1, 1, image.Width + 4, image.Height + 4, Color.black.Faded(0.50f));
				Add(frame);
				Add(image);
				var shadow = new GuiFrameRect(3, 3, image.Width, image.Height, Color.black.Faded(0.5f));
				Add(shadow);

			}

		}

		/** Causes the notification to be pushed down a slot, making room for another notification */
		public void PushDown()
		{
			pushDownCounter++;
		}

		/** Applies animation to notification to show and hide the notification. */
		public override void Update()
		{
			base.Update();
		
			int baseX = Screen.width - this.Width - 20;
			int baseY = 35 + (int)(location * (this.Height + NotificationGap));

			if (pushDownCounter > 0) {
				float amount = Math.Min(Time.deltaTime / AnimationDuration, pushDownCounter);
				pushDownCounter -= amount;
				location += amount;
			}

			switch (Stage) {
			case  NotificationStage.AnimateOn:
				X = baseX + (int)(stageLife / AnimationDuration * Width);
				Y = baseY;
				break;
			case  NotificationStage.Active:
				X = baseX;
				Y = baseY;
				break;
			case  NotificationStage.AnimateOff:
				X = baseX + (int)((1 - (stageLife / AnimationDuration)) * Width);
				Y = baseY;
				break;
			case  NotificationStage.Destroyed:
				Active = false;
				return;
			}

			stageLife -= Time.deltaTime;
			if (stageLife <= 0) {
				Stage++;
				stageLife = (Stage == NotificationStage.Active) ? NotificationDuration : AnimationDuration;
			}

		}

	}
}

