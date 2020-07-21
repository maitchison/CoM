using UnityEngine;

/** Defines the type of texel in the sprite sheet */
enum TexelType { Normal, Background, Edge, Shadow }

[ExecuteInEditMode]
/** Process a sprite sheet remapping various colours */
public class ProcessSpriteSheet : MonoBehaviour 
{
	public Texture2D Source;

	/** Destination file to write to */
	public string DestinationPath;

	/** The color to use for background texels */
	public Color BackgroundColor;
	/** The color to use for edge texels */
	public Color EdgeColor;
	/** The color to use for shadow texels */
	public Color ShadowColor;

	/** Set to true to reprocess the texture */
	public bool Refresh = false;

	/** The color that the source uses to represent transparient colors */
	private Color32 sourceTransparientColor;
	
	/** Source bitmap data */
	private Color32[] data;

	private Texture2D Destination;

	void Start () 
	{
		BackgroundColor = Color.clear;
		EdgeColor = Color.black;
		ShadowColor = new Color(0f,0f,0f,0.5f);
		sourceTransparientColor = new Color32(255,0,255,255);
	}

	void Update () 
	{
		if (Refresh)
			Process();
	}
	
	private void CreateDestinationTexture()
	{
		if (Source)
			Destination = new Texture2D(Source.width,Source.height,TextureFormat.ARGB32,false);
		else
			Destination = null;
	}

	/** Gets a texel x,y, out of bounds returns pink. */
	private Color32 GetTexel(int x,int y)
	{
		int index = x + y * Source.width;
		if ((index < 0) || (index >= data.Length))
			return sourceTransparientColor;
		return data[index];
	}

	/** Returns true if texel color is 'normal' i.e. not source background color, and not edge */
	private bool NormalColor(Color32 col)
	{
		return (!col.Equals(sourceTransparientColor) && !SourceEdgeColor(col));
	}

	/** Returns true if texel color is edge color.  This is more complicated that it needs to be because edges can be any of the first 32 grey colors */
	private bool SourceEdgeColor(Color32 col)
	{
		return (((col.r == col.g) && (col.r == col.b)) && (col.r <= 32));
	}

	/** Calculates the texel type at x,y */
	private TexelType GetTexelType(int x,int y)
	{
		Color32 col = GetTexel(x,y);

		// background is source transparient color 
		if (col.Equals(sourceTransparientColor))
			return TexelType.Background;

		// work out what to do with the black texels
		if (SourceEdgeColor(col))
		{
			int adjoiningNormalTexelCount = 0;
			if (NormalColor(GetTexel(x-1,y))) adjoiningNormalTexelCount ++;
			if (NormalColor(GetTexel(x+1,y))) adjoiningNormalTexelCount ++;
			if (NormalColor(GetTexel(x,y-1))) adjoiningNormalTexelCount ++;
			if (NormalColor(GetTexel(x,y+1))) adjoiningNormalTexelCount ++;

			if (adjoiningNormalTexelCount == 0)
				return TexelType.Shadow;
			if (adjoiningNormalTexelCount >= 3)
				return TexelType.Normal;

			return TexelType.Edge;
		}

		return TexelType.Normal;
	}
	

	/** Process the source texture by remapping the colors, write to destination as output */
	private void Process()
	{
		Refresh = false;

		CreateDestinationTexture();
		
		if ((Source == null) || (Destination == null)) return;

		Trace.Log("Processing texture "+Source);

		if (!TextureMagic.IsReadable(Source))
			throw new UnityException("Can not read from source texture.");

		data = Source.GetPixels32();

		for (int ylp = 0; ylp < Source.height; ylp ++)
		{
			for (int xlp = 0; xlp < Source.width; xlp ++)
			{
				Color32 col = Color.red;

				switch (GetTexelType(xlp,ylp))
				{
				case TexelType.Background:
					col = BackgroundColor;
					break;
				case TexelType.Edge:
					col = EdgeColor;
					break;
				case TexelType.Normal:
					col = GetTexel(xlp,ylp);
					break;
				case TexelType.Shadow:
					col = ShadowColor;
					break;

				}
				Destination.SetPixel(xlp,ylp,col);
			}
		}

		Destination.Apply();
		TextureMagic.SaveTextureToFile(Destination,DestinationPath);

		Trace.Log ("Saved "+DestinationPath);

		data = null;
	}
}
