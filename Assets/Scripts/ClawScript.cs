using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClawScript : MonoBehaviour {
    static float SCORE_WINDOW = 2;

    public BeaconScript beacon;
    public AudioSource audioSource;
    public Transform[] segmentTransforms;
    public float rotationMult, maxRotation;
    public float volumeMult;

    Quaternion[] startingRotations;
    Quaternion[] vRots;
    Queue<Vector2> timesAndTotals;

    void Start() {
        startingRotations = new Quaternion[segmentTransforms.Length];
        for (int i = 0; i < segmentTransforms.Length; i++) {
            startingRotations[i] = segmentTransforms[i].localRotation;
        }
        vRots = new Quaternion[segmentTransforms.Length];
        timesAndTotals = new Queue<Vector2>();
    }

    void FixedUpdate() {
        timesAndTotals.Enqueue(new Vector2(Time.time, beacon.GetTotalScore()));
        while (timesAndTotals.Peek().x < Time.time - SCORE_WINDOW) {
            timesAndTotals.Dequeue();
        }
        float totalScore = timesAndTotals.Count == 0 ? 0 : timesAndTotals.Max(t => t.y);
        float rotation = totalScore * rotationMult;
        rotation = Mathf.Min(rotation, maxRotation);
        Quaternion before = segmentTransforms[0].localRotation;
        for (int i = 0; i < segmentTransforms.Length; i++) {
            Quaternion target = Quaternion.Euler(-rotation, 0, 0) * startingRotations[i];
            segmentTransforms[i].localRotation = Util.SmoothDampQuaternion(segmentTransforms[i].localRotation, target, ref vRots[i], 1f, Mathf.Infinity, Time.fixedDeltaTime);
        }
        float angularSpeed = Util.SmallAngle(before, segmentTransforms[0].localRotation) / Time.fixedDeltaTime;
        audioSource.volume = angularSpeed * volumeMult;
    }
}
