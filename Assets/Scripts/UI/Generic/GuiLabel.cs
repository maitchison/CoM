using UnityEngine;

namespace UI
{
	/** Label that shows a solid background */
	public class GuiSolidLabel : GuiLabel
	{
		public GuiSolidLabel(int x, int y, string caption, int width = -1, int height = -1)
			: base(x, y, caption, width, height)
		{
			EnableBackground = true;
			Style.padding = new RectOffset(2, 2, 2, 2);
		}
	}

	public class GuiLabel : GuiComponent
	{
		/** If enabled draws a dropshadow behind text */
		public bool DropShadow = false;

		/** Enables a really hacked together edging around the font, 4x slower than dropshadow, and might not look right when faded out. */
		public bool FauxEdge = false;

		public int ShadowDistance = 1;

		private string _colorisedCaption = null;
		private Color colorisedCaptionColor;

		/** Our caption with color codes adjusted to font color. */
		protected string colorisedCaption {
			get {
				if (_colorisedCaption == null || FontColor != colorisedCaptionColor) {
					_colorisedCaption = Util.AdjustColorCodes(NativeSizedCaption, FontColor);
					_shadowCaption = null; // alpha might have changed.
					colorisedCaptionColor = FontColor;
				}
				return _colorisedCaption;
			}
		}

		public GuiLabel(int x, int y, string caption, int width = -1, int height = -1)
			: base(width, height)
		{
			X = x; 
			Y = y;
			Style = Engine.GetStyleCopy("Label");
			Style.font = CoM.Instance.TextFont;
			TextAlign = TextAnchor.UpperLeft;
			EnableBackground = false;
			IgnoreClipping = true; //stub: might help with performance
			Interactive = false;

			WordWrap = false;
			Caption = caption;

			FontColor = new Color(0.9f, 0.9f, 0.9f);
		}

		public GuiLabel(string caption, int width = -1, int height = -1)
			: this(0, 0, caption, width, height)
		{
			Interactive = false;
		}

		override protected void setCaption(string value)
		{
			base.setCaption(value);
			_colorisedCaption = null;
		}

		protected override void DrawBackground()
		{
			// do nothing.
		}

		/**
		 * Draw label with potential dropshadow 
		 */
		public override void Draw()
		{	
			SmartUI.Color = Color;

			if (EnableBackground)
				SmartUI.Draw(Bounds, Style);

			SmartUI.Color = Color.white;

			if (FauxEdge) {
				// SmartUI has quite slow shadowing as it has to go in an modify all the color codes.  So I use my own cached one here. 
				SmartUI.Text(ContentsBounds.Translated(new Vector2(ShadowDistance, ShadowDistance)), shadowCaption, Style);
				SmartUI.Text(ContentsBounds.Translated(new Vector2(-ShadowDistance, ShadowDistance)), shadowCaption, Style);
				SmartUI.Text(ContentsBounds.Translated(new Vector2(-ShadowDistance, -ShadowDistance)), shadowCaption, Style);
				SmartUI.Text(ContentsBounds.Translated(new Vector2(ShadowDistance, -ShadowDistance)), shadowCaption, Style);
				SmartUI.Text(ContentsBounds, colorisedCaption, Style);
			} else if (DropShadow) {
				// SmartUI has quite slow shadowing as it has to go in an modify all the color codes.  So I use my own cached one here. 
				SmartUI.Text(ContentsBounds.Translated(new Vector2(ShadowDistance, ShadowDistance)), shadowCaption, Style);
				SmartUI.Text(ContentsBounds, colorisedCaption, Style);
			} else
				SmartUI.Text(ContentsBounds, colorisedCaption, Style);
	
			SmartUI.Color = Color.white;
		}

	}
}