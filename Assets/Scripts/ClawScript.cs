using UnityEngine;

public class ClawScript : MonoBehaviour {
    public BeaconScript beacon;
    public Transform[] segmentTransforms;
    public float rotationMult, maxRotation;

    Quaternion[] startingRotations;
    Quaternion[] vRots;

    void Start() {
        startingRotations = new Quaternion[segmentTransforms.Length];
        for (int i = 0; i < segmentTransforms.Length; i++) {
            startingRotations[i] = segmentTransforms[i].localRotation;
        }
        vRots = new Quaternion[segmentTransforms.Length];
    }

    void FixedUpdate() {
        float rotation = beacon.GetTotalScore() * rotationMult;
        rotation = Mathf.Min(rotation, maxRotation);
        for (int i = 0; i < segmentTransforms.Length; i++) {
            Quaternion target = Quaternion.Euler(-rotation, 0, 0) * startingRotations[i];
            segmentTransforms[i].localRotation = Util.SmoothDampQuaternion(segmentTransforms[i].localRotation, target, ref vRots[i], 1f, Mathf.Infinity, Time.fixedDeltaTime);
        }
    }
}
