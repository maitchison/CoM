using UnityEngine;

namespace UI
{
	/** This is the old gui image button, it uses a custom image to draw and therefore supports framing, which is why I've needed to keep it */
	public class GuiSimpleImageButton : GuiButton
	{
		public bool ClickOverlay = false;

		public GuiSimpleImageButton(Sprite image)
			: base("")
		{
			EnableBackground = false;
			Image = new GuiImage(0, 0, image);
			Image.Framed = true;

			Style = Engine.GetStyleCopy("Button");
			Style.padding = new RectOffset(0, 0, 0, 0);
			DepressedOffset = 1;

			this.Width = (int)image.rect.width;
			this.Height = (int)image.rect.height;
		}

		public override void DrawContents()
		{
			base.DrawContents();

			Image.Width = (int)ContentsBounds.width;
			Image.Height = (int)ContentsBounds.height;
			Image.Update();
			Image.Draw();

			RectOffset border = new RectOffset(2, 2, 2, 2);

			if (!Enabled)
				SmartUI.DrawFillRect(border.Remove(Frame), new Color(0.4f, 0.4f, 0.4f, 0.66f));
		}


	}
}
