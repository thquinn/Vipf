using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandheldScript : GrabbableScript {
    public Sprite[] spriteSubgameIcons;

    public AudioClip sfxGameOver;

    public SubgameBaseScript subgame;
    public Renderer screenRenderer;
    public SpriteRenderer srIcon;
    public Transform transformDPad, transformZButton, transformXButton;
    public AudioSource audioSourceBGM, audioSourceSFX;
    public XPortScript xPortScript;

    MaterialPropertyBlock block;
    public SubgameID subgameID;
    RenderTexture rt;
    Quaternion vRotDPad;
    Vector3 vZButton, vXButton;
    float initialButtonY;
    float glitch, vGlitch;

    void Start() {
        // Set up screen.
        subgame.Init(this);
        rt = new RenderTexture(240, 180, 24, RenderTextureFormat.ARGB32) {
            filterMode = FilterMode.Point,
            useMipMap = false,
            wrapMode = TextureWrapMode.Repeat,
        };
        rt.Create();
        subgame.cam.gameObject.SetActive(true);
        subgame.cam.targetTexture = rt;
        block = new MaterialPropertyBlock();
        screenRenderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", rt);
        screenRenderer.SetPropertyBlock(block);
        initialButtonY = transformZButton.localPosition.y;

        // Other stuff.
        if (subgameID != SubgameID.None) {
            srIcon.sprite = spriteSubgameIcons.First(s => s.name == $"icon_{subgameID}".ToLower());
        }
    }

    void Update() {
        transformDPad.localRotation = Util.SmoothDampQuaternion(transformDPad.localRotation, GetTargetDPadRotation(), ref vRotDPad, 0.05f);
        float magnitude = .003f;
        Vector3 targetZPosition = transformZButton.localPosition;
        targetZPosition.y = initialButtonY - (Keyboard.current.zKey.isPressed ? magnitude : 0);
        transformZButton.localPosition = Vector3.SmoothDamp(transformZButton.localPosition, targetZPosition, ref vZButton, 0.05f);
        Vector3 targetXPosition = transformXButton.localPosition;
        targetXPosition.y = initialButtonY - (Keyboard.current.xKey.isPressed ? magnitude : 0);
        transformXButton.localPosition = Vector3.SmoothDamp(transformXButton.localPosition, targetXPosition, ref vXButton, 0.05f);
        // Glitch.
        glitch = Mathf.SmoothDamp(glitch, 0, ref vGlitch, 0.04f);
        screenRenderer.GetPropertyBlock(block);
        block.SetFloat("_Glitch", glitch);
        screenRenderer.SetPropertyBlock(block);
    }

    Quaternion GetTargetDPadRotation() {
        if (!isHeld) return Quaternion.identity;
        float magnitude = 5;
        float x = 0, z = 0;
        if (Keyboard.current.leftArrowKey.isPressed) {
            x = magnitude;
        } else if (Keyboard.current.rightArrowKey.isPressed) {
            x = -magnitude;
        }
        if (Keyboard.current.downArrowKey.isPressed) {
            z = magnitude;
        } else if (Keyboard.current.upArrowKey.isPressed) {
            z = -magnitude;
        }
        return Quaternion.Euler(x, 0, z);
    }

    public void PlaySFXGameOver() {
        audioSourceSFX.PlayOneShot(sfxGameOver);
    }
    public void Glitch() {
        glitch = 1;
        vGlitch = 0;
    }

    public bool HasXPak() {
        return xPortScript.xPakScript != null;
    }

    void OnDestroy() {
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
