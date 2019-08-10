using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapManager))]
public class MapManagerEditor : Editor {

	public override void OnInspectorGUI() {

		DrawDefaultInspector ();

		MapManager map = target as MapManager;
		MapMeshManager mapMesh = map.GetComponent<MapMeshManager> ();

		if (GUILayout.Button ("Generate Map")) {
			mapMesh.DestroyAllChildren ();
			mapMesh.DestroyWallMesh();
			map.GenerateMap ();

		}
	}

}
