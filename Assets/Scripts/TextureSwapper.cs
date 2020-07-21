using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextureSwapper : MonoBehaviour
{

	/** List of materials to be processed. */
	public Material[] Materials;

	public Dictionary<Material,Material> origionalMaterials;

	public Material CompositeMaterial;

	[SerializeField]
	private bool initialized = false;

	private List<RenderTexture> replacementTextures;

	public static TextureSwapper Instance = null;

	void Start()
	{
		replacementTextures = new List<RenderTexture>();
		origionalMaterials = new Dictionary<Material, Material>();
		saveDungeonMaterials();
		initialized = true;
	}

	public TextureSwapper()
	{
		Instance = this;
		initialized = false;
	}


	void OnDestroy()
	{
		if (initialized)
			RestoreMaterials();
		initialized = false;
	}

	public void Clay()
	{
		var diffuseTexture = StockTexture.CreateSolidTexture(Util.HexToColor("FFDAAA"));

		SetShader("Standard");

		SwapTexture(diffuseTexture, "_MainTex");
		SwapTexture(null, "_MetallicGlossMap");

		SetValue("_Glossiness", 0.25f);
		SetValue("_Metallic", 0.2f);
		SetValue("_BumpScale", 0.5f);

		EnableKeyword("_NORMALMAP");
		DisableKeyWord("_METALLICGLOSSMAP");
		DisableKeyWord("_SPECGLOSSMAP");		
	}

	/** Merges occusion with difuse.  Useful for low quality modes. */
	public void CompositeOcculusion()
	{
		RestoreMaterials();

		if (replacementTextures != null) {
			foreach (var rt in replacementTextures) {
				rt.Release();
			}
		}

		foreach (var material in Materials) {
			var diffuse = material.mainTexture;
			var occulusion = material.GetTexture("_OcclusionMap");
			if (occulusion == null || diffuse == null)
				return;

			RenderTexture rt = new RenderTexture(diffuse.width, diffuse.height, 0);

			Graphics.Blit(diffuse, rt);
			Graphics.SetRenderTarget(rt);
			Graphics.Blit(occulusion, rt, CompositeMaterial);
			//Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height + 24), occulusion, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, Color.red.Faded(0.5f), CompositeMaterial);
			Graphics.SetRenderTarget(null);

			material.mainTexture = rt;

			replacementTextures.Add(rt);
		}
			
	}

	/** Saves a copy of the dungeion materials difuse texture so we can restore it later. */
	private void saveDungeonMaterials()
	{		
		foreach (var material in Materials) {
			var materialCopy = new Material(material);
			origionalMaterials[material] = materialCopy;
		}
	}

	/** Saves a copy of the dungeion materials difuse texture so we can restore it later. */
	private void showKeywords()
	{		
		foreach (var material in Materials) {
			string keywordsString = material.name + ": ";
			foreach (var item in material.shaderKeywords) {
				keywordsString += string.Format("{0} [{1}]", item, material.IsKeywordEnabled(item));
			}
			Trace.Log(keywordsString);
		}
	}

	/** Changes the textures for each material. */
	public void SwapTexture(Texture2D replacementTexture, string materialName = "_MainTex")
	{		
		foreach (var material in Materials) {
			if (material.HasProperty(materialName)) {
				material.SetTexture(materialName, replacementTexture);
				Trace.Log("Swaped material on [{0}]", material);
			}
		}
	}

	/** Set a property on texture. */
	public void SetValue(string propertyName, float value)
	{		
		foreach (var material in Materials) {
			if (material.HasProperty(propertyName)) {				
				material.SetFloat(propertyName, value);
				Trace.Log("Set value {0} on material [{1}] to {2}", propertyName, material, value);
			} else
				Trace.Log("No property '{0}' on material {1}", propertyName, material);
		}
	}

	public void EnableKeyword(string keyword)
	{		
		foreach (var material in Materials) {
			material.EnableKeyword(keyword);
		}
	}

	public void DisableKeyWord(string keyword)
	{		
		foreach (var material in Materials) {
			material.DisableKeyword(keyword);
		}
	}


	/** Set a property on texture. */
	public void SetShader(string shaderName)
	{		
		foreach (var material in Materials) {
			material.shader = Shader.Find(shaderName);
		}
	}

	/** Restores all materials to their default state. */
	public void RestoreMaterials()
	{			
		if (!initialized)
			return;
		foreach (var material in Materials) {			
			material.CopyPropertiesFromMaterial(origionalMaterials[material]);
		}
	}
}
