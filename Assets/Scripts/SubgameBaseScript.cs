using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class SubgameBaseScript : MonoBehaviour {
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
    public bool GetKeyDown(Key key) {
        Assert.IsTrue(ANY_KEY.Contains(key));
        if (!handheldScript.isHeld) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }
    
    public void PlayBGM(AudioClip bgm) {
        handheldScript.audioSourceBGM.clip = bgm;
        handheldScript.audioSourceBGM.Play();
    }
    public void PlaySFX(AudioClip sfx) {
        handheldScript.audioSourceBGM.PlayOneShot(sfx);
    }
}

public enum SubgameID {
    None, Rhythm
}