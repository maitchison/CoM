using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

/** When enabled will continiously allocate memory. */
public class TestMemoryAllocator : MonoBehaviour {

	public const int AllocationBlockSize = 1024*1024;

	public float TotalMBToAllocate = 2048;
	public float TotalTextureMBToAllocate = 512;

	public Text OutputLabel;

	public long TotalMemoryAllocation {
		get { return AllocationBlockSize * memoryBlocks.Count; }
	}

	private List<object> memoryBlocks;
	private List<Texture> textures;

	// Use this for initialization
	void Start () {
		memoryBlocks = new List<object> ();
		textures = new List<Texture> ();
	}

	public void StartMemoryTest()
	{
		StartCoroutine(gcMemoryTest());
	}

	public void StartTextureMemoryTest()
	{
		StartCoroutine(textureMemoryTest());
	}

	/** Creates a new 1 meg texture. */
	private Texture2D create1mbTexture()
	{
		var texture = new Texture2D(512,512,TextureFormat.ARGB32,false);
		texture.Apply(false,true);
		return texture;
	}

	/** Tests limits of Texture Memory. */
	private IEnumerator textureMemoryTest()
	{		
		Trace.Log ("Starting texture memory test...");

		while (textures.Count < TotalTextureMBToAllocate) {
			try {
				var texture = create1mbTexture();
				textures.Add(texture);
			} catch (Exception error) {
				Trace.Log ("Error at texture {0} - {1}", textures.Count , error);
				yield break;
			}

			var debugString = string.Format("Total allocation: {0}mb [gc:{1}]",textures.Count,System.GC.GetTotalMemory(false)/1024/1024);

			if (OutputLabel) {
				OutputLabel.text = debugString;
			}

			Trace.Log (debugString);
			yield return new WaitForEndOfFrame ();
		}
	}

	/** Tests limits of GC. */
	private IEnumerator gcMemoryTest()
	{		
		Trace.Log ("Starting memory test...");
		while (TotalMemoryAllocation < TotalMBToAllocate*1024*1024) {
			try {
				byte[] data = new byte[AllocationBlockSize];
				memoryBlocks.Add(data);
			} catch (Exception error) {
				Trace.Log ("Error at {0}: {1}", TotalMemoryAllocation/1024/1024, error);
				yield break;
			}

			var debugString = string.Format("Total allocation: {0} - [gc:{1}]",TotalMemoryAllocation/1024/1024,System.GC.GetTotalMemory(false)/1024/1024);
			if (OutputLabel) {
				OutputLabel.text = debugString;
			}
			Trace.Log (debugString);
			yield return new WaitForEndOfFrame ();
		}
	}
}
