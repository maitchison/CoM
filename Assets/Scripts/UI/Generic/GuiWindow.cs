
using System;

using UnityEngine;

namespace UI
{
	public enum GuiWindowStyle
	{
		/** A simple framed window */
		Normal,
		/** A simple framed window with a title */
		Titled,
		/** Dark background with gray frame */
		Dark,
		/** Semi transparent background with gray frame */
		Transparent,
		/** Semi transparent background with thin black */
		ThinTransparent,
		/** Similar to normal, but a little cleaner */
		Clean
	}

	/** Defines a windowed componenet with a title bar */
	public class GuiWindow : GuiContainer
	{
		public string Title;

		public GuiWindowStyle WindowStyle { get { return _style; } set { setStyle(value); } }

		/** The style to use when drawing the title bar */
		protected GUIStyle TitleStyle { get { return TitleLabel.Style; } }

		protected int TitleHeight = 20;

		protected GuiComponent TitleLabel;

		private GuiWindowStyle _style;

		/** Image to stretch over windows background */
		public GuiImage Background;

		public GuiWindow(int width = 400, int height = 400, string title = "")
			: base(width, height)
		{
			Title = title;
			WindowStyle = title == "" ? GuiWindowStyle.Normal : GuiWindowStyle.Titled;

			Background = new GuiImage(0, 0, ResourceManager.GetSprite("Gui/InnerWindow"));
			Background.Align = GuiAlignment.Full;
			Background.Color = Color.clear;
			Add(Background);

			TitleLabel = new GuiComponent() { Caption = Title };
			TitleLabel.Style = Engine.GetStyleCopy("Title");

			TitleStyle.wordWrap = false;
			TitleStyle.font = Engine.Instance.TitleFont;
			TitleStyle.fontSize = 16;
			TitleStyle.fontStyle = FontStyle.Bold;
			TitleStyle.alignment = TextAnchor.UpperCenter;
			TitleStyle.normal.textColor = Colors.FourNines;
		}

		/** Override to draw windows title (outside of the content area) */
		public override void Draw()
		{
			if (Title != "") {
				TitleLabel.Update();
				TitleLabel.Caption = Title;
				int offset = (Width - (TitleLabel.Width)) / 2;
				TitleLabel.Y = Y - 34;
				TitleLabel.X = X + offset;
				TitleLabel.Draw();
			}
			base.Draw();
		}

		/** Creates window using the given object as a source.  Window will be sized accordingly */
		public static GuiWindow CreateFrame(GuiComponent source, string title = "", GuiWindowStyle style = GuiWindowStyle.Normal)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			GuiWindow frame = new GuiWindow(0, 0, title);
			frame.WindowStyle = style;
			frame.SizeForContent(source.Width, source.Height);
			frame.Add(source);
			frame.X = source.X;
			frame.Y = source.Y;
			source.X = 0;
			source.Y = 0;
			return frame;
		}

		/** Updates the window style */
		private void setStyle(GuiWindowStyle value)
		{
			_style = value;
			// update window style 
			switch (WindowStyle) {
				case GuiWindowStyle.Normal:	
					Style = Engine.GetStyleCopy("Box");
					break;
				case GuiWindowStyle.Clean:					
					Style = Engine.GetStyleCopy("Frame");
					break;
				case GuiWindowStyle.Titled:	
					Style = Engine.GetStyleCopy("Box");
					break;
				case GuiWindowStyle.Dark:	
					Style = Engine.GetStyleCopy("BoxDark");
					break;
				case GuiWindowStyle.Transparent:	
					Style = Engine.GetStyleCopy("BoxTrans");
					break;
				case GuiWindowStyle.ThinTransparent:	
					Style = Engine.GetStyleCopy("BoxTrans");
					Style.border = new RectOffset(2, 2, 2, 2);
					Style.normal.background = StockTexture.CreateRectTexture(Color.white, Color.black.Faded(0.5f));
					break;
			}
		}
	}

}