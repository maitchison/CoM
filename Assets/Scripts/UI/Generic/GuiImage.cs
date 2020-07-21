
using UnityEngine;

namespace UI
{
	/** Displays a sprite as a gui component */
	public class GuiImage : GuiComponent
	{
		/** The sprite to draw */
		public Sprite Sprite { get { return _sprite; } set { setSprite(value); } }

		/** How much to scale the sprite */
		public float Scale { get { return _scale; } set { setScale(value); } }

		public Color ImageColor;
		public ColorTransform ImageColorTransform;

		public GUIStyle FrameStyle;

		/** How much to rotate the image (in degrees) */
		public float Rotation;

		public bool AlphaBlend;

		/** If true sprite will be drawn with a background and border*/
		public bool Framed { get { return _framed; } set { setFramed(value); } }

		private bool _framed;
		private Sprite _sprite;
		private float _scale;

		private const int frameWidth = 4;

		public GuiImage(int x = 0, int y = 0, Sprite sprite = null, float scale = 1.0f)
			: base(32, 32)
		{

			Interactive = false;

			X = x;
			Y = y;
			this.FrameStyle = Engine.GetStyleCopy("Slot");
			this.Scale = scale;
			this.AlphaBlend = true;
			this.Color = Color.white;
			this.ImageColor = Color.white;
			this.ImageColorTransform = ColorTransform.Identity;

			EnableBackground = false;
			Framed = false;

			this.Sprite = sprite;

		}

		/** Adjusts scale of image so that it fits given rectangle, without distortion */
		public void BestFit(Rect rect, bool maximum = true)
		{
			updateSize();
			float xscale = rect.width / Width;
			float yscale = rect.height / Height;
			Scale = maximum ? Mathf.Max(xscale, yscale) : Mathf.Min(xscale, yscale);
		}

		private void setScale(float value)
		{
			_scale = value;
			updateSize();
		}

		/** Sets the current sprite, updating the size */
		private void setSprite(Sprite sprite)
		{
			_sprite = sprite;
			updateSize();
		}

		private void updateSize()
		{
			if ((Sprite != null) && (Align != GuiAlignment.Full)) {
				Width = (int)(Sprite.rect.width * Scale) + (Framed ? 2 * frameWidth : 0);
				Height = (int)(Sprite.rect.height * Scale) + (Framed ? 2 * frameWidth : 0);
			}
		}

		/** enables or disables the border for the sprite, also updates the objects size */
		private void setFramed(bool value)
		{
			_framed = value;
			Style = Framed ? FrameStyle : GUIStyle.none;
			Style.padding = (value ? new RectOffset(frameWidth, frameWidth, frameWidth, frameWidth) : new RectOffset(0, 0, 0, 0));
			setSprite(Sprite);
			EnableBackground = value;
		}

		/** Draws the sprite */
		public override void DrawContents()
		{
			if (Sprite != null) {
				DrawParameters dp = GetDrawParameters;
				dp.AlphaBlend = AlphaBlend;
				dp.Scale = new Vector2(ContentsFrame.width / Sprite.rect.width, ContentsFrame.height / Sprite.rect.height);
				dp.Rotation = Rotation;

				if (!ImageColorTransform.IsIdentity)
					dp.Transform = dp.Transform + ImageColorTransform;

				SmartUI.Color = Color * ImageColor;
				SmartUI.Draw(0, 0, Sprite, dp);
				SmartUI.Color = Color.white;
			}
		}
	}
}
