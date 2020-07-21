
using System;
using System.IO;

using UnityEngine;

/** Routines for writing to textures */
public class TextureMagic
{
	public TextureMagic()
	{
		throw new Exception("Texture Magic is a static class");
	}

	/** 
	 * Writes source sprite to destination texture, without any alpha transperiancy 
	 * Destination.apply will need to be updated for results to show 
	 **/
	public static void Blit(Sprite source, Texture2D destination, int x, int y)
	{
		destination.SetPixels(x, y, (int)source.rect.width, (int)source.rect.height, source.texture.GetPixels(0, 0, (int)source.rect.width, (int)source.rect.height));
	}

	/**
	 * Clears the destination with black
	 **/
	public static void Clear(Texture2D destination, Color color = default(Color))
	{
		Color32 color32 = color;
		Color32[] data = new Color32[destination.width * destination.height];
		for (int lp = 0; lp < data.Length; lp++)
			data[lp] = color32;
		destination.SetPixels32(data);
	}

	/**
	 * Draws a filled rectangle at given co-ords 
	 **/
	public static void FillRect(Texture2D destination, int x, int y, int width, int height, Color color)
	{
		Color[] colors = new Color[width * height];
		for (int lp = 0; lp < width * height; lp++) {
			colors[lp] = color;
		}
		destination.SetPixels(x, y, width, height, colors);
	}

	/**
	 * Draws a framed rectangle at given co-ords 
	 **/
	public static void FrameRect(Texture2D destination, int x, int y, int width, int height, Color color)
	{
		Color[] colors = new Color[width + height];
		for (int lp = 0; lp < width + height; lp++) {
			colors[lp] = color;
		}
		destination.SetPixels(x, y, width, 1, colors);
		destination.SetPixels(x, y + height - 1, width, 1, colors);
		destination.SetPixels(x, y, 1, height, colors);
		destination.SetPixels(x + width - 1, y, 1, height, colors);
	}


	/** 
	 * Writes source sprite to destination texture, using the source alpha blending.  
	 * Destination.apply will need to be updated for results to show 
	 **/
	public static void BlitWithAlpha(Sprite source, Texture2D destination, int x, int y)
	{
		Color[] data = source.texture.GetPixels((int)source.rect.x, (int)source.rect.y, (int)source.rect.width, (int)source.rect.height);

		int index = 0;

		int height = (int)source.rect.height;
		int width = (int)source.rect.width;

		for (int ylp = 0; ylp < height; ylp++) {
			for (int xlp = 0; xlp < width; xlp++) {
				Color sourceTexel = data[index];
				index++;

				if (sourceTexel.a == 0.0)
					continue;
			
				if (sourceTexel.a != 1.0) {
					Color destinationTexel = destination.GetPixel(x + xlp, y + ylp);
					float origionalSourceAlpha = sourceTexel.a;
					sourceTexel = Color.Lerp(destinationTexel, sourceTexel, sourceTexel.a);
					sourceTexel.a = 1 - ((1 - origionalSourceAlpha) * (1 - destinationTexel.a));
				}

				destination.SetPixel(x + xlp, y + ylp, sourceTexel);

			}
		}
	}

	/**
	 * Returns if the given texture is readable or not 
	 */
	public static bool IsReadable(Texture2D source)
	{
		try {
			source.GetPixel(0, 0);
			return true;
		} catch {
			return false;
		}
	}

	/** Saves texture to file */
	public static void SaveTextureToFile(Texture2D texture, string fileName)
	{
		byte[] bytes = texture.EncodeToPNG();
		FileStream file = File.Open(Application.dataPath + "/" + fileName, FileMode.Create);
		BinaryWriter binary = new BinaryWriter(file);
		binary.Write(bytes);
		file.Close();
	}
}
