using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(PixelMesh))]
public class PixelMeshEditor : Editor
{

    [MenuItem("GameObject/3D Object/Pixel Mesh")]
    public static void CreatePixelMesh() {

	    // create the pixel mesh
        var newObject = new GameObject("Pixel Mesh");
        newObject.AddComponent<PixelMesh>();

        // create empty mesh to use.
        var mesh = new Mesh();
        mesh.name = "PixelMesh";
        newObject.GetComponent<MeshFilter>().mesh = mesh;

        // create material
        var material = new Material(Shader.Find("Diffuse"));
        newObject.GetComponent<MeshRenderer>().material = material;
    }

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		base.OnInspectorGUI();
		if (EditorGUI.EndChangeCheck())
			(this.target as PixelMesh).Apply();
	}
}
