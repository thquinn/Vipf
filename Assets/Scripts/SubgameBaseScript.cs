using JetBrains.Annotations;
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

    public AudioClip sfxGlitch;

    public Camera cam;
    public TMP_Text tmpScore, tmpGameOver;
    public GameObject gameOverContainer, startBlocker;

    [HideInInspector] public HandheldScript handheldScript;
    AudioSource audioSourceBGM;
    [HideInInspector] public int score;
    [HideInInspector] public bool isStartBlocked, isGameOver;
    float inputBlockTimer, gameOverTimer;
    Vector2? bgmLoopPoints;

    public void Init(HandheldScript handheldScript) {
        this.handheldScript = handheldScript;
        audioSourceBGM = handheldScript.audioSourceBGM;
        isStartBlocked = true;
    }

    void Update() {
        if (isStartBlocked) {
            foreach (Key key in ANY_KEY) {
                if (GetKeyDown(key)) {
                    isStartBlocked = false;
                    startBlocker.SetActive(false);
                    inputBlockTimer = 0.25f;
                    Glitch();
                    break;
                }
            }
            return;
        }
        tmpScore.gameObject.SetActive(!isGameOver);
        tmpScore.text = score.ToString();
        gameOverContainer.SetActive(isGameOver);
        if (handheldScript.isHeld && Keyboard.current.f1Key.wasPressedThisFrame) {
            AddPoints(1);
        }
        inputBlockTimer -= Time.deltaTime;
        gameOverTimer -= Time.deltaTime;
        if (bgmLoopPoints.HasValue) {
            if (audioSourceBGM.isPlaying && audioSourceBGM.time > bgmLoopPoints.Value.y) {
                audioSourceBGM.time = bgmLoopPoints.Value.x;
            }
        }
    }

    public void AddPoints(int amount) {
        score += amount;
    }

    public void GameOver() {
        isGameOver = true;
        score = 0;
        gameOverTimer = 1;
        handheldScript.PlaySFXGameOver();
    }
    public bool ShouldRestart() {
        if (!isGameOver) return false;
        if (gameOverTimer > 0) return false;
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
        if (inputBlockTimer > 0) return false;
        if (!handheldScript.isHeld) return false;
        bool ret = Keyboard.current[key].isPressed;
        if (ret) PlayerScript.instance.UnduckGrabbed();
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
        if (!GetKey(key)) {
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
        if (handheldScript.audioSourceBGM.clip == bgm && handheldScript.audioSourceBGM.isPlaying) {
            return;
        }
        audioSourceBGM.clip = bgm;
        audioSourceBGM.Play();
    }
    public void StopBGM() {
        audioSourceBGM.Stop();
    }
    public void SetBGMLoopPoints(Vector2 loopPoints) {
        bgmLoopPoints = loopPoints;
    }
    public void PlaySFX(AudioClip sfx) {
        handheldScript.audioSourceSFX.PlayOneShot(sfx);
    }
    public void Glitch() {
        handheldScript.Glitch();
        PlaySFX(sfxGlitch);
    }

    public void EmitEvent(SubgameEvent e) {
        OnSubgameEvent?.Invoke(e, handheldScript);
    }

    public bool HasXPak() {
        return handheldScript.HasXPak();
    }
}

public enum SubgameID {
    None, Rhythm, Tetris, Pong, Jumper, Tictactic
}
public enum SubgameEvent {
    None, PongZX, TetrisTetris, TetrisXClear, TetrisPentris, JumperScoreThreshold
}