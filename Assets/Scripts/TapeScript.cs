using UnityEngine;

public class TapeScript : GrabbableScript {
    public AudioClip clip;
    public Vector3 posOffset;
    public Quaternion rotOffset;

    public override Vector3 GrabbedPositionOffset() { return posOffset; }
    public override Quaternion GrabbedRotationOffset() { return rotOffset; }
}
