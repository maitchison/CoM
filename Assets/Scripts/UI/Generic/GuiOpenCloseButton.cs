using System;

namespace UI
{
	enum OpenCloseButtonMode
	{
		Open,
		Close
	}

	/** Toggels visibility on another component */
	public class GuiOpenCloseButton : GuiButton
	{
		public GuiComponent Target;
		private OpenCloseButtonMode Mode;

		/** Adds an open / close button to target and adds it to targets parent. */
		public static GuiOpenCloseButton Create(GuiComponent target)
		{
			var button = new GuiOpenCloseButton(target);
			button.X = (int)target.Bounds.xMax + 5 - button.Width;
			button.Y = (int)target.Bounds.yMin - 5;
			target.Parent.Add(button);
			return button;
		}

		public GuiOpenCloseButton(GuiComponent target)
			: base("", 35, 35)
		{			
			Style = Engine.GetStyleCopy("SquareButton");
			Target = target;
			Image = new GuiImage(0, 0);
			setMode(OpenCloseButtonMode.Close);
			OnMouseClicked += delegate {
				SwitchMode();
			};
		}

		public override void Update()
		{
			base.Update();
			if (Target != null) {
				Target.Visible = (Mode == OpenCloseButtonMode.Close);
			}
		}

		/** Switches buttons mode from open to close, or close to open. */
		public void SwitchMode()
		{
			if (Mode == OpenCloseButtonMode.Close)
				setMode(OpenCloseButtonMode.Open);
			else
				setMode(OpenCloseButtonMode.Close);
		}

		private void setMode(OpenCloseButtonMode mode)
		{
			switch (mode) {
				case OpenCloseButtonMode.Close:
					Image.Sprite = ResourceManager.GetSprite("Icons/Close");
					Image.X = (int)(ContentsFrame.width - Image.Width) / 2;
					Image.Y = (int)(ContentsFrame.height - Image.Height) / 2;
					break;
				case OpenCloseButtonMode.Open:
					Image.Sprite = ResourceManager.GetSprite("Icons/Open");
					Image.X = (int)(ContentsFrame.width - Image.Width) / 2;
					Image.Y = (int)(ContentsFrame.height - Image.Height) / 2;
					break;
			}
			Mode = mode;
		}
	}
}

