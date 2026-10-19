using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeaconScript : MonoBehaviour {
    public static float SUBGAME_EVENT_CLIP_TIME_RADIUS = 2;

    static Color COLOR_SUPERGREEN = new Color(3, 16, 0);
    static Color COLOR_SUPERPURPLE = new Color(10, 0, 10);

    public MeshRenderer mrLED;
    public SubgameID subgame;
    public int score, totalScore;
    public SubgameEvent subgameEvent;
    public AudioClip subgameEventClip;
    public float subgameEventClipTime;
    public bool requireXPak;
    public BroadcastType broadcastType;
    public bool broadcasting;

    HashSet<HandheldScript> handheldsInRange;
    float subgameEventTimer, broadcastSuccessTimer;
    MaterialPropertyBlock block;

    void Start() {
        handheldsInRange = new();
        if (subgameEvent != SubgameEvent.None) {
            SubgameBaseScript.OnSubgameEvent += HandleSubgameEvent;
        }
        block = new MaterialPropertyBlock();
    }

    public virtual void Update() {
        Color ledColor = broadcasting ? COLOR_SUPERPURPLE : COLOR_SUPERGREEN;
        mrLED.GetPropertyBlock(block);
        bool on = handheldsInRange.Count > (totalScore > 0 ? 1 : 0);
        subgameEventTimer -= Time.deltaTime;
        broadcastSuccessTimer -= Time.deltaTime;
        // Blink on and off.
        if (Activated() && (Time.time % 0.5f) < 0.25f) on = false;
        if ((BroadcastCheck() || broadcastSuccessTimer > 0  ) && (Time.time % 0.5f) < 0.25f) on = false;
        block.SetColor("_EmissionColor", on ? ledColor : Color.black);
        mrLED.SetPropertyBlock(block);
    }

    public virtual bool Activated() {
        if (Keyboard.current.f2Key.wasPressedThisFrame) return true;
        if (broadcasting) return false;
        int scoreTally = 0, countWithScore = 0;
        foreach (HandheldScript handheld in handheldsInRange) {
            bool eventOK = subgameEvent == SubgameEvent.None || subgameEventTimer > 0;
            if (handheld.subgame.score >= score && eventOK) {
                if (broadcastType != BroadcastType.None) {
                    Invoke("SwitchToBroadcast", 3);
                }
                return true;
            }
            // Total score.
            if (handheld.subgame.score > 0) {
                scoreTally += handheld.subgame.score;
                countWithScore++;
            }
        }
        if (totalScore > 0 && scoreTally >= totalScore && countWithScore >= 2) {
            return true;
        }
        return false;
    }
    void HandleSubgameEvent(SubgameEvent e, HandheldScript handheldScript) {
        if (requireXPak && !handheldScript.HasXPak()) return;
        if (subgameEventClip != null) {
            TapePlayerScript tapePlayer = TapePlayerScript.instance;
            if (!tapePlayer.audioSource.isPlaying) return;
            if (tapePlayer.audioSource.clip != subgameEventClip) return;
            float timeDelta = Mathf.Abs(tapePlayer.audioSource.time - subgameEventClipTime);
            if (timeDelta > SUBGAME_EVENT_CLIP_TIME_RADIUS) return;
        }
        if (e == subgameEvent && handheldsInRange.Contains(handheldScript)) {
            subgameEventTimer = 0.5f;
        }
    }

    public int GetTotalScore() {
        return handheldsInRange.Sum(h => h.subgame.score);
    }

    private void OnTriggerEnter(Collider other) {
        HandheldScript handheldScript = other.GetComponent<HandheldScript>();
        if (handheldScript == null) return;
        if (requireXPak && !handheldScript.HasXPak()) return;
        if (handheldScript?.subgameID == subgame || totalScore > 0) {
            handheldsInRange.Add(handheldScript);
        }
    }
    private void OnTriggerExit(Collider other) {
        HandheldScript handheldScript = other.GetComponent<HandheldScript>();
        if (handheldScript == null) return;
        if (handheldsInRange.Contains(handheldScript)) {
            handheldsInRange.Remove(handheldScript);
        }
    }

    void SwitchToBroadcast() {
        broadcasting = true;
    }
    bool BroadcastCheck() {
        if (!broadcasting) return false;
        bool result = false;
        foreach (HandheldScript handheld in handheldsInRange) {
            if (broadcastType == BroadcastType.TetrisX) result |= TetrisX(handheld.subgame.GetComponent<Subgame02Script>());
        }
        if (result) broadcastSuccessTimer = 1;
        return result;
    }
    bool TetrisX(Subgame02Script subgame02) {
        return subgame02.ReceiveBroadcastTetrisX();
    }
}

public enum BroadcastType {
    None, TetrisX, TetrisLongI
}