using UnityEngine;

public class DisplayCaseScript : MonoBehaviour {
    public Transform transformGlassHinge;

    public BeaconScript beacon;
    public Quaternion openRotation;

    bool open;
    Quaternion vHinge;

    void Update() {
        if (beacon.Activated()) open = true;
        transformGlassHinge.localRotation = Util.SmoothDampQuaternion(transformGlassHinge.localRotation, open ? openRotation : Quaternion.identity, ref vHinge, 1f);
    }
}
