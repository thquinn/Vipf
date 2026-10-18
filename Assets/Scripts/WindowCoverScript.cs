using UnityEngine;

public class WindowCoverScript : MonoBehaviour {
    public MeshRenderer mr;

    void Update() {
        mr.enabled = PlayerScript.instance.transform.position.z > transform.position.z;
    }
}
