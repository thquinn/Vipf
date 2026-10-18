using System.Xml.Serialization;
using UnityEngine;
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
    public GrabbableScript grabbedGrabbable;
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
            // Drop.
            grabbedGrabbable.GetComponent<Rigidbody>().isKinematic = false;
            grabbedGrabbable.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 2;
            Release();
        } else {
            // Grab.
            grabbedGrabbable = Util.GetCameraCollider(cam, layerMaskGrab, 1.5f)?.GetComponent<GrabbableScript>();
            if (grabbedGrabbable != null) {
                Rigidbody handheldRB = grabbedGrabbable.GetComponent<Rigidbody>();
                handheldRB.linearVelocity = Vector3.zero;
                handheldRB.angularVelocity = Vector3.zero;
                handheldRB.isKinematic = true;
                handheldInitialPosition = grabbedGrabbable.transform.position;
                handheldInitialRotation = grabbedGrabbable.transform.rotation;
                handheldGrabAnimT = 0;
                grabbedGrabbable.isHeld = true;
            }
        }
    }
    public void Release() {
        grabbedGrabbable.isHeld = false;
        grabbedGrabbable = null;
    }
}
