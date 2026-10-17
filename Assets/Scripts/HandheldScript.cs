using UnityEngine;

public class HandheldScript : MonoBehaviour {
    public SubgameBaseScript subgame;
    public Renderer screenRenderer;
    public AudioSource audioSourceBGM, audioSourceSFX;

    [HideInInspector] public bool isHeld;
    RenderTexture rt;

    void Start() {
        subgame.Init(this);
        rt = new RenderTexture(240, 180, 24, RenderTextureFormat.ARGB32) {
            filterMode = FilterMode.Point,
            useMipMap = false
        };
        rt.Create();
        subgame.cam.gameObject.SetActive(true);
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
