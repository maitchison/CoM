
using UnityEngine;
using Data;
using System;
using UI.Generic;

namespace UI
{

	/** Displays a tool tip describing given item. */
	public abstract class GuiToolTip<T> : GuiToolTipBase where T : class
	{
		/** The Monster to display information about */
		public T Data  { get { return _data; } set { setData(value); } }

		private T _data;

		public float Life;

		/** Hides tooltip after "life" seconds. */
		public bool AutoHide = true;

		/** Sets the Monster, updates the panel's size */
		protected void setData(T newData)
		{
			Visible = (newData != null);
			Life = 0.01f;
			if (_data != newData) {
				_data = newData;
				UpdateToolTip();
			}
		}

		abstract public void UpdateToolTip();

		/** Creates a new tool tip and attaches it to given gamestate. */
		public GuiToolTip()
			: base(400, 150)
		{
			IconSprite.Scale = 1.0f;
			IconSprite.X = 17;
			IconSprite.Y = 12;
			IconShadow.Visible = false;
		}

		public override void Update()
		{
			base.Update();

			if (AutoHide) {
				if (Life <= 0)
					Visible = false;
				Life -= Time.deltaTime;
			}

		}
	}

	/** Used for controls that temporarly display information on the screen (like a tooltip) */
	public class GuiToolTipBase : GuiWindow
	{
		protected GuiLabel InfoLabel;
		protected GuiLabel HeaderLabel;
		protected GuiImage IconBackground;
		protected GuiImage IconSprite;
		protected GuiImage IconShadow;

		protected string Header { set { setHeader(value); } }

		protected string Info { set { InfoLabel.Caption = value; } }

		protected Sprite Sprite { set { IconSprite.Sprite = value; } }

		/** If true tool tip will be auto sized from the contents of the header. */
		public bool SizeFromHeader = true;

		public GuiToolTipBase(int width = 300, int height = 150)
			: base(width, height)
		{
			Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
			Style.normal.textColor = Color.white;
			Style.wordWrap = true;
			Visible = false;

			WindowStyle = GuiWindowStyle.Transparent;
			Color = new Color(0.3f, 0.3f, 0.3f);

			HeaderLabel = new GuiLabel(90, 5, "", 220, 60);
			HeaderLabel.TextAlign = TextAnchor.MiddleLeft;
			HeaderLabel.WordWrap = true;
			Add(HeaderLabel);

			InfoLabel = new GuiLabel(10, 90, "");
			InfoLabel.FontSize = 12;
			InfoLabel.WordWrap = true;
			InfoLabel.AutoHeight = true;
			Add(InfoLabel);

			IconBackground = new GuiImage(10, 5, CoM.Instance.IconSprites["Slot_Large"]);
			Add(IconBackground);

			IconShadow = new GuiImage(10 + 21 + 4, 5 + 21 + 1, null);
			IconShadow.AlphaBlend = true;
			IconShadow.Color = new Color(0f, 0f, 0f, 0.50f);
			Add(IconShadow);
	
			IconSprite = new GuiImage(10 + 21, 5 + 21, null);
			IconSprite.AlphaBlend = true;
			Add(IconSprite);
		}

		/** Sets the position of the tool tip based on the mouse location, making sure to not let the tool tip go off the 
		 * screen. */
		public void PositionToMouse()
		{
			int xPos = (int)Mouse.Position.x - Width - 10;
			int yPos = (int)Mouse.Position.y - Height - 10;

			xPos = Util.ClampInt(xPos, 10, (int)(Screen.width / Engine.GuiScale) - Width - 10);
			yPos = Util.ClampInt(yPos, 10, (int)(Screen.height / Engine.GuiScale) - Height - 10);

			X = xPos;
			Y = yPos;
		}

		/** Formats and sets the contents of the header. */
		protected virtual void setHeader(string value)
		{
			HeaderLabel.Caption = FormatHeader(value); 
			if (SizeFromHeader) {
				Width = (int)HeaderLabel.Frame.xMax + 20 + Style.padding.horizontal;			
				InfoLabel.Width = Width - InfoLabel.X - 10;
			}
		}

		public override void Update()
		{
			base.Update();

			IconShadow.Sprite = IconSprite.Sprite;
		}

		/** markup text as header */
		protected string FormatHeader(string text)
		{
			return "<size=20><color=#d4d4d4>" + text + "</color></size>";
		}

		/** markup text as hilighted */
		protected string FormatHilight(string text)
		{
			return "<size=12><color=#70b8ff>" + text + "</color></size>";
		}

		/** markup text as normal */
		protected string FormatNormal(string text)
		{
			return "<size=12><color=#d4d4d4>" + text + "</color></size>";
		}

		protected string FormatNormal(int value)
		{
			return FormatNormal(value.ToString());
		}

		protected string FormatHilight(int value)
		{
			return FormatHilight(value.ToString());
		}


	}


}
