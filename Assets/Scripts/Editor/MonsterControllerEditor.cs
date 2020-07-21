using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MonsterController))]
public class MonsterControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		MonsterController script = (MonsterController)target;

		script.X = EditorGUILayout.IntField("X", script.X);
		script.Y = EditorGUILayout.IntField("Y", script.Y);
		script.Facing = 90 * EditorGUILayout.IntField("Facing", (int)script.Facing.Angle / 90);
	}
}

