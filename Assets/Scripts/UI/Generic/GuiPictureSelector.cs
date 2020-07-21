namespace UI
{

	/** Displays an image with buttons to select from a list of images */
	public class GuiPictureSeletor : GuiWindow
	{

		public int SelectedIndex = 0;

		/** If true pictures will cycle when the end, or begining is reached */
		public bool Cycle = true;

		/** If true control will be sized to fit the first pictures dimentions when Pictures is set */
		public bool AutoSize = true;

		private GuiButton backButton;
		private GuiButton forwardButton;

		/** List of pictures to cycle through */
		public IDrawableSprite[] Pictures { 
			get { return _pictures; }
			set { setPictures(value); } 
		}

		private IDrawableSprite[] _pictures;

		public GuiPictureSeletor(int width = 200, int height = 200)
			: base(width, height)
		{
			IgnoreClipping = true;

			backButton = new GuiButton("<", 24);
			forwardButton = new GuiButton(">", 24);
			backButton.Align = GuiAlignment.Left;
			forwardButton.Align = GuiAlignment.Right;
			Add(backButton);
			Add(forwardButton);

			backButton.OnMouseClicked += delegate {
				if (Pictures == null)
					return;
				SelectedIndex--;
				if (SelectedIndex < 0)
					SelectedIndex = Cycle ? Pictures.Length - 1 : 0;
			};

			forwardButton.OnMouseClicked += delegate {
				if (Pictures == null)
					return;
				SelectedIndex++;
				if (SelectedIndex >= Pictures.Length)
					SelectedIndex = Cycle ? 0 : Pictures.Length - 1;
			};

		}

		public IDrawableSprite Selected {
			get { return Pictures[SelectedIndex]; }
		}

		public override void Draw()
		{
			base.Draw();
		}

		/** Draw the selected image */
		public override void DrawContents()
		{
			if (Pictures != null) {
				SmartUI.Draw((this.ContentsBounds.width - Pictures[SelectedIndex].Sprite.rect.width) / 2, 0, Pictures[SelectedIndex].Sprite);
			}
			base.DrawContents();
		}

		/** Sets the list of pictures to display and auto sizes if enabled */
		private void setPictures(IDrawableSprite[] value)
		{
			_pictures = value;
			SelectedIndex = 0;
			if ((value != null) && AutoSize) {
				Width = Style.padding.horizontal + (2 * backButton.Width) + (int)value[0].Sprite.rect.width;
				Height = Style.padding.vertical + (int)value[0].Sprite.rect.height;
			}
		}
	}
}