using UnityEngine;

public class CashRegisterScript : MonoBehaviour {
    public Transform transformDrawer;
    public BeaconScript beacon;
    public Vector3 openDrawerPosition;

    bool open;
    Vector3 vDrawer;

    void Update() {
        if (beacon.Activated()) open = true;
        transformDrawer.localPosition = Vector3.SmoothDamp(transformDrawer.localPosition, open ? openDrawerPosition : Vector3.zero, ref vDrawer, 1f);
    }
}
