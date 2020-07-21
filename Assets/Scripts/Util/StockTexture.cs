
using System;
using UnityEngine;


/** Static class for creating and holding solid color textures */
public class StockTexture 
{
	public static Texture2D WhiteLR = CreateGradedTexture(
		new Color(1.0f,1.0f,1.0f),new Color(0.5f,0.5f,0.5f),
		new Color(0.5f,0.5f,0.5f),new Color(1.0f,1.0f,1.0f));
	public static Texture2D White = CreateSolidTexture(Color.white);
	public static Texture2D Black = CreateSolidTexture(Color.black);
	public static Texture2D Gray = CreateSolidTexture(Color.gray);
	public static Texture2D SimpleRect = CreateRectTexture(Color.white,new Color(0f,0f,0f,0f));

	/** Creates a 1x1 texture using the given color */
	public static Texture2D CreateSolidTexture(Color color) 
	{
		Texture2D result = new Texture2D(1,1);
		result.SetPixel(0,0,color);
		result.Apply();
		return result;
	}

	/** Creates a 2x2 texture using the given colors */
	public static Texture2D CreateGradedTexture(Color c1,Color c2,Color c3,Color c4) 
	{
		Texture2D result = new Texture2D(2,2);
		result.SetPixel(0,0,c1);
		result.SetPixel(1,0,c2);
		result.SetPixel(1,1,c3);
		result.SetPixel(0,1,c4);
		result.Apply();
		return result;
	}

	/** Creates a 4x4 texture where edge pixels and inside pixels are the given colors*/
	public static Texture2D CreateRectTexture(Color outside, Color inside) 
	{
		Texture2D result = new Texture2D(4,4);
		for (var ylp = 0; ylp <= 3; ylp++)
			for (var xlp = 0; xlp <= 3; xlp++)
				result.SetPixel(xlp,ylp,outside);
		for (var ylp = 1; ylp <= 2; ylp++)
			for (var xlp = 1; xlp <= 2; xlp++)
				result.SetPixel(xlp,ylp,inside);

		result.Apply();
		return result;
	}

}


