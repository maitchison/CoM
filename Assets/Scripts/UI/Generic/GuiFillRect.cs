
using UnityEngine;

namespace UI.Generic
{
	/** Simple component to display a solid colored rectangle. */
	public class GuiFillRect : GuiComponent
	{
		public GuiFillRect(int x, int y, int width, int height, Color color) : base(width, height)
		{
			Color = color;
		}

		public override void Draw()
		{
			SmartUI.DrawFillRect(Bounds, Color);
		}
	}

	/** Simple component to display a solid colored rectangle. */
	public class GuiFrameRect : GuiComponent
	{
		public GuiFrameRect(int x, int y, int width, int height, Color color) : base(width, height)
		{
			Color = color;
		}

		public override void Draw()
		{
			SmartUI.DrawFrameRect(Bounds, Color);
		}
	}
}