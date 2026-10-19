using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour {
    public static PlayerScript instance;

    public AudioClip[] sfxSteps;

    static float MOVE_SPEED = 5;
    static float MOVE_DECAY = 10;
    static float LOOK_SENSITIVITY = 0.2f * (Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 1);
    static Quaternion SWIVEL_DOWN_POSITION = Quaternion.Euler(30, 0, 0);
    static float NO_DUCK_TIME = 2;
    static float VIGNETTE_MAX_INTENSITY = 0.6f;
    static Vector3 HEAD_GROUND_POSITION = new Vector3(-0.2f, 0.4f, 1f);
    static Quaternion HEAD_GROUND_ROTATION = Quaternion.Euler(0, 0, 90);
    static float STEP_DISTANCE = 3f;

    public Transform transformHead, transformHandheldAnchor, transformHandheldSwivel;
    public Camera cam;
    public Rigidbody rb;
    public LayerMask layerMaskGrab;
    public Volume globalVolume;
    public Image wipeImage;
    public AudioSource audioSource;
    
    InputSystem_Actions input;
    Vector3 initialHeadPosition, vHead;
    [HideInInspector] public GrabbableScript grabbedGrabbable;
    Vector3 handheldInitialPosition;
    Quaternion handheldInitialRotation;
    float handheldGrabAnimT;
    [HideInInspector] public bool duckGrabbable; // swivel the grabbed object down while they're trying to move or look
    Quaternion vSwivelRot, vHeadRot;
    float noDuckTimer;
    CutsceneState cutsceneState;
    float cutsceneTimer, wipe, vWipe, initialVignetteIntensity, vignetteIntensity, vVignette;
    Vignette vignette;
    Vector3 lastPosition;
    float stepDistance;

    void Start() {
        instance = this;
        input = new InputSystem_Actions();
        input.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        initialHeadPosition = transformHead.localPosition;
        if (!globalVolume.profile.TryGet(out vignette)) {
            Debug.Log("Failed to get vignette.");
        }
        initialVignetteIntensity = vignette.intensity.value;
        if (!Application.isEditor) {
            transformHead.localPosition = HEAD_GROUND_POSITION;
            transformHead.localRotation = HEAD_GROUND_ROTATION;
            wipe = 1;
            vignetteIntensity = VIGNETTE_MAX_INTENSITY;
            cutsceneState = CutsceneState.IntroRise;
            cutsceneTimer = 2;
        }
        lastPosition = transformHead.position;
    }

    void Update() {
        // Cutscenes.
        wipeImage.SetAlpha(wipe);
        vignette.intensity.value = vignetteIntensity;
        if (cutsceneTimer > 0) {
            cutsceneTimer -= Time.deltaTime;
        } else if (cutsceneState == CutsceneState.IntroRise) {
            transformHead.localPosition = Vector3.SmoothDamp(transformHead.localPosition, initialHeadPosition, ref vHead, 2f);
            transformHead.localRotation = Util.SmoothDampQuaternion(transformHead.localRotation, Quaternion.identity, ref vHeadRot, 2f);
            wipe = Mathf.SmoothDamp(wipe, 0, ref vWipe, 2f);
            vignetteIntensity = Mathf.SmoothDamp(vignetteIntensity, initialVignetteIntensity, ref vVignette, 2f);
            if (wipe < 0.01f) {
                transformHead.localRotation = Quaternion.identity;
                wipe = 0;
                vignetteIntensity = initialVignetteIntensity;
                cutsceneState = CutsceneState.None;
            }
        } else if (cutsceneState == CutsceneState.OutroVignette) {
            vignetteIntensity = Mathf.SmoothDamp(vignetteIntensity, VIGNETTE_MAX_INTENSITY, ref vVignette, 4f);
        } else if (cutsceneState == CutsceneState.OutroFall) {
            transformHead.localPosition = Vector3.SmoothDamp(transformHead.localPosition, HEAD_GROUND_POSITION, ref vHead, 3f);
            transformHead.localRotation = Util.SmoothDampQuaternion(transformHead.localRotation, HEAD_GROUND_ROTATION, ref vHeadRot, 3f);
            wipe = Mathf.SmoothDamp(wipe, 1, ref vWipe, 2f);
            if (wipe > .999f) Application.Quit();
        }

        if (ControlsLocked()) return;
        UpdateLook();
        // Swiveling the grabbable down.
        noDuckTimer -= Time.deltaTime;
        if (grabbedGrabbable == null) duckGrabbable = false;
        transformHandheldSwivel.localRotation = Util.SmoothDampQuaternion(
            transformHandheldSwivel.localRotation,
            duckGrabbable ? SWIVEL_DOWN_POSITION : Quaternion.identity,
            ref vSwivelRot,
            0.2f
        );
        // Footsteps.
        Vector3 delta = lastPosition - transform.position;
        stepDistance += new Vector2(delta.x, delta.z).magnitude;
        if (stepDistance >= STEP_DISTANCE) {
            audioSource.PlayOneShot(sfxSteps[Random.Range(0, sfxSteps.Length)]);
            stepDistance = stepDistance % STEP_DISTANCE;
        }
        lastPosition = transform.position;
    }
    void LateUpdate() {
        if (ControlsLocked()) return;
        UpdateGrab();
    }
    void FixedUpdate() {
        if (ControlsLocked()) return;
        UpdateVelocity();
    }

    void UpdateLook() {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * LOOK_SENSITIVITY;
        transformHead.Rotate(0, mouseDelta.x, 0);
        float thetaX = cam.transform.localRotation.eulerAngles.x;
        if (thetaX > 180) {
            thetaX -= 360;
        }
        thetaX = Mathf.Clamp(thetaX - mouseDelta.y, -90, 90);
        cam.transform.localRotation = Quaternion.Euler(thetaX, 0, 0);
        if (mouseDelta != Vector2.zero) DuckGrabbed();
    }
    void UpdateVelocity() {
        if (rb.isKinematic) return;
        Vector2 move = input.Player.Move.ReadValue<Vector2>();
        Vector3 inputVelocity = transformHead.forward * move.y * MOVE_SPEED + transformHead.right * move.x * MOVE_SPEED;
        if (inputVelocity != Vector3.zero) DuckGrabbed();
        if (Keyboard.current.leftShiftKey.isPressed) inputVelocity *= 2;
        float t = 1f - Mathf.Exp(-MOVE_DECAY * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(
            Mathf.Lerp(rb.linearVelocity.x, inputVelocity.x, t),
            0,
            Mathf.Lerp(rb.linearVelocity.z, inputVelocity.z, t)
        );
        if (rb.linearVelocity.sqrMagnitude < .5f) stepDistance = 0;
        Vector3 targetHeadPosition = Keyboard.current.leftCtrlKey.isPressed ? initialHeadPosition * .66f : initialHeadPosition;
        transformHead.localPosition = Vector3.SmoothDamp(transformHead.localPosition, targetHeadPosition, ref vHead, 0.2f);
    }

    void UpdateGrab() {
        if (grabbedGrabbable != null) {
            handheldGrabAnimT = Mathf.Clamp01(handheldGrabAnimT + Time.deltaTime * 6f);
            float t = handheldGrabAnimT;
            Vector3 targetPosition = transformHandheldAnchor.transform.position + transformHandheldAnchor.transform.rotation * grabbedGrabbable.GrabbedPositionOffset();
            grabbedGrabbable.transform.position = Vector3.Lerp(handheldInitialPosition, targetPosition, t);
            Quaternion targetRotation = transformHandheldAnchor.transform.rotation * grabbedGrabbable.GrabbedRotationOffset();
            grabbedGrabbable.transform.rotation = Quaternion.Lerp(handheldInitialRotation, targetRotation, t);
        }

        // Grabbing and dropping.
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (grabbedGrabbable != null) {
            if (duckGrabbable) {
                UnduckGrabbed();
            } else {
                Drop();
            }
        } else {
            // Grab.
            float dotUp = Vector3.Dot(cam.transform.forward, Vector3.up);
            float grabDistance = Mathf.Lerp(2.25f, 1, Mathf.Clamp01(dotUp));

            grabbedGrabbable = Util.GetCameraCollider(cam, layerMaskGrab, grabDistance)?.GetComponent<GrabbableScript>();
            if (grabbedGrabbable?.gameObject.layer == 0) grabbedGrabbable = null;
            if (grabbedGrabbable != null) {
                Rigidbody handheldRB = grabbedGrabbable.GetComponent<Rigidbody>();
                handheldRB.isKinematic = false;
                handheldRB.linearVelocity = Vector3.zero;
                handheldRB.angularVelocity = Vector3.zero;
                handheldRB.isKinematic = true;
                handheldInitialPosition = grabbedGrabbable.transform.position;
                handheldInitialRotation = grabbedGrabbable.transform.rotation;
                handheldGrabAnimT = 0;
                grabbedGrabbable.isHeld = true;
                grabbedGrabbable.hasBeenHeld = true;
                UnduckGrabbed();
            }
        }
    }
    void Drop() {
        if (grabbedGrabbable == null) return;
        grabbedGrabbable.GetComponent<Rigidbody>().isKinematic = false;
        grabbedGrabbable.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 2;
        Release();
    }
    void Release() {
        grabbedGrabbable.isHeld = false;
        grabbedGrabbable = null;
    }
    public void ReleaseIfHolding(GrabbableScript grabbable) {
        if (grabbedGrabbable == grabbable) Release();
    }

    void DuckGrabbed() {
        if (noDuckTimer > 0) return;
        duckGrabbable = true;
    }
    public void UnduckGrabbed() {
        duckGrabbable = false;
        noDuckTimer = NO_DUCK_TIME;
    }

    bool ControlsLocked() {
        return cutsceneState == CutsceneState.IntroRise || cutsceneState == CutsceneState.OutroFall;
    }
    public void TriggerOutroVignette() {
        if (cutsceneState >= CutsceneState.OutroVignette) return;
        cutsceneState = CutsceneState.OutroVignette;
    }
    public void TriggerOutroFall() {
        if (cutsceneState >= CutsceneState.OutroFall) return;
        Drop();
        cutsceneState = CutsceneState.OutroFall;
    }
}

enum CutsceneState {
    None, IntroRise, OutroVignette, OutroFall
}