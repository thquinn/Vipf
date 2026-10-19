using UnityEngine;

public class GrabbableScript : MonoBehaviour {
    public Vector3 posOffset;
    public Quaternion rotOffset = Quaternion.identity;

    public Vector3 GrabbedPositionOffset() { return posOffset; }
    public Quaternion GrabbedRotationOffset() { return rotOffset; }

    [HideInInspector] public bool isHeld, hasBeenHeld;
}
