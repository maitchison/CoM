/**
 */

using UnityEngine;
using System.Reflection;

using UI;
using UI.Generic;
using System;

namespace UI.State.Menu
{
	/** Dispalys game options that can be changed */
	public class SettingsMenuState : GuiState
	{
		const int WINDOW_WIDTH = 600;
		const int WINDOW_HEIGHT = 600;

		private GuiWindow MainWindow;

		private GuiComponent generalGroup;
		private GuiComponent advancedGroup;
		private GuiComponent informationGroup;

		private float previousSoundFXLevel = 0;

		public SettingsMenuState() : base("Settings Menu")
		{
			TransparentDraw = true;

			MainWindow = new GuiWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Settings");
			Add(MainWindow, 0, 0);

			var buttonsGroup = new GuiRadioButtonGroup();
			buttonsGroup.AddItem("General");
			if (Settings.Advanced.PowerMode)
				buttonsGroup.AddItem("Advanced");
			if (Settings.Advanced.PowerMode)
				buttonsGroup.AddItem("Information");
			buttonsGroup.OnValueChanged += delegate {
				switch (buttonsGroup.SelectedIndex) {
				case 0:
					setSettingsGroup(generalGroup);
					break;
				case 1:
					setSettingsGroup(advancedGroup);
					break;
				case 2:
					setSettingsGroup(informationGroup);
					break;
				}
			};
			buttonsGroup.EnableBackground = false;
			buttonsGroup.Align = GuiAlignment.Top;
			MainWindow.Add(buttonsGroup);
					
			var closeButton = new GuiButton("Close");
			MainWindow.Add(closeButton, -10, -10);

			var resetButton = new GuiButton("Reset");
			MainWindow.Add(resetButton, 10, -10);

			CreateSettingsControls();

			previousSoundFXLevel = Settings.General.SoundFXVolume;

			closeButton.OnMouseClicked += delegate {
				Engine.PopState();
			};
			resetButton.OnMouseClicked += delegate {
				Engine.ConfirmAction(Settings.ResetSettings, "Are you sure you want to reset the settings?");
			};	

			Settings.Advanced.OnValueChanged += UpdateControls;
			Settings.General.OnValueChanged += UpdateControls;

			setSettingsGroup(generalGroup);
		}

		/** Updates controls to reflect change in given controlname */
		private void UpdateControls(string controlName, string value)
		{
			var control = MainWindow.FindControl(controlName, true);
			if (control != null) {
				if (control is GuiToggleButton)
					(control as GuiToggleButton).Value = bool.Parse(value);
				if (control is GuiTextField)
					(control as GuiTextField).Value = value;
			}
		}

		private void CreateSettingsControls()
		{
			generalGroup = MainWindow.Add(CreateUIFromSettingsGroup(Settings.General), 0, 0);
			advancedGroup = MainWindow.Add(CreateUIFromSettingsGroup(Settings.Advanced), 0, 0);
			informationGroup = MainWindow.Add(CreateUIFromSettingsGroup(Settings.Information), 0, 0);
		}

		/** 
		 * Changes the currently displayed settings group 
		 */
		private void setSettingsGroup(GuiComponent group)
		{
			generalGroup.Visible = false;
			advancedGroup.Visible = false;
			informationGroup.Visible = false;

			if (group == null) {
				return;
			}

			group.Visible = true;

		}

		/** Detect sound change and chime. */
		public override void Update()
		{
			base.Update();

			if (Input.GetMouseButtonUp(0) && previousSoundFXLevel != Settings.General.SoundFXVolume) {
				previousSoundFXLevel = Settings.General.SoundFXVolume;
				SoundManager.Play("OLDFX_SFXTEST", 1f, 0f, 0);
			}	
		}

		/** Sets a boolean property to a value via a string */
		private void setIntProperty(object source, PropertyInfo property, string value)
		{
			property.SetValue(source, value, null);
		}

