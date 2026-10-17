using UnityEngine;

public class HandheldScript : MonoBehaviour {
    public Renderer screenRenderer;
    public GameBaseScript game;

    RenderTexture rt;

    void Start() {
        rt = new RenderTexture(120, 90, 24, RenderTextureFormat.ARGB32) {
            filterMode = FilterMode.Point,
            useMipMap = false
        };
        rt.Create();
        game.cam.targetTexture = rt;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        screenRenderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", rt);
        screenRenderer.SetPropertyBlock(block);
    }

    void Update() {
        
    }

    void OnDestroy() {
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
