using UnityEngine;
using System.Collections;

/** Removes height map from a material */
public class RemoveHeightmapFromMaterial : MonoBehaviour
{
	public Material SourceMaterial;

	// Use this for initialization
	void Start()
	{
		if (SourceMaterial.HasProperty("_ParallaxMap"))
			SourceMaterial.SetTexture("_ParallaxMap", null);
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}
}
