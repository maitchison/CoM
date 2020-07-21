using System;
using UnityEngine;
using Mordor;

namespace UI
{
	/** Displays an image that fades out. */
	public class GuiFadingImage : GuiContainer
	{
		/** Sets the damage value. */
		public int Value {
			get { return _value; }
			set {				
				_value = value;
				Label.Caption = "<B>" + _value.ToString() + "</B>";
				//Label.Visible = (_value != 0);
			}
		}

		private int _value = 0;

		/** The fading image to draw underneath the value. */
		public GuiImage Image;

		/** Displays a label above the image. */
		public GuiLabel Label;

		/** How long this sprite should last. */
		public float Life;

		/** The age in seconds of the sprite. */
		private float age;

		public float ShowDelay { set { age = -value; } }

		public float Scale {
			get { return Image.Scale; }
			set {
				Image.Scale = value; 
				Width = Image.Width;
				Height = Image.Height;
				Label.Width = Width;
				Label.Height = Height;
			}
		}

		/** Adds a new fading ijmage. 
		 * @param value The value to show
		 * @param killTime Number of seconds after which to remove the splat 
		 * @param showDelta time in second to wait before showing splat
		 */ 
		public GuiFadingImage(Sprite sprite, int value = 0, float life = 2f, float showDelay = 0.0f)
			: base(30, 30)
		{			
			this.age = -showDelay;
			this.Life = life;

			Image = new GuiImage(0, 0, sprite);

			Label = new GuiLabel("", Width, Height);
			Label.Font = CoM.Instance.TitleFont;
			Label.TextAlign = TextAnchor.MiddleCenter;
			Label.FontSize = 14;		
			Label.FauxEdge = true;
			Label.FontColor = Color.white;

			Scale = 1f;

			IgnoreClipping = true;

			Value = value;
		}

		public override void Update()
		{			
			base.Update();
			updateFadeout();
			Image.Update();
			Label.Update();
			age += Time.deltaTime;
			Visible = (age >= 0);
		}

		public override void DrawContents()
		{
			base.DrawContents();
			Image.Draw();
			if (Label.Visible)
				Label.Draw();
		}

		/** Makes the splat larger over time and fades it out. */
		private void updateFadeout()
		{		
			if (age < 0)
				return;

			if (age >= Life) {
				Remove();
				return;
			}

			float factor = 1f - (age / Life);
			CompositeAlpha = Util.Clamp(factor * 2f, 0f, 1f);

			Visible = true;
		}
	}
}