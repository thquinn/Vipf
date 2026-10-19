using UnityEngine;

public class CashRegisterScript : MonoBehaviour {
    public AudioClip sfxOpen;

    public Transform transformDrawer;
    public BeaconScript beacon;
    public AudioSource audioSource;
    public Vector3 openDrawerPosition;

    bool open;
    Vector3 vDrawer;

    void FixedUpdate() {
        if (!open && beacon.Activated()) {
            open = true;
            audioSource.PlayOneShot(sfxOpen);
        }
        transformDrawer.localPosition = Vector3.SmoothDamp(transformDrawer.localPosition, open ? openDrawerPosition : Vector3.zero, ref vDrawer, 1f);
    }
}
