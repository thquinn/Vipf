using UnityEngine;

public class LockScript : BeaconScript {
    public MeshRenderer[] mrs;

    bool unlocked;

    public override bool Activated() {
        return unlocked;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.name == "Key") {
            unlocked = true;
            Destroy(other.gameObject);
            foreach (MeshRenderer mr in mrs) {
                mr.enabled = false;
            }
        }
    }

    public override void Update() { }
}
