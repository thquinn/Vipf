using System.Collections.Generic;
using UnityEngine;

public class BeaconScript : MonoBehaviour {
    static Color COLOR_SUPERGREEN = new Color(3, 16, 0);

    public MeshRenderer mrLED;
    public SubgameID scoreGame;
    public int score;

    HashSet<HandheldScript> handheldsInRange;
    MaterialPropertyBlock block;

    void Start() {
        handheldsInRange = new();
        block = new MaterialPropertyBlock();
    }

    void Update() {
        mrLED.GetPropertyBlock(block);
        bool on = handheldsInRange.Count > 0;
        // Blink on and off.
        if (Activated() && (Time.time % 0.5f) < 0.25f) on = false;
        block.SetColor("_EmissionColor", on ? COLOR_SUPERGREEN : Color.black);
        mrLED.SetPropertyBlock(block);
    }

    public bool Activated() {
        foreach (HandheldScript handheld in handheldsInRange) {
            if (handheld.subgame.score >= score) {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other) {
        HandheldScript handheldScript = other.GetComponent<HandheldScript>();
        if (handheldScript?.subgameID == scoreGame) {
            handheldsInRange.Add(handheldScript);
        }
    }
    private void OnTriggerExit(Collider other) {
        HandheldScript handheldScript = other.GetComponent<HandheldScript>();
        if (handheldsInRange.Contains(handheldScript)) {
            handheldsInRange.Remove(handheldScript);
        }
    }
}
