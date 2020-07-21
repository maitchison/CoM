using UnityEngine;

/** Defines how a gui object should be drawn */
public struct DrawParameters
{
	public static DrawParameters DebugDraw = new DrawParameters(ColorTransform.Identity) { IsDebugDraw = true };

	public static DrawParameters NoAlpha {
		get {
			var dp = new DrawParameters(ColorTransform.Identity);
			dp.AlphaBlend = false;
			return dp;
		}
	}

	public static DrawParameters Default = new DrawParameters(ColorTransform.Identity);
	public static DrawParameters BlackAndWhite = new DrawParameters(ColorTransform.BlackAndWhite);
	public static DrawParameters ShowAlpha = new DrawParameters(ColorTransform.ShowAlpha);

	public ColorTransform Transform;
	public bool Blur;
	public Vector2 Scale;

	/** Rotation in degrees. */
	public float Rotation;

	public bool AlphaBlend;
	/** This draw call is a debugging draw, so disable debug info for it. */
	public bool IsDebugDraw;

	public DrawParameters(ColorTransform colorTransform, bool blur = false)
	{
		Transform = colorTransform;
		Blur = blur;
		IsDebugDraw = false;
		AlphaBlend = true;
		Scale = Vector2.one;
		Rotation = 0;
	}

	/** Returns if these draw parameters are visible or not */
	public bool IsVisibile(GUIStyle style)
	{
		return (style != null) && (style.normal.background != null); 
	}

	/** Returns if these draw parameters are visible or not */
	public bool IsVisibile(Texture texture)
	{
		return (texture != null);
	}

	public Texture GetTexture(GUIStyle style)
	{
		return style.normal.background;
	}

	public Color DebugColor {
		get { 
			Material material = getMaterial();
			if (material.HasProperty("_DebugColor"))
				return material.GetColor("_DebugColor");
			return Color.white;
		}
	}

	/** Returns the most approriate material to use */
	private Material getMaterial()
	{
		if (Blur)
			return Engine.Instance.GuiMaterial_Blurred;
		if (!Transform.IsIdentity)
			return Engine.Instance.GuiMaterial_ColorTransform;

		return AlphaBlend ? Engine.Instance.GuiMaterial : Engine.Instance.GuiMaterial_Solid;
	}

	/** Applies drawing parameters to a sutiable guiMaterial and then return it */
	public Material GetPreparedMaterial(Texture texture)
	{
		if (!IsVisibile(texture))
			return null;
		
		Material guiMaterial = getMaterial();

		if (guiMaterial.HasProperty("_TexelSize"))
			guiMaterial.SetVector("_TexelSize", texture.texelSize);
		
		// Color transform isn't a property so we just go ahead and try and set it.
		guiMaterial.SetMatrix("_ColorTransform", Transform.Matrix);
		
		if (guiMaterial.HasProperty("_ColorOffset"))
			guiMaterial.SetVector("_ColorOffset", Transform.ColorOffset);		

		return guiMaterial;		
	}

	/** Applies drawing parameters to a sutiable guiMaterial and then return it */
	public Material GetPreparedMaterial(GUIStyle style)
	{
		if (!IsVisibile(style))
			return null;
		return GetPreparedMaterial(GetTexture(style));
	}

}