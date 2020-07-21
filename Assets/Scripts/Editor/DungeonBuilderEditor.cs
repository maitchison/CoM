using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Furnishing))]
public class FurnishingDrawer: PropertyDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{		
		EditorGUIUtility.labelWidth = 65;

		var enabled = prop.FindPropertyRelative("Enabled");
		var source = prop.FindPropertyRelative("Source");
		var placementMethod = prop.FindPropertyRelative("Placement");
		var chance = prop.FindPropertyRelative("Chance");
		var group = prop.FindPropertyRelative("GroupID");
		var pv = prop.FindPropertyRelative("PositionVariation");
		var rv = prop.FindPropertyRelative("RotationVariation");

		var previousEnabled = GUI.enabled;

		EditorGUI.PropertyField(pos, enabled);
		GUI.enabled = enabled.boolValue;

		pos.y += 16;

		var sourcePos = pos;
		sourcePos.xMax = pos.width - 100 - 10;
		var groupPos = pos;
		groupPos.xMin = pos.width - 100 + 10;


		EditorGUI.PropertyField(sourcePos, source);
		EditorGUI.PropertyField(groupPos, group);

		GUILayout.Space(16);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PropertyField(placementMethod, GUILayout.Width(160));
		EditorGUIUtility.labelWidth = 55;
		EditorGUILayout.PropertyField(chance, GUILayout.ExpandWidth(true));
		EditorGUILayout.EndHorizontal();			
		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 120;
		EditorGUILayout.PropertyField(pv, GUILayout.Width(160));
		EditorGUILayout.PropertyField(rv, GUILayout.Width(160));
		EditorGUILayout.EndHorizontal();			
		EditorGUILayout.Separator();

		GUI.enabled = previousEnabled;
	}
}

/** 
 * Custom property drawer for an array of dungeon detail descriptions.  Unity doesn't support List<class> in the UI 
 * unless the class is a known type, like int. So I need to make my own custom editor for it.
 */
[CustomPropertyDrawer(typeof(FurnishingList))]
public class FurnishingListDrawer: PropertyDrawer
{
	private static bool expanded = false;

	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{		
		var list = prop.FindPropertyRelative("List");

		Util.Assert(list != null, "No list in FurnishingList property.");

		expanded = EditorGUI.Foldout(pos, expanded, prop.name);
		if (expanded) {

			for (int lp = 0; lp < list.arraySize; lp++) {
				EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(lp));
			} 

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Add", GUILayout.Width(100))) {
				list.arraySize++;	
			}

			if (GUILayout.Button("Remove", GUILayout.Width(100))) {
				if (list.arraySize > 0)
					list.arraySize--;	
			}

			GUILayout.EndHorizontal();
		}

	}
}

[CustomEditor(typeof(DungeonBuilder))]
public class DungeonBuilderEditor : Editor
{
	[MenuItem("Dungeon/Rebuild")]
	public static void RebuildAll()
	{
		DungeonBuilder.InitializeAndRebuildDungeon();
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		DungeonBuilder script = (DungeonBuilder)target;

		bool isInitialized = script.IsInitialized;

		GUI.enabled = isInitialized;

		script.BuildMethod = (DungeonBuildMethod)EditorGUILayout.EnumPopup(script.BuildMethod);

		if (GUILayout.Button("Rebuild Map")) {
			script.RebuildSelectedMap();
		}

		if (GUILayout.Button("Rebuild Dungeon")) {
			script.RebuildDungeon();
		}

		GUI.enabled = true;

		if (!isInitialized)
			GUILayout.Label("You must initialize before rebuilding.");

		if (GUILayout.Button("Initialize")) {
			script.Initialize();
		}

		if (GUILayout.Button("Clear")) {
			DungeonBuilder.ClearAll();
		}

		GUILayout.Label("Active Level:");
		if (isInitialized)
			script.SelectedMap = EditorGUILayout.IntSlider(script.SelectedMap, 0, script.Maps);
	}
}

