using UnityEngine;

public class GrabbableScript : MonoBehaviour {
    [HideInInspector] public bool isHeld;

    public virtual Vector3 GrabbedPositionOffset() { return Vector3.zero; }
    public virtual Quaternion GrabbedRotationOffset() { return Quaternion.identity; }
}
