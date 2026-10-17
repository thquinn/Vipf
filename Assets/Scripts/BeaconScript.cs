using System.Collections.Generic;
using UnityEngine;

public class BeaconScript : MonoBehaviour {
    static Color COLOR_SUPERGREEN = new Color(3, 16, 0);

    public MeshRenderer mrLED;
    public SubgameID subgame;
    public int score;
    public SubgameEvent subgameEvent;

    HashSet<HandheldScript> handheldsInRange;
    float subgameEventTimer;
    MaterialPropertyBlock block;

    void Start() {
        handheldsInRange = new();
        if (subgameEvent != SubgameEvent.None) {
            SubgameBaseScript.OnSubgameEvent += HandleSubgameEvent;
        }
        block = new MaterialPropertyBlock();
    }

    void Update() {
        mrLED.GetPropertyBlock(block);
        bool on = handheldsInRange.Count > 0;
        subgameEventTimer -= Time.deltaTime;
        // Blink on and off.
        if (Activated() && (Time.time % 0.5f) < 0.25f) on = false;
        block.SetColor("_EmissionColor", on ? COLOR_SUPERGREEN : Color.black);
        mrLED.SetPropertyBlock(block);
    }

    public bool Activated() {
        foreach (HandheldScript handheld in handheldsInRange) {
            bool eventOK = subgameEvent == SubgameEvent.None || subgameEventTimer > 0;
            if (handheld.subgame.score >= score && eventOK) {
                return true;
            }
        }
        return false;
    }
    void HandleSubgameEvent(SubgameEvent e, HandheldScript handheldScript) {
        if (e == subgameEvent && handheldsInRange.Contains(handheldScript)) {
            subgameEventTimer = 0.5f;
        }
    }

    private void OnTriggerEnter(Collider other) {
        HandheldScript handheldScript = other.GetComponent<HandheldScript>();
        if (handheldScript?.subgameID == subgame) {
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
