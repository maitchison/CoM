using UnityEngine;

namespace UI.Generic
{
	/** Slider to select from a range of values */
	public class GuiSlider : GuiLabeledComponent
	{
		private float _value = 0;

		public float Value {
			get { return _value; }
			set { setValue(value); }
		}

		public float Min = 0;
		public float Max = 100;

		/** Called whenever this controls value changes */
		public GuiEvent OnValueChanged;

		public GuiSlider(int width = 220, int height = 16) : base(width, height)
		{
			
		}

		private void setValue(float newValue)
		{
			if (_value == newValue)
				return;
			if (!Enabled)
				return;
			_value = newValue;
			if (OnValueChanged != null)
				OnValueChanged(this, null);
		}


		public override void DrawContents()
		{
			SmartUI.DrawLegacySlow(
				delegate {
					Value = GUI.HorizontalSlider(Frame, Value, Min, Max);
				}
			);
		}
	}
}
