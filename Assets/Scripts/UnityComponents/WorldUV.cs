using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class WorldUV : MonoBehaviour
{
	private Vector3 lastPosition;
	private Vector3 lastScale;

	public Vector3 Scale = new Vector3(1, 1, 1);

	// Use this for initialization
	void Start()
	{
		StartCoroutine(updateLoop());
	}

	protected IEnumerator updateLoop()
	{
		var objectTransform = transform;
		var updateInterval = new WaitForSeconds(1f + Util.Roll(100, true) / 100f);

		while (true) {			
			if ((lastPosition != objectTransform.position) || (lastScale != Scale)) {
				AlterMesh();
			}
			yield return updateInterval;
		}
	}

	private void AlterMesh()
	{		
		var mesh = GetComponent<MeshFilter>().sharedMesh;

		if (mesh == null)
			return;

		var worldPosition = transform.position;

		Vector2[] uvs = new Vector2[mesh.vertices.Length];	

		var localTransform = transform.localToWorldMatrix;

		for (var lp = 0; lp < uvs.Length; lp++) {

			var vertex = localTransform.MultiplyPoint(mesh.vertices[lp]);

			var uv = (new Vector2(vertex.x, vertex.z) - new Vector2(0.5f, 0.5f));
			uv.Scale(Scale);
			uvs[lp] = new Vector2(uv.x, uv.y);
		}
		mesh.uv = uvs;
		lastPosition = worldPosition;
		lastScale = Scale;
	}

}
