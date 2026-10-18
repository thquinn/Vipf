using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ShutterScript : MonoBehaviour {
    public BeaconScript beaconScript;
    public SplineContainer splineContainer;
    public Collider collidre;
    public GameObject slat;
    public int maxSlats;
    public float closedStart, closedEnd, openStart, openEnd;
    public float dampTime;

    List<GameObject> slats;
    float startT, endT, startV, endV;
    bool isOpen;

    void Start() {
        slats = new();
        slats.Add(slat);
        startT = closedStart;
        endT = closedEnd;
    }

    void Update() {
        if (slats.Count < maxSlats) slats.Add(Instantiate(slat, transform));
        if (beaconScript.Activated()) isOpen = true;
        collidre.enabled = !isOpen;

        startT = Mathf.SmoothDamp(startT, isOpen ? openStart : closedStart, ref startV, dampTime);
        endT = Mathf.SmoothDamp(endT, isOpen ? openEnd : closedEnd, ref endV, dampTime);
        for (int i = 0; i < slats.Count; i++) {
            float t = (float)i / (slats.Count - 1);
            t = Mathf.Lerp(startT, endT, t);
            t = Mathf.Lerp(0.001f, 0.999f, t);
            splineContainer.Evaluate(t, out var pos, out var tangent, out var up);
            slats[i].transform.SetPositionAndRotation(pos, Quaternion.LookRotation(tangent, up));
        }
    }
}
