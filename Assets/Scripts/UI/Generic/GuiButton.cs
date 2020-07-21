using UnityEngine;

namespace UI
{
	/** A simple styled button */
	public class GuiButton : GuiLabeledComponent
	{
		/** Image to overlay over button. */
		public GuiImage Image;

		public GuiButton(string caption, int width = 120, int height = -1)
			: base(width, height)
		{
			Style = Engine.GetStyleCopy("Button");
			this.Caption = caption;
			this.Name = caption;
			DepressedOffset = 1;
			CanReceiveFocus = true;
		}

		public override void DrawContents()
		{
			base.DrawContents();
			if (Image != null) {

				var origonalTransform = Image.ColorTransform;

				ColorTransform effectTranform = ColorTransform.Identity;

				if (IsMouseOver)
					effectTranform = ColorTransform.Multiply(new Color(1.3f, 1.3f, 1.2f));
				if (Depressed)
					effectTranform = ColorTransform.Multiply(new Color(0.8f, 0.8f, 0.8f));
				if (!Enabled)
					effectTranform = ColorTransform.BlackAndWhite;

				if (!effectTranform.IsIdentity)
					Image.ColorTransform = Image.ColorTransform + effectTranform;

				Image.Update();
				Image.Draw();

				Image.ColorTransform = origonalTransform;
			}		
		}
	}
}
