using UnityEngine;
using UnityEngine.InputSystem;

public class SubgameBaseScript : MonoBehaviour {
    public Camera cam;

    HandheldScript handheldScript;

    void Start() {
        
    }
    public void Init(HandheldScript handheldScript) {
        this.handheldScript = handheldScript;
    }

    void Update() {
    }

    public bool GetKey(Key key) {
        if (!handheldScript.isHeld) return false;
        return Keyboard.current[key].isPressed;
    }
    public bool GetKeyDown(Key key) {
        if (!handheldScript.isHeld) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }
}