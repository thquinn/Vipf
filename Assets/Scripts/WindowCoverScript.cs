using UnityEngine;

public class WindowCoverScript : MonoBehaviour {
    public MeshRenderer mr;
    public bool reverseChildren;

    void Update() {
        bool beyond = PlayerScript.instance.transform.position.z > transform.position.z;
        if (mr != null) {
            mr.enabled = beyond;
        }
        if (reverseChildren) {
            foreach (Transform child in transform) {
                child.gameObject.SetActive(!beyond);
            }
        }
    }
}
