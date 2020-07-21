using UnityEngine;

namespace UI
{
	/** The position of the label relative to the component */
	public enum LabelPosition
	{
		Left,
		Right,
		Top,
		Bottom
	}

	/** A component with an optional label */
	public class GuiLabeledComponent : GuiComponent
	{
		/** The location to display the label */
		public LabelPosition LabelPosition { get; set; }

		/** The label to display for this component */
		public string LabelText { get { return labelControl.Caption; } set { setLabel(value); } }

		public Color LabelColor { get { return labelControl.FontColor; } set { labelControl.FontColor = value; } }

		public TextAnchor LabelAlign { get { return labelControl.TextAlign; } set { labelControl.TextAlign = value; } }

		protected GuiLabel labelControl;

		public GuiLabeledComponent(int width = 100, int height = 50)
			: base(width, height)
		{
			labelControl = new GuiLabel(0, 0, "");
			LabelColor = Color.white;
		}

		/** Sets the text of this fields label */
		private void setLabel(string value)
		{
			labelControl.Caption = value;
		}

		public override void Draw()
		{
			base.Draw();
			if (LabelText != "")
				labelControl.Draw();
		}

		public override void Update()
		{
			base.Update();

			switch (LabelPosition) {
				case LabelPosition.Left:
					labelControl.X = this.X - 2 - labelControl.Width;
					labelControl.Y = this.Y + (this.Height - labelControl.Height) / 2;
					break;
				case LabelPosition.Right:
					labelControl.X = this.X + 4 + (this.Width);
					labelControl.Y = this.Y + (this.Height - labelControl.Height) / 2;
					break;
				case LabelPosition.Top:
					labelControl.X = this.X + 2;
					labelControl.Y = this.Y - 2 - labelControl.Height;
					break;
				case LabelPosition.Bottom:
					labelControl.X = this.X + 2;
					labelControl.Y = this.Y + 2 + (this.Height);
					break;
			}

			labelControl.Update();

		}

	}
}
	