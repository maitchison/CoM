
using UnityEngine;


using UI;

namespace UI
{
	/** An on/off toggle button */
	public class GuiToggleButton : GuiLabeledComponent
	{
		public bool Value { get { return _value; } set { setValue(value); } }

		private bool _value;

		/** Called whenever this controls value changes */
		public GuiEvent OnValueChanged;

		public GuiToggleButton(int height = 24, int width = 23) : base(width, height)
		{
			Value = false;
		}

		private void setValue(bool newValue)
		{
			if (_value == newValue)
				return;
			if (!Enabled)
				return;
			_value = newValue;
			if (OnValueChanged != null)
				OnValueChanged(this, null);
		}

		protected override void DrawBackground()
		{
			SmartUI.Color = Enabled ? Color : Color.gray;
			Value = SmartUI.Toggle(Bounds, Value, "");
			SmartUI.Color = Color.white;
		}
	}

}
