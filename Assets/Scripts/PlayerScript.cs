using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour {
    static float MOVE_SPEED = 5;
    static float MOVE_DECAY = 10;
    static float LOOK_SENSITIVITY = 0.2f * (Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 1);

    public Transform transformHead;
    public Camera cam;
    public Rigidbody rb;
    
    InputSystem_Actions input;

    void Start() {
        input = new InputSystem_Actions();
        input.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update() {
        UpdateLook();
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

}
