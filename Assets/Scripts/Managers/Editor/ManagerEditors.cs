using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(ResourceManager))]
public class ResourceManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ResourceManager script = (ResourceManager)target;

		GUILayout.Label("Resources: " + script.SpriteList.Count);

		ResourceManager.Include2x = GUILayout.Toggle(ResourceManager.Include2x, "Include 2x");

		if (GUI.changed) {
			UpdateResources();
		}

		DrawDefaultInspector();

		if (GUILayout.Button("Load")) {
			UpdateResources();
		}
	}

	public static void UpdateResources()
	{
		var resourceManager = ResourceManager.Instance;
		resourceManager.SetResources(getResourcesAtPath("/ResourceManager/" + resourceManager.ResourcePath));	
	}

	private static Sprite[] getResourcesAtPath(string path)
	{
		List<Sprite> result = new List<Sprite>();
		string[] files = Directory.GetFiles(Application.dataPath + path, "*.*");
		foreach (string matFile in files) {
			string assetPath = "Assets" + matFile.Replace(Application.dataPath, "").Replace('\\', '/');
			var asset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
			if (asset != null)
				result.Add(asset);
		}
		return result.ToArray();
	}

}
