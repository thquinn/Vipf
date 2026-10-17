using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour {
    static float MOVE_SPEED = 5;
    static float MOVE_DECAY = 10;
    static float LOOK_SENSITIVITY = 0.2f * (Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 1);

    public Transform transformHead, transformHandheldAnchor;
    public Camera cam;
    public Rigidbody rb;
    public LayerMask layerMaskHandheld;
    
    InputSystem_Actions input;
    HandheldScript handheldScript;
    Vector3 handheldInitialPosition;
    Quaternion handheldInitialRotation;
    float handheldGrabAnimT;

    void Start() {
        input = new InputSystem_Actions();
        input.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
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
    }

    void UpdateGrab() {
        if (handheldScript != null) {
            handheldGrabAnimT = Mathf.Clamp01(handheldGrabAnimT + Time.deltaTime * 4f);
            float t = handheldGrabAnimT;
            handheldScript.transform.position = Vector3.Lerp(handheldInitialPosition, transformHandheldAnchor.transform.position, t);
            handheldScript.transform.rotation = Quaternion.Lerp(handheldInitialRotation, transformHandheldAnchor.transform.rotation, t);
        }

        // Grabbing and dropping.
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (handheldScript != null) {
            // Drop.
            handheldScript.GetComponent<Collider>().enabled = true;
            handheldScript.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward;
            handheldScript = null;
        } else {
            // Grab.
            handheldScript = Util.GetCameraCollider(cam, layerMaskHandheld)?.GetComponent<HandheldScript>();
            if (handheldScript != null) {
                handheldScript.GetComponent<Collider>().enabled = false;
                handheldInitialPosition = handheldScript.transform.position;
                handheldInitialRotation = handheldScript.transform.rotation;
                handheldGrabAnimT = 0;
            }
        }
    }
}
