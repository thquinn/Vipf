using UnityEngine;

public class XPortScript : MonoBehaviour {
    public HandheldScript handheldScript;
    public Vector3 posOffset;
    public Quaternion rotOffset;

    [HideInInspector] public XPakScript xPakScript;

    float t;

    void Update() {
        if (xPakScript != null) {
            t = Mathf.Clamp01(t + Time.deltaTime * 4);
            xPakScript.transform.localPosition = Vector3.Lerp(xPakScript.transform.localPosition, posOffset, t);
            xPakScript.transform.localRotation = Quaternion.Lerp(xPakScript.transform.localRotation, rotOffset, t);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (xPakScript != null) return;
        xPakScript = other.GetComponent<XPakScript>();
        if (xPakScript == null) return;
        if (xPakScript.subgameID != handheldScript.subgameID) {
            xPakScript = null;
            return;
        }
        PlayerScript.instance.ReleaseIfHolding(xPakScript);
        Collider collider = xPakScript.GetComponent<Collider>();
        Physics.IgnoreCollision(collider, handheldScript.GetComponent<Collider>());
        Physics.IgnoreCollision(collider, PlayerScript.instance.GetComponent<Collider>());
        xPakScript.GetComponent<Rigidbody>().isKinematic = true;
        xPakScript.gameObject.layer = 0;
        xPakScript.transform.parent = transform;
    }
}
