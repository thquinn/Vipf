using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour {
    public static PlayerScript instance;

    static float MOVE_SPEED = 5;
    static float MOVE_DECAY = 10;
    static float LOOK_SENSITIVITY = 0.2f * (Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 1);

    public Transform transformHead, transformHandheldAnchor;
    public Camera cam;
    public Rigidbody rb;
    public LayerMask layerMaskGrab;
    
    InputSystem_Actions input;
    Vector3 initialHeadPosition, vHead;
    public HandheldScript grabbedHandheld;
    Vector3 handheldInitialPosition;
    Quaternion handheldInitialRotation;
    float handheldGrabAnimT;

    void Start() {
        instance = this;
        input = new InputSystem_Actions();
        input.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        initialHeadPosition = transformHead.localPosition;
    }

    void Update() {
        UpdateLook();
    }
    void LateUpdate() {
        UpdateGrab();
    }
    void FixedUpdate() {
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
    }
    void UpdateVelocity() {
        if (rb.isKinematic) return;
        Vector2 move = input.Player.Move.ReadValue<Vector2>();
        Vector3 inputVelocity = transformHead.forward * move.y * MOVE_SPEED + transformHead.right * move.x * MOVE_SPEED;
        float t = 1f - Mathf.Exp(-MOVE_DECAY * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(
            Mathf.Lerp(rb.linearVelocity.x, inputVelocity.x, t),
            0,
            Mathf.Lerp(rb.linearVelocity.z, inputVelocity.z, t)
        );
        Vector3 targetHeadPosition = Keyboard.current.leftCtrlKey.isPressed ? initialHeadPosition / 2 : initialHeadPosition;
        transformHead.localPosition = Vector3.SmoothDamp(transformHead.localPosition, targetHeadPosition, ref vHead, 0.1f);
    }

    void UpdateGrab() {
        if (grabbedHandheld != null) {
            handheldGrabAnimT = Mathf.Clamp01(handheldGrabAnimT + Time.deltaTime * 6f);
            float t = handheldGrabAnimT;
            grabbedHandheld.transform.position = Vector3.Lerp(handheldInitialPosition, transformHandheldAnchor.transform.position, t);
            grabbedHandheld.transform.rotation = Quaternion.Lerp(handheldInitialRotation, transformHandheldAnchor.transform.rotation, t);
        }

        // Grabbing and dropping.
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (grabbedHandheld != null) {
            // Drop.
            grabbedHandheld.GetComponent<Rigidbody>().isKinematic = false;
            grabbedHandheld.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 2;
            grabbedHandheld.isHeld = false;
            grabbedHandheld = null;
        } else {
            // Grab.
            grabbedHandheld = Util.GetCameraCollider(cam, layerMaskGrab, 1.5f)?.GetComponent<HandheldScript>();
            if (grabbedHandheld != null) {
                Rigidbody handheldRB = grabbedHandheld.GetComponent<Rigidbody>();
                handheldRB.linearVelocity = Vector3.zero;
                handheldRB.angularVelocity = Vector3.zero;
                handheldRB.isKinematic = true;
                handheldInitialPosition = grabbedHandheld.transform.position;
                handheldInitialRotation = grabbedHandheld.transform.rotation;
                handheldGrabAnimT = 0;
                grabbedHandheld.isHeld = true;
            }
        }
    }
}
