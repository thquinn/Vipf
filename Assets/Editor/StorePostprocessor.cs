using UnityEditor;
using UnityEngine;

public class StorePostprocessor : AssetPostprocessor {
    void OnPostprocessModel(GameObject root) {
        if (!assetPath.EndsWith("/store.fbx")) return;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) {
            var go = mf.gameObject;
            if (go.name.ToLower().EndsWith("-mc")) continue;

            var meshCol = go.GetComponent<MeshCollider>();
            if (meshCol != null) Object.DestroyImmediate(meshCol);

            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();

            box.center = mf.sharedMesh.bounds.center;
            box.size = mf.sharedMesh.bounds.size;
        }
    }
}