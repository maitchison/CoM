using Mordor;
using UnityEngine;
using Data;

namespace UI
{
	/** Displays a page of character information, such as stats, guild etc... */
	public class GuiCharacterPage : GuiTabPage
	{
		/** The fixed width of this component */
		public static int WIDTH = 300;

		/** The fixed height of this component */
		public static int HEIGHT = 360;

		/** The character to display */
		public MDRCharacter Character { get { return _character; } set { SetCharacter(value); } }

		private MDRCharacter _character;

		/** Creates a new message box */
		public GuiCharacterPage()
			: base(0, 0, WIDTH, HEIGHT)
		{
			var background = new GuiImage(0, 0, ResourceManager.GetSprite("Gui/InnerWindow"));
			background.Align = GuiAlignment.Full;
			background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			background.Color = Colors.BackgroundBlue;
			Add(background);
		}

		virtual protected void SetCharacter(MDRCharacter value)
		{
			if (_character == value)
				return;
			_character = value;
			Sync();
		}

		protected string formatPercent(float value)
		{
			var color = Color.gray;
			if (value > 0)
				color = Colors.GeneralPercentPostiveColor;
			if (value < 0)
				color = Colors.GeneralPercentNegitiveColor;
			return Util.Colorise(value.ToString("0.0") + "%", color);
		}

		protected string formatValue(int value)
		{
			return Util.Colorise(value.ToString("0"), Colors.GeneralHilightValueColor);
		}


		protected string formatGuild(MDRGuild guild)
		{
			if (guild == null)
				return "";
			return Util.Colorise("<Size=10>[" + guild.Name + "]</size>", Color.gray);
		}

		/** Called when components need to be refreshed. */
		virtual public void Sync()
		{
		}

	}
}
