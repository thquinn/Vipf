using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class SubgameBaseScript : MonoBehaviour {
    public static event Action<SubgameEvent, HandheldScript> OnSubgameEvent;

    static Key[] ANY_KEY = new Key[] { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.Z, Key.X };

    public Camera cam;
    public TMP_Text tmpScore, tmpGameOver;
    public GameObject gameOverContainer;

    [HideInInspector] public HandheldScript handheldScript;
    [HideInInspector] public int score;
    [HideInInspector] public bool isGameOver;

    void Start() {
        
    }
    public void Init(HandheldScript handheldScript) {
        this.handheldScript = handheldScript;
    }

    void Update() {
        tmpScore.gameObject.SetActive(!isGameOver);
        tmpScore.text = score.ToString();
        gameOverContainer.SetActive(isGameOver);
        if (handheldScript.isHeld && Keyboard.current.f1Key.wasPressedThisFrame) {
            AddPoints(1);
        }
    }

    public void AddPoints(int amount) {
        score += amount;
    }

    public void GameOver() {
        isGameOver = true;
        score = 0;
    }
    public bool ShouldRestart() {
        if (!isGameOver) return false;
        foreach (Key key in ANY_KEY) {
            if (GetKeyDown(key)) {
                isGameOver = false;
                return true;
            }
        }
        return false;
    }

    public bool GetKey(Key key) {
        Assert.IsTrue(ANY_KEY.Contains(key));
        if (!handheldScript.isHeld) return false;
        return Keyboard.current[key].isPressed;
    }
    Dictionary<Key, float> keyPressTimes = new();
    static float KEY_REPEAT_DELAY = 0.5f;
    static float KEY_REPEAT_TIME = 0.066f;
    public bool GetKeyDown(Key key) {
        Assert.IsTrue(ANY_KEY.Contains(key));
        if (!handheldScript.isHeld) {
            keyPressTimes.Remove(key);
            return false;
        }
        if (!Keyboard.current[key].isPressed) {
            keyPressTimes.Remove(key);
            return false;
        }
        if (Keyboard.current[key].wasPressedThisFrame) {
            keyPressTimes[key] = Time.unscaledTime;
        }
        if (!keyPressTimes.TryGetValue(key, out float pressTime)) {
            return false;
        }
        float oldT = Time.unscaledTime - pressTime;
        float newT = oldT + Time.unscaledDeltaTime;
        if (oldT == 0) return true;
        if (newT < KEY_REPEAT_DELAY) return false;
        if (oldT < KEY_REPEAT_DELAY) return true;
        return Mathf.FloorToInt((oldT - KEY_REPEAT_DELAY) / KEY_REPEAT_TIME)
             < Mathf.FloorToInt((newT - KEY_REPEAT_DELAY) / KEY_REPEAT_TIME);
    }

    public void PlayBGM(AudioClip bgm) {
        handheldScript.audioSourceBGM.clip = bgm;
        handheldScript.audioSourceBGM.Play();
    }
    public void PlaySFX(AudioClip sfx) {
        handheldScript.audioSourceBGM.PlayOneShot(sfx);
    }

    public void EmitEvent(SubgameEvent e) {
        OnSubgameEvent?.Invoke(e, handheldScript);
    }

    public bool HasXPak() {
        return handheldScript.HasXPak();
    }
}

public enum SubgameID {
    None, Rhythm, Tetris, Pong
}
public enum SubgameEvent {
    None, PongZX, TetrisTetris, TetrisXClear, TetrisPentris
}