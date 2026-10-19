using UnityEngine;

public class DisplayCaseScript : MonoBehaviour {
    public AudioClip sfxOpen;

    public Transform transformGlassHinge;
    public AudioSource audioSource;

    public BeaconScript beacon;
    public Quaternion openRotation;

    bool open;
    Quaternion vHinge;

    void Update() {
        if (!open && beacon.Activated()) {
            open = true;
            if (sfxOpen != null) audioSource.PlayOneShot(sfxOpen);
        }
        transformGlassHinge.localRotation = Util.SmoothDampQuaternion(transformGlassHinge.localRotation, open ? openRotation : Quaternion.identity, ref vHinge, 1f);
    }
}
