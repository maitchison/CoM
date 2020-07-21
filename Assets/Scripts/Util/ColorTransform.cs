
using UnityEngine;

using System.Collections.Generic;


public enum ColorChannel
{
	None,
	Red,
	Green,
	Blue,
	Alpha
}

/** 
 * Class to handle color transforms. 
 * Unfortunately multiplying the matrixes for cumulative effect won't work terraibly well because it uses a 4x4 matrix and a vector4 instead
 * if a 5x5 matrix as it should. 
 * 
 * see: http://docs.rainmeter.net/tips/colormatrix-guide
 */
public struct ColorTransform
{
	public static ColorTransform Identity = new ColorTransform(Matrix4x4.identity);
	public static ColorTransform BlackAndWhite = Saturation(0.0f);
	public static ColorTransform HalfSaturation = Saturation(0.5f);
	public static ColorTransform ShowAlpha = ChannelRemap(ColorChannel.Alpha, ColorChannel.Alpha, ColorChannel.Alpha, ColorChannel.Alpha);
	public static ColorTransform Bronze = Multiply(new Color32(255, 203, 125, 255));
	public static ColorTransform Faded = Multiply(new Color(0.7f, 0.7f, 0.5f));
	public static ColorTransform Saphire = Multiply(new Color(0.7f, 0.7f, 1.0f));

	public Matrix4x4 Matrix;

	// Note: a 5x5 matrix would be perfect, but it's a no go so I'm using a matrix multiply and an add instead.
	public Vector4 ColorOffset;

	public ColorTransform(Matrix4x4 matrix, Vector4 colorOffset)
	{
		Matrix = matrix;
		ColorOffset = colorOffset;
	}

	public ColorTransform(Matrix4x4 matrix) : this(matrix, Vector4.zero)
	{
	}

	/** Overload addition to allow transformation combining */
	public static ColorTransform operator +(ColorTransform t1, ColorTransform t2)
	{
		ColorTransform result = new ColorTransform();
		result.Matrix = t1.Matrix * t2.Matrix;
		result.ColorOffset = (t1.ColorOffset + t2.ColorOffset);
		return result;
	}



	/** Returns a preset color transform by its name, or identity if not found. */
	public static ColorTransform ByName(string name)
	{
		switch (name) {
		case "Normal":
			return ColorTransform.Identity;
		case "BlackAndWhite":
			return ColorTransform.BlackAndWhite;
		case "Bronze":
			return ColorTransform.Bronze;
		case "Faded":
			return ColorTransform.Faded;
		case "Saphire":
			return ColorTransform.Saphire;
		default:
			return Identity;
		}
	}

	public bool IsIdentity {
		get { 
			// for some reason Matrix.isIdentity is broken 
			return (				
			    (Matrix.GetRow(0) == new Vector4(1, 0, 0, 0)) &&
			    (Matrix.GetRow(1) == new Vector4(0, 1, 0, 0)) &&
			    (Matrix.GetRow(2) == new Vector4(0, 0, 1, 0)) &&
			    (Matrix.GetRow(3) == new Vector4(0, 0, 0, 1)) &&
			    ColorOffset == new Vector4(0, 0, 0, 0)
			);
		}
	}

	private static Vector4 ChannelToVector(ColorChannel channel)
	{
		switch (channel) {
		case ColorChannel.Red:
			return new Vector4(1, 0, 0, 0);
		case ColorChannel.Green:
			return new Vector4(0, 1, 0, 0);
		case ColorChannel.Blue:
			return new Vector4(0, 0, 1, 0);
		case ColorChannel.Alpha:
			return new Vector4(0, 0, 0, 1);
		default:
			return new Vector4(0, 0, 0, 0);
		}
	}

	/** Remaps color channels */ 
	public static ColorTransform ChannelRemap(ColorChannel red, ColorChannel green, ColorChannel blue, ColorChannel alpha)
	{
		Matrix4x4 matrix = new Matrix4x4();

		matrix.SetColumn(0, ChannelToVector(red));
		matrix.SetColumn(1, ChannelToVector(green));
		matrix.SetColumn(2, ChannelToVector(blue));
		matrix.SetColumn(3, ChannelToVector(alpha));

		return new ColorTransform(matrix);

	}

	/** Creates a transform to adjust saturation */
	public static ColorTransform Saturation(float value)
	{
		Matrix4x4 matrix = new Matrix4x4();

		float s = value;
		float sr = (1 - s) * 0.3086f;
		float sg = (1 - s) * 0.6094f;
		float sb = (1 - s) * 0.0820f;

		matrix.SetRow(0, new Vector4(sr + s,	sr, sr, 0));
		matrix.SetRow(1, new Vector4(sg, sg + s,	sg, 0));
		matrix.SetRow(2, new Vector4(sb, sb, sb + s,	0));
		matrix.SetRow(3, new Vector4(0, 0, 0, 1));

		return new ColorTransform(matrix);
	}

	/** Creates a color multiply adjustment */
	public static ColorTransform Multiply(float red, float green, float blue, float alpha = 1.0f)
	{
		Matrix4x4 matrix = new Matrix4x4();
		matrix.SetRow(0, new Vector4(red,	0, 0, 0));
		matrix.SetRow(1, new Vector4(0, green,	0, 0));
		matrix.SetRow(2, new Vector4(0, 0, blue,	0));
		matrix.SetRow(3, new Vector4(0, 0, 0, alpha));

		return new ColorTransform(matrix);
	}

	public static ColorTransform Multiply(Color color)
	{
		return Multiply(color.r, color.g, color.b, color.a);
	}

	/** Creates a tint and multiply adjustment */
	public static ColorTransform TintAndMultiply(float tintRed, float tintGreen, float tintBlue, float tintAlpha, float red, float green, float blue, float alpha)
	{
		Matrix4x4 matrix = new Matrix4x4();
		matrix.SetRow(0, new Vector4(red,	0, 0, 0));
		matrix.SetRow(1, new Vector4(0, green,	0, 0));
		matrix.SetRow(2, new Vector4(0, 0, blue,	0));
		matrix.SetRow(3, new Vector4(0, 0, 0, alpha));

		return new ColorTransform(matrix, new Vector4(tintRed, tintGreen, tintBlue, tintAlpha));
	}

	public static ColorTransform TintAndMultiply(Color tint, Color multiply)
	{	
		return TintAndMultiply(tint.r, tint.g, tint.b, tint.a, multiply.r, multiply.g, multiply.b, multiply.a);
	}
}
