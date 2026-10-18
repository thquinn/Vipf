using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandheldScript : GrabbableScript {
    public Sprite[] spriteSubgameIcons;

    public SubgameBaseScript subgame;
    public Renderer screenRenderer;
    public SpriteRenderer srIcon;
    public Transform transformDPad, transformZButton, transformXButton;
    public AudioSource audioSourceBGM, audioSourceSFX;
    public XPortScript xPortScript;

    public SubgameID subgameID;
    RenderTexture rt;
    Quaternion vRotDPad;
    Vector3 vZButton, vXButton;
    float initialButtonY;

    void Start() {
        // Set up screen.
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

    void OnDestroy() {
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
