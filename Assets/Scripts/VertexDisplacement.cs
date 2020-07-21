using UnityEngine;
using System.Collections;

/** Add noise to vertexs based on world location. */
[ExecuteInEditMode]
public class VertexDisplacement : MonoBehaviour
{

	private Vector3 lastPosition;
	private Vector3 lastScale;
	private float lastNoiseScale;
	private Vector3[] origionalVertexes;
	private Mesh _uniqueMesh;
	public Mesh _origionalMesh;
	public float NoiseScale = 1f;

	public Vector3 Scale = new Vector3(0, 0.1f, 0);

	// Use this for initialization
	void Start()
	{
		getOrigionalVertexes();
		StartCoroutine(updateLoop());
	}

	/*
	void Update()
	{
		if ((lastPosition != transform.position) || (lastScale != Scale) || (lastNoiseScale != NoiseScale)) {
			AlterMesh();
		}
	}*/

	private void getOrigionalVertexes()
	{
		if (origionalVertexes != null)
			return;

		var mesh = (_origionalMesh != null) ? _origionalMesh : GetComponent<MeshFilter>().sharedMesh;

		if (mesh == null)
			return;

		origionalVertexes = mesh.vertices;
	}

	protected IEnumerator updateLoop()
	{
		var objectTransform = transform;
		var updateInterval = new WaitForSeconds(1f + Util.Roll(100, true) / 100f);

		while (true) {
			if ((lastPosition != objectTransform.position) || (lastScale != Scale) || (lastNoiseScale != NoiseScale)) {
				AlterMesh();
			}
			yield return updateInterval;
		}
	}

	private Mesh UniqueMesh {
		get {
			if (_uniqueMesh != null) {
				return _uniqueMesh;
			} else {
				_uniqueMesh = new Mesh();
				var mesh = GetComponent<MeshFilter>().sharedMesh;
				if (_origionalMesh == null)
					_origionalMesh = mesh;
				if (mesh == null)
					return null;
				_uniqueMesh.vertices = mesh.vertices;
				_uniqueMesh.SetIndices(mesh.GetIndices(0), MeshTopology.Triangles, 0);
				_uniqueMesh.uv = mesh.uv;
				_uniqueMesh.normals = mesh.normals;
				_uniqueMesh.tangents = mesh.tangents;
				_uniqueMesh.name = "mesh for " + this.name;
				return _uniqueMesh;
			}
		}
	}

	private void AlterMesh()
	{		
		if (origionalVertexes == null || origionalVertexes.Length == 0)
			return;

		var mesh = UniqueMesh;

		if (mesh == null)
			return;

		var worldPosition = transform.position;

		var localTransform = transform.localToWorldMatrix;

		var newVertices = new Vector3[origionalVertexes.Length];

		for (var lp = 0; lp < newVertices.Length; lp++) {

			var vertex = localTransform.MultiplyPoint(origionalVertexes[lp]);

			var adjustedScale = NoiseScale / 31.7f;

			var noisePower = 
				Mathf.PerlinNoise(vertex.x * adjustedScale, vertex.z * adjustedScale) * 0.5f +
				Mathf.PerlinNoise(vertex.x * adjustedScale * 2f, vertex.z * adjustedScale * 2f) * 0.33f +
				Mathf.PerlinNoise(vertex.x * adjustedScale * 4f, vertex.z * adjustedScale * 4f) * -0.23f;
				
			var noiseVector = Scale * noisePower;

			vertex = vertex + noiseVector;

			newVertices[lp] = localTransform.inverse.MultiplyPoint(vertex);
		}

		mesh.vertices = newVertices;

		GetComponent<MeshFilter>().sharedMesh = mesh;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		lastPosition = worldPosition;
		lastScale = Scale;
		lastNoiseScale = NoiseScale;
	}

}
