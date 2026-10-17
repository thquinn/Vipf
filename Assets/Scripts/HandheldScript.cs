using UnityEngine;

public class HandheldScript : MonoBehaviour {
    public Renderer screenRenderer;
    public SubgameBaseScript subgame;

    [HideInInspector] public bool isHeld;
    RenderTexture rt;

    void Start() {
        subgame.Init(this);
        rt = new RenderTexture(240, 180, 24, RenderTextureFormat.ARGB32) {
            filterMode = FilterMode.Point,
            useMipMap = false
        };
        rt.Create();
        subgame.cam.targetTexture = rt;

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
