using System;
using UnityEngine;

namespace UI
{
	/** An editiable text box */
	public class GuiTextField : GuiLabeledComponent
	{
		/** The contents of this field */
		public string Value { get { return _value; } set { setValue(value); } }

		private string _value;

		/** If true control can be edited */
		public bool Editable;

		public GuiEvent OnValueChanged;

		public GuiTextField(int x, int y, int width = 200, int height = 30) : base(width, height)
		{
			Editable = true;
			TextAlign = TextAnchor.MiddleLeft;
		}

		private void setValue(string value)
		{
			if (!Enabled)
				return;
			if (_value == value)
				return;
			_value = value;
			if (OnValueChanged != null)
				OnValueChanged(this, null);
		}

		public override void DrawContents()
		{
			SmartUI.Color = Enabled ? Color : Color.gray;
			SmartUI.Enabled = this.Enabled;
			string newText = SmartUI.TextField(Frame, String.IsNullOrEmpty(Value) ? "" : Value, FontSize);
			SmartUI.Enabled = true;
			if (Editable && Enabled)
				Value = newText;
			SmartUI.Color = Color.white;
		}

	}
}

