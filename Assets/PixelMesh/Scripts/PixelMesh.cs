
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

public enum ColorMethod
{
	// UVs of each voxel are set to texels of the origional source image.  Most efficent method.
	UVs,
	// Color is set using each voxels render color.  This requires a renderer for each voxel and will impact performance.
	Renderer,
	// Color is set in the vertex colors, requires a special shader to see the voxel colors.
	Vertex
}

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PixelMesh : MonoBehaviour
{
	// Public properties.
	[Tooltip("The sprite used to create the mesh.  Height is in alpha.")]
	public Sprite Sprite;

	[Tooltip("Blocks will be centered.")]
	public bool Centered = false;

	[Tooltip("Height of the mesh.")]
	[Range(0.1f, 10f)]
	public float Height = 1f;

	[Tooltip("Reduces resolution of sprite.")]
	[Range(1, 4)]
	public int Downsample = 1;

	[Tooltip("Creates individual cubes unstead of a mesh.  Slower, but physics can be applied to each voxel.")]
	public bool Explode = false;

	private ColorMethod ColorMethod = ColorMethod.UVs;

	/** Settings the collider size to less than 1.0f will help with voxels not getting stuck during physics */
	public static float COLLIDER_SIZE = 0.97f;

	private int segmentsW = 0;
	private int segmentsH = 0;
	private float mWidth;
	private float mDepth;

	public float Width
	{ get { return mWidth; } }

	public float Depth
	{ get { return mDepth; } }

	private bool dirty = false;

	// used to offset texels
	private Vector2 uvOffset = new Vector2(0.25f, 0.25f);
	// 0.5f, 0.0f

	private Texture2D bitmapData;

	private List<Vector3> vertex;
	private List<int> indices;
	private Vector2[] normal;
	private List<Vector2> uvs;

	public Mesh VoxelMesh;
	public Material VoxelMaterial;

	// The UV frame allows stretching a subsection of a texture over the mesh.  This is helpful when a non power of two texture is used, or if there are
	// many images on one texture.
	private Rect uvFrame;

	public PixelMesh()
	{
		vertex = new List<Vector3>();
		indices = new List<int>();
		uvs = new List<Vector2>();
	}

	// Use this for initialization
	void Start()
	{

		VoxelMesh = getCubePrimitiveMesh();
		if (VoxelMaterial == null)
			VoxelMaterial = new Material(Shader.Find("Diffuse"));
		Apply();
	}
	
	// Update is called once per frame
	void Update()
	{
		if (dirty)
			Apply();
	}

	/** Apply to current mesh */
	public void Apply()
	{
		if (Sprite == null)
			return;

		if (GetComponent<MeshFilter>().sharedMesh == null)
			GetComponent<MeshFilter>().mesh = new Mesh();


		var mesh = GetComponent<MeshFilter>().sharedMesh;

		var normalisedUV = new Rect(Sprite.textureRect.x / Sprite.texture.width,
			                   Sprite.textureRect.y / Sprite.texture.height,
			                   Sprite.textureRect.width / Sprite.texture.width,
			                   Sprite.textureRect.height / Sprite.texture.height);

		PixelMeshGeometry(Sprite.texture, normalisedUV);

		mesh.SetTriangles(new int[0], 0);
		mesh.vertices = vertex.ToArray();
		mesh.SetTriangles(indices.ToArray(), 0);
		mesh.uv = uvs.ToArray();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		if (!Explode)
			GetComponent<MeshRenderer>().sharedMaterial.mainTexture = Sprite.texture;

		dirty = false;
	}

	private Mesh getCubePrimitiveMesh()
	{
		var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		var mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
		GameObject.DestroyImmediate(tempCube);
		return mesh;
	}

	private Material getDefaultMaterial()
	{
		var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		var material = tempCube.GetComponent<MeshRenderer>().sharedMaterial;
		GameObject.DestroyImmediate(tempCube);
		return material;
	}

	/** 
	 * Creates a pixel mesh from given bitmap data.
	 * 
	 * Currently only the red channel is used, and defines the height of the cube, however later other channels might specify other things 
	 * such as offset etc.
	 * 
	 * @param bitmapdata The bitmap to use to create the mesh.  Each pixel within the uvFrame will represent one cube on the mesh
	 * @param uvFrame The uvFraming to use when creating the mesh, and generating uvCo-ords.  Defaults to 0,0,1,1 which means the whole texture will be mapped over the mesh.
	 * @param centering This is just a temporary fix, it will center all blocks along the z axis.  Later on offset will be implemented on a per pixel
	 * bases with the attributes map.
	 * 
	 **/
	public void PixelMeshGeometry(Texture2D bitmapData, Rect uvFrame, bool centering = false)
	{
		this.bitmapData = bitmapData;
		this.mWidth = bitmapData.width * uvFrame.width;
		this.mDepth = bitmapData.height * uvFrame.height;
		this.segmentsW = (int)(bitmapData.width * uvFrame.width / Downsample);
		this.segmentsH = (int)(bitmapData.height * uvFrame.height / Downsample);		
		this.uvFrame = uvFrame;
	
		if ((mWidth * mDepth) > (128 * 128)) {
			throw new Exception("Sorry the dimentions [" + mWidth + "x" + mDepth + "] for pixel mesh are too large.  Try a smaller value"); 
		}

		buildGeometry();	
	}

	/** Converts from mesh x location to texture u co-ordinate via planar mapping */
	private float x2u(float x)
	{
		return ((x - uvOffset.x + 0.00f) / Width + 0.5f) * uvFrame.width + uvFrame.x;
	}

	/** Converts from mesh z location to texture v co-ordinate via planar mapping 
      * note: this has a bug and isn't right yet. */
	private float z2v(float z)
	{
		return (1 - (((z + uvOffset.y + 0.75f) / Depth) + 0.5f)) * uvFrame.height + uvFrame.y;
	}

	/** Helpful function for build geometry.  Adds a new face to the current texel. */
	private void addFace(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, float v3x, float v3y, float v3z, float v4x, float v4y, float v4z, float u, float v)
	{	
		// 0 - 2: vertex position X, Y, Z
		// 3 - 5: normal X, Y, Z
		// 6 - 8: tangent X, Y, Z
		// 9 - 10: U V
		// 11 - 12: Secondary U V

		int baseVertex = vertex.Count;

		// create vertexes
		vertex.Add(new Vector3(v1x, v1y, v1z));
		vertex.Add(new Vector3(v2x, v2y, v2z));
		vertex.Add(new Vector3(v3x, v3y, v3z));
		vertex.Add(new Vector3(v4x, v4y, v4z));
		 
		// calculate uvs		
		uvs.Add(new Vector2(u, v));
		uvs.Add(new Vector2(u, v));
		uvs.Add(new Vector2(u, v));
		uvs.Add(new Vector2(u, v));	

		// create faces
		indices.Add(baseVertex);
		indices.Add(baseVertex + 2);
		indices.Add(baseVertex + 1);
		indices.Add(baseVertex);
		indices.Add(baseVertex + 3);
		indices.Add(baseVertex + 2);	
	
	}

	private int sample(float x, float y)
	{
		return sample((int)x, (int)y);
	}

	/** Samples from current bitmap */
	private int sample(int x, int y)
	{
		// Calculates height based on neigbours.
		var AUTO_HEIGHT = false;

		// Randomly adjusts the height of texels.
		var RANDOM_HEIGHT = false;

		int sample = (int)(bitmapData.GetPixel(x, y).a * 255);

		if (sample == 0)
			return 0;

		if (RANDOM_HEIGHT) {
			UnityEngine.Random.seed = (int)(x + (y * bitmapData.width));
			sample += (int)UnityEngine.Random.Range(0, 100);
		}

		if (AUTO_HEIGHT) {
			int totalFaces = 0;
			if (bitmapData.GetPixel(x - 1, y).a != 0)
				totalFaces++;
			if (bitmapData.GetPixel(x + 1, y).a != 0)
				totalFaces++;
			if (bitmapData.GetPixel(x, y - 1).a != 0)
				totalFaces++;
			if (bitmapData.GetPixel(x, y + 1).a != 0)
				totalFaces++;
			sample = (int)((0.25f + totalFaces * 0.75f) * 255f);
		}

		return sample;

	}

	/** Creates a new mesh from source */
	private Mesh cloneMesh(Mesh source)
	{
		Mesh mesh = new Mesh();

		mesh.vertices = source.vertices;
		mesh.triangles = source.triangles;
		mesh.uv = source.uv;
		mesh.normals = source.normals;

		return mesh;
	}

	/** Sets all vertex colors to given color for given mesh */
	private void colorMesh(Mesh mesh, Color color)
	{
		int vertexes = mesh.vertexCount;
		var colors = new Color32[vertexes];
		for (int lp = 0; lp < vertexes; lp++)
			colors[lp] = color;
		mesh.colors32 = colors;
	}

	/** Sets all vertex uvs to given location for given mesh */
	private void uvMesh(Mesh mesh, Vector2 uv)
	{
		int vertexes = mesh.vertexCount;
		var uvs = new Vector2[vertexes];
		for (int lp = 0; lp < vertexes; lp++)
			uvs[lp] = uv;
		mesh.uv = uvs;
	}

	/** Creates cube at given co-ords */
	private GameObject createCube(float x, float y, float z, float sizeX, float sizeY, float sizeZ)
	{
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var mesh = cloneMesh(VoxelMesh);
		cube.GetComponent<MeshFilter>().mesh = mesh;      

		cube.transform.parent = gameObject.transform;

		cube.AddComponent<BoxCollider>().size = new Vector3(COLLIDER_SIZE, COLLIDER_SIZE, COLLIDER_SIZE);

		var rigidBody = cube.AddComponent<Rigidbody>();
		rigidBody.useGravity = true;        
        
		cube.transform.localEulerAngles = Vector3.zero;

		cube.transform.localPosition = new Vector3(x + (sizeX / 2), y + (sizeY / 2), z + (sizeZ / 2));
		cube.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

		cube.layer = gameObject.layer;

		cube.name = "Voxel";

		return cube;
	}

	/** Removes any created child objects */
	private void removeChildObjects()
	{
		var children = new List<GameObject>();
		foreach (Transform child in transform) {
			if (child.name == "Voxel")
				children.Add(child.gameObject);
		}
		children.ForEach(child => DestroyImmediate(child));
	}

	/** 
	 * builds geometry for a pixel mesh.  
	 * todo: Add some optermisations:
	 * 		- sides need not be full height, but only to the height of their neigbour (helps with overdraw)
	 * 		- bottom and top faces can be a single poly (greatly reducing the poly count) if it is drawn with an apropriate alpha map
	 * 		- I add a face if the texel differs in height from it's neigbour.  But this means two sides are added when only one would be needed.
	 */
	private void buildGeometry()
	{
		float x;
		float y;
		float z;

		float uDiv = (bitmapData.width * uvFrame.width) / segmentsW;
		float vDiv = (bitmapData.height * uvFrame.height) / segmentsH;
		float u;
		float v;

		vertex.Clear();
		indices.Clear();
		uvs.Clear();

		removeChildObjects();
       
		// Used for debuging..
		var TOP_FACE_ONLY = false;

		float voxelWidth = (bitmapData.width * uvFrame.width) / segmentsW;
		float voxelHeight = (bitmapData.height * uvFrame.height) / segmentsH;

		// Create texels for each non trasperiant bitmap pixel
		for (int zi = 0; zi < segmentsH; ++zi) {
			for (int xi = 0; xi < segmentsW; ++xi) {
				// calculate some useful variables
				x = ((float)xi / segmentsW - 0.5f) * Width;
				z = ((((float)segmentsH - 1f) - zi) / segmentsH - 0.5f) * Depth;

				// current bitmap sample
				u = (xi * uDiv) + uvFrame.x * bitmapData.width;
				v = (zi * vDiv) + uvFrame.y * bitmapData.height;

				// uv co-rds (note we inset the sample point ever so slightly from the texels top left corner (to avoid rounding issues)
				float uu = uvFrame.x + ((float)(xi) / segmentsW) * uvFrame.width + uvOffset.x / bitmapData.height;
				float vv = uvFrame.y + ((float)(zi) / segmentsH) * uvFrame.height + uvOffset.y / bitmapData.width;

				int thisSample = sample(u, v);
				Color color = bitmapData.GetPixel((int)u, (int)v);

				if (thisSample == 0)
					continue;

				// get our alpha value, and work out the height
				y = (thisSample / 255f) * Height;

				// sample neigbours
				int leftSample = sample((u - uDiv), v);
				int rightSample = sample(u + uDiv, v);
				int backSample = sample(u, v + vDiv);
				int frontSample = sample(u, v - vDiv);

				// see which faces we need to render
				bool topFace = true;
				bool bottomFace = true;

				bool leftFace = (xi == 0) || (leftSample != thisSample);
				bool rightFace = (xi == segmentsW - 1) || (rightSample != thisSample);
				bool frontFace = (zi == segmentsH - 1) || (frontSample != thisSample);
				bool backFace = (zi == 0) || (backSample != thisSample);

				float top;
				float bottom;

				if (Centered) {
					top = 1 - (1 - y) / 2;
					bottom = (1 - y) / 2;
				} else {
					top = (y);
					bottom = 0;
				}
     				
				if (TOP_FACE_ONLY) {
					leftFace = false;
					rightFace = false;
					frontFace = false;
					backFace = false;
					bottomFace = false;
				} 

				if (Explode) {
					var voxel = createCube(x, bottom, z, voxelWidth, (top - bottom), voxelHeight);
					voxel.GetComponent<MeshRenderer>().sharedMaterial = VoxelMaterial;                            

					switch (ColorMethod) {
						case ColorMethod.UVs:
							uvMesh(voxel.GetComponent<MeshFilter>().sharedMesh, new Vector2(uu, vv));
							break;
						case ColorMethod.Renderer:                            
							throw new NotImplementedException();
						case ColorMethod.Vertex:
							throw new NotImplementedException();
					}

				} else {
					// top face
					if (topFace)
						addFace(
							x, top, z,
							x + uDiv, top, z,
							x + uDiv, top, z + vDiv,
							x, top, z + vDiv,
							uu, vv);

					// bottom face
					if (bottomFace)
						addFace(
							x, bottom, z,
							x, bottom, z + vDiv,
							x + uDiv, bottom, z + vDiv,
							x + uDiv, bottom, z,
							uu, vv);

					if (leftFace)
						addFace(
							x, top, z,
							x, top, z + vDiv,
							x, bottom, z + vDiv,
							x, bottom, z,
							uu, vv);

					if (rightFace)
						addFace(
							x + uDiv, top, z,
							x + uDiv, bottom, z,
							x + uDiv, bottom, z + vDiv,
							x + uDiv, top, z + vDiv,
							uu, vv);

					if (frontFace)
						addFace(
							x, top, z + vDiv,
							x + uDiv, top, z + vDiv,
							x + uDiv, bottom, z + vDiv,
							x, bottom, z + vDiv,
							uu, vv);

					if (backFace)
						addFace(
							x, top, z,
							x, bottom, z,
							x + uDiv, bottom, z,
							x + uDiv, top, z,
							uu, vv);
				}
			}
		}

	}

}
