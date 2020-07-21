using UnityEngine;

namespace UI.Generic
{
	/** Displays progress on a horizontal bar */
	public class GuiProgressBar : GuiComponent
	{
		/** The progress bar's current progress, from 0 to 1. */
		public float Progress;

		public Color ProgressColor;

		public GuiProgressBar(int width = 220, int height = 22) : base(width, height)
		{
			Style = Engine.GetStyleCopy("Solid");
			Style.padding = new RectOffset(1, 1, 1, 1);
			ProgressColor = new Color(0.6f, 0.6f, 0.9f, 0.8f);
		}

		public override void DrawContents()
		{
			base.DrawContents();
			DrawProgress();
		}

		/** Draws progress indicator */
		protected void DrawProgress()
		{
			SmartUI.DrawFillRect(new Rect(0, 0, (float)ContentsBounds.width * Util.Clamp(Progress, 0f, 1f), ContentsBounds.height), ProgressColor);
		}

	}

	/** Displays progress on a veritual bar */
	public class GuiVerticalProgressBar : GuiComponent
	{
		/** The progress bar's current progress, from 0 to 1. */
		public float Progress;

		public Color ProgressColor;

		public GuiVerticalProgressBar(int width = 22, int height = 220) : base(width, height)
		{
			ProgressColor = new Color(0.6f, 0.6f, 0.9f, 0.8f);
		}

		public override void DrawContents()
		{
			base.DrawContents();
			DrawProgress();
		}

		/** Draws progress indicator */
		protected void DrawProgress()
		{
			int drawHeight = (int)(ContentsBounds.height * Util.Clamp(Progress, 0f, 1f));
			SmartUI.DrawFillRect(new Rect(0, (int)ContentsBounds.height - drawHeight, ContentsBounds.width, (float)ContentsBounds.height * Util.Clamp(Progress, 0f, 1f)), ProgressColor);
		}

	}
}
