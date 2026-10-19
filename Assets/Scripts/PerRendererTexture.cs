using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class PerRendererTexture : MonoBehaviour {
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Albedo")]
    [Tooltip("Leave empty to use the texture from the shared material.")]
    [SerializeField] Texture albedo;
    [SerializeField] Vector2 tiling = Vector2.one;
    [SerializeField] Vector2 offset = Vector2.zero;

    [Header("Tint (optional)")]
    [SerializeField] bool overrideTint = false;
    [SerializeField] Color tint = Color.white;

    [Header("Multi-material meshes")]
    [Tooltip("Which material slot on the renderer to affect.")]
    [SerializeField, Min(0)] int materialIndex = 0;

    Renderer rend;
    MaterialPropertyBlock block;

    public Texture Albedo {
        get => albedo;
        set { albedo = value; Apply(); }
    }

    void OnEnable() => Apply();
    void OnDisable() => Clear();

    // Called in the editor whenever you change a value in the Inspector.
    void OnValidate() => Apply();

    public void Apply() {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) return;

        int slot = Mathf.Clamp(materialIndex, 0, Mathf.Max(0, rend.sharedMaterials.Length - 1));

        block ??= new MaterialPropertyBlock();
        rend.GetPropertyBlock(block, slot);   // keep any other overrides on this slot

        if (albedo != null) {
            block.SetTexture(BaseMapId, albedo);
            block.SetVector(BaseMapStId, new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        }

        if (overrideTint)
            block.SetColor(BaseColorId, tint);

        rend.SetPropertyBlock(block, slot);
    }

    /// <summary>Removes the override so the renderer falls back to the material.</summary>
    public void Clear() {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) return;

        int slot = Mathf.Clamp(materialIndex, 0, Mathf.Max(0, rend.sharedMaterials.Length - 1));
        rend.SetPropertyBlock(null, slot);
    }
}

#if UNITY_EDITOR
/// <summary>
/// After a texture is (re)imported, Unity recreates its GPU resource and a
/// MaterialPropertyBlock that still points at the old one renders black.
/// This re-applies every override once the import has finished.
/// </summary>
class PerRendererAlbedoReimportWatcher : UnityEditor.AssetPostprocessor {
    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        foreach (var path in imported) {
            var type = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type != null && typeof(Texture).IsAssignableFrom(type)) {
                UnityEditor.EditorApplication.delayCall -= RefreshAll;
                UnityEditor.EditorApplication.delayCall += RefreshAll;
                return;
            }
        }
    }

    static void RefreshAll() {
        foreach (var c in Object.FindObjectsByType<PerRendererTexture>(FindObjectsInactive.Exclude))
            c.Apply();

        UnityEditor.SceneView.RepaintAll();
    }
}
#endif