		/** 
		 * Creates a control for given property.
		 * 
		 * ToggleButtons will be used for boolean properties.
		 * For the moment other types of properties are not supported.
		 * 
		 * @param source The source object this property belongs to.
		 * @param property The property to create a control for.
		 */
		private GuiLabeledComponent CreateControl(object source, PropertyInfo property)
		{
			GuiLabeledComponent result = null;

			var rangeAttribute = getAttribute<SettingRange>(property);

			if (property.PropertyType == typeof(System.Boolean)) {
				
				var toggleButton = new GuiToggleButton();
				toggleButton.Value = (bool)property.GetValue(source, null);
				if (property.CanWrite)
					toggleButton.OnValueChanged += delegate {
						property.SetValue(source, toggleButton.Value, null);
					};
				result = toggleButton;

			} else if (property.PropertyType.IsEnum) {

				var radioGroup = new GuiRadioButtonGroup();

				radioGroup.EnableBackground = false;
				radioGroup.ButtonSpacing = 2;
				radioGroup.ButtonSize = new Vector2(80, 32);

				var enumNames = Enum.GetNames(property.PropertyType);
				foreach (string enumValueName in enumNames) {
					if (!enumValueName.StartsWith("_"))
						radioGroup.AddItem(enumValueName);
				}

				radioGroup.SelectedIndex = (int)property.GetValue(source, null);

				if (property.CanWrite) {
					radioGroup.OnValueChanged += delegate {
						property.SetValue(source, radioGroup.SelectedIndex, null);
					};
				}

				result = radioGroup;

			} else if (property.PropertyType == typeof(System.Single) && (rangeAttribute != null)) {
				
				var slider = new GuiSlider();
				slider.Min = rangeAttribute.Min;
				slider.Max = rangeAttribute.Max;
				slider.Value = (float)property.GetValue(source, null);
				if (property.CanWrite)
					slider.OnValueChanged += delegate {
						property.SetValue(source, slider.Value, null);
					};
				result = slider;

			} else if (
				// fallback to edit box
				(property.PropertyType == typeof(int)) ||
				(property.PropertyType == typeof(float)) ||
				(property.PropertyType == typeof(string))) {
				int editWidth = (property.PropertyType == typeof(string)) ? 325 : 60;

				var editBox = new GuiTextField(0, 0, editWidth);

				object propertyValue = property.GetValue(source, null);
				if (propertyValue != null) {
					editBox.Value = propertyValue.ToString();
					if (editBox.Value.Length > 35)
						editBox.FontSize = 12;
					if (editBox.Value.Length > 55)
						editBox.FontSize = 10;
					if (editBox.Value.Length > 75)
						editBox.FontSize = 8;
				}
		
				if (property.CanWrite)
					editBox.OnValueChanged += delegate { 
						if (property.PropertyType == typeof(int))
							property.SetValue(source, Util.ParseIntDefault(editBox.Value, 0), null);
						else if (property.PropertyType == typeof(float))
							property.SetValue(source, Util.ParseFloatDefault(editBox.Value, 0), null);
						else if (property.PropertyType == typeof(string))
							property.SetValue(source, editBox.Value, null);
					};
		
				result = editBox;
			}			

			if (result != null) {
				if (!property.CanWrite)
					result.SelfEnabled = false;
				result.LabelText = property.Name;
				result.LabelPosition = LabelPosition.Left;
				result.Name = property.Name;
			}


			return result;
		}

		/** returns the first attribute of given type assocated with given property. */
		private T getAttribute<T>(PropertyInfo property, T _default = null) where T : Attribute
		{
			object[] attributes = property.GetCustomAttributes(typeof(T), false);
			T attribute = (attributes.Length >= 1) ? (T)attributes[0] : _default;
			return attribute;
		}

		/** 
		 * Creates ui elements to configure each settings in given group.
		 * 
		 * Returns a GuiComponent containing the controls.
		 */
		private GuiComponent CreateUIFromSettingsGroup(SettingsGroup group)
		{
			GuiContainer result = new GuiScrollableArea(WINDOW_WIDTH - 50, WINDOW_HEIGHT - 150) { Y = 50 };
			result.Name = group.Name;

			int atY = 10;

			var binding = (BindingFlags.Public |
			              BindingFlags.NonPublic |
			              BindingFlags.Instance |
			              BindingFlags.DeclaredOnly);

			foreach (var property in group.GetType().GetProperties(binding)) {

				var attribute = getAttribute<SettingAttribute>(property, SettingAttribute.Default);
				var divider = getAttribute<SettingDivider>(property);

				// filter out some properties
				if (property.Name == "Item")
					continue;
				if (!property.CanRead)
					continue;
				if (attribute.Ignore)
					continue;

				GuiLabeledComponent control = CreateControl(group, property);
				if (control == null) {
					Trace.Log("No suitable control found for property " + property.Name + " of type " + property.PropertyType);
					continue;
				}

				if (group.isDisabled(property.Name)) {
					if (control is GuiToggleButton)
						(control as GuiToggleButton).Value = false;
					control.SelfEnabled = false;
				}					

				if (divider != null) {
					string dividerText = divider.Name;
					var dividerControl = new GuiLabel(dividerText);
					dividerControl.TextAlign = TextAnchor.MiddleLeft;
					dividerControl.FontColor = new Color(0.75f, 0.75f, 0.75f);
					result.Add(dividerControl, 20, atY + 3);
					atY += (dividerText == "") ? 5 : 30;
				}

				// apply attributes
				if (control.Enabled)
					control.LabelColor = attribute.Color;
				else
					control.LabelColor = Color.Lerp(attribute.Color, Color.gray, 0.75f);

				control.LabelText = attribute.DisplayName ?? property.Name;

				result.Add(control, 200, atY);

				int spacing = Util.ClampInt(control.Height + 10, 30, 999);
				atY += spacing;
			}

			result.FitToChildren();

			result = GuiWindow.CreateFrame(result); 
			(result as GuiWindow).WindowStyle = GuiWindowStyle.Transparent;
			result.Color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
			result.FitToChildren();
			result.Width = WINDOW_WIDTH - 40;

			return result;
		}
	}
}
