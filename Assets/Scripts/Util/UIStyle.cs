using System;
using UI;
using UnityEngine;

public class UIStyle
{
	/** Styles a component with a red tint suitable for warnings. */
	public static void RedWarning(GuiComponent component)
	{
		Util.Assert(component != null, "Component must not be null.");
		component.ColorTransform = ColorTransform.BlackAndWhite;
		component.ColorTransform += ColorTransform.Multiply(1.2f, 1.2f, 1.2f);
		component.Color = new Color(1f, 0.4f, 0.3f);

	}

	/** Styles a component with a green tint. */
	public static void Green(GuiComponent component)
	{
		Util.Assert(component != null, "Component must not be null.");
		component.ColorTransform = ColorTransform.BlackAndWhite;
		component.ColorTransform += ColorTransform.Multiply(1.2f, 1.2f, 1.2f);
		component.Color = new Color(0.4f, 1.0f, 0.4f);
	}


}


