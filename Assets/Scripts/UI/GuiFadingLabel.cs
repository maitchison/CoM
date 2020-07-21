using System;
using UnityEngine;

namespace UI
{
	/** A label that fades out over time. Setting the label will cause it to be visibile again. */
	public class GuiFadingLabel : GuiLabel
	{
		/** Time it takes label to start to fade out. */
		public float LifeSpan = 4f;

		/** Time it takes label to fade out after life expires. */
		public float FadeOutTime = 1f;

		/** Time it takes label to fade in when caption is changed. */
		public float FadeInTime = 0.2f;

		/** Time since our caption was set. */
		private float age = 0f;

		private float fadeAlpha = 0f;

		public GuiFadingLabel(string caption = "") : base(caption)
		{
			age = 99;
		}

		public override void Update()
		{
			base.Update();
			age += Time.deltaTime;

			if (age > LifeSpan + FadeOutTime)
				fadeAlpha = 0f;
			else if (age > LifeSpan)
				fadeAlpha = 1f - ((age - LifeSpan) / FadeOutTime);
			else if (age < 0)
				fadeAlpha = 1f - (-age / FadeInTime);
			else
				fadeAlpha = 1f;
		}

		public override void Draw()
		{
			if (fadeAlpha == 0f)
				return;
			if (fadeAlpha <= 1f) {
				var oldFontColor = FontColor;
				var oldColor = Color;
				FontColor = oldFontColor.Faded(fadeAlpha);
				Color = oldColor.Faded(fadeAlpha);
				base.Draw();
				FontColor = oldFontColor;
				Color = oldColor;
			} else
				base.Draw();
		}

		protected override void setCaption(string value)
		{			
			if (Caption != value && value != "")
				age = -FadeInTime;
			
			base.setCaption(value);
		}

	}
}

