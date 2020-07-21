
using UnityEngine;

namespace UI
{
	public enum GuiPanelMode
	{
		Round,
		Square,
		Inset,
		Custom

	}

	/** A blank panel which componenets can be added to */
	public class GuiPanel : GuiContainer
	{
		public GuiPanelMode PanelMode { get { return _mode; } set { setMode(value); } }

		/** Style to use when mode is set to custom */
		public string CustomStyleName { get { return _customStyleName; } set { setCustomStyleName(value); } }

		private string _customStyleName;

		private GuiPanelMode _mode;

		public GuiPanel(int width = 300, int height = 24) : base(width, height)
		{
			PanelMode = GuiPanelMode.Round;
		}

		/** Draws the panel */
		protected override void DrawBackground()
		{
			SmartUI.Color = Color;
			SmartUI.Draw(new Rect(X, Y, Width, Height), Style);
			SmartUI.Color = Color.white;	
		}

		private void setCustomStyleName(string value)
		{
			if (_customStyleName == value)
				return;
			_customStyleName = value;
			setMode(GuiPanelMode.Custom);
		}

		/** Sets the panel mode */
		private void setMode(GuiPanelMode value)
		{
			_mode = value;
			switch (value) {
			case GuiPanelMode.Round:
				Style = Engine.GetStyleCopy("Panel");
				break;
			case GuiPanelMode.Square:
				Style = Engine.GetStyleCopy("PanelSquare");
				break;
			case GuiPanelMode.Inset:
				Style = Engine.GetStyleCopy("PanelInset");
				break;
			case GuiPanelMode.Custom:
				Style = Engine.GetStyleCopy(CustomStyleName);
				break;
			}
		}
	}
}
