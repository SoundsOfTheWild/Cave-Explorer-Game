using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapMeshManager))]
public class MapMeshManagerEditor : Editor {

	public override void OnInspectorGUI() {

		DrawDefaultInspector ();

		MapMeshManager mapMesh = target as MapMeshManager;

		if (GUILayout.Button("DestroyMesh")) {
			mapMesh.DestroyAllChildren ();
			mapMesh.DestroyWallMesh ();
            mapMesh.DestroyColliders();
		}

	}
}
