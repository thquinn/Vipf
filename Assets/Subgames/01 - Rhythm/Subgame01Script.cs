using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Subgame01Script : MonoBehaviour {
    static float NO_BEAT = -999;
    static float BGM_TEMPO = 85;
    static double[] BEATS = new double[] {
        0, 2, 4, 5, 6,
        8, 10, 12, 13, 14,
        16, 17, 18, 19, 20, 22,
        24, 24.5, 25, 25.5, 26, 28, 29, 30,
    };

    public GameObject prefabGridCell;

    public Transform transformGrid, transformSelector;
    public AudioClip bgm;
    public AudioClip sfxClick;
    public double audioOffset;

    SubgameBaseScript subgame;

    Vector2Int fails;
    [HideInInspector] public Subgame01GridState[,] grid;
    Subgame01GridCellScript[,] cellScripts;
    Vector2Int selectorCoor;
    
    AudioSource audioSourceBGM;
    float initialBGMVolume;
    double loopLength, dspStartTime;
    double lastBeat;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        grid = new Subgame01GridState[10, 8];
        cellScripts = new Subgame01GridCellScript[10, 8];
        
        audioSourceBGM = subgame.handheldScript.audioSourceBGM;
        initialBGMVolume = audioSourceBGM.volume;
        audioSourceBGM.clip = bgm;
        loopLength = (double)bgm.samples / bgm.frequency;
        Restart();
    }

    void Update() {
        UpdateSelector();
        UpdateBeats();
        UpdateGrid();
        
        if (subgame.isGameOver) {
            audioSourceBGM.volume -= Time.deltaTime * 0.5f;
        }
        if (audioSourceBGM.volume <= 0 && subgame.ShouldRestart()) {
            Restart();
        }
    }

    void InitGrid() {
        Array.Clear(grid, 0, grid.Length);
        grid[0, 0] = Subgame01GridState.Empty;
        grid[3, 2] = Subgame01GridState.Empty;
        grid[4, 2] = Subgame01GridState.Empty;
        grid[5, 2] = Subgame01GridState.Empty;
        grid[6, 2] = Subgame01GridState.Empty;
        grid[9, 7] = Subgame01GridState.Empty;
    }

    void UpdateSelector() {
        if (subgame.GetKeyDown(Key.LeftArrow)) {
            selectorCoor.x--;
        } else if (subgame.GetKeyDown(Key.RightArrow)) {
            selectorCoor.x++;
        }
        if (subgame.GetKeyDown(Key.DownArrow)) {
            selectorCoor.y--;
        } else if (subgame.GetKeyDown(Key.UpArrow)) {
            selectorCoor.y++;
        }
        selectorCoor = new Vector2Int(
            Mathf.Clamp(selectorCoor.x, 0, grid.GetLength(0) - 1),
            Mathf.Clamp(selectorCoor.y, 0, grid.GetLength(1) - 1)
        );
        transformSelector.localPosition = new Vector3(selectorCoor.x, selectorCoor.y);
    }

    void UpdateBeats() {
        double raw = GetOffsetDSPTime() - dspStartTime;
        double songTime = raw % loopLength;
        double beat = songTime / 60 * BGM_TEMPO;
        if (lastBeat > beat) {
            lastBeat = -1;
        }
        if (beat < 0) {
            if (lastBeat != NO_BEAT && (int) beat != (int) lastBeat) {
                subgame.PlaySFX(sfxClick);
            }
        } else {
            foreach (double songBeat in BEATS) {
                if (lastBeat < songBeat && beat >= songBeat) {
                    Beat();
                    break;
                }
            }
        }
        lastBeat = beat;
    }
    double GetOffsetDSPTime() {
        return AudioSettings.dspTime + audioOffset;
    }
    void Beat() {
        Subgame01GridState state = grid[selectorCoor.x, selectorCoor.y];
        if (state == Subgame01GridState.Empty) {
            grid[selectorCoor.x, selectorCoor.y] = Subgame01GridState.Pass;
        } else {
            bool isVoid = state == Subgame01GridState.Void || state == Subgame01GridState.FailVoid;
            grid[selectorCoor.x, selectorCoor.y] = isVoid ? Subgame01GridState.FailVoid : Subgame01GridState.Fail;
            fails.x++;
            if (fails.x == fails.y) {
                subgame.GameOver();
            }
        }
    }

    void UpdateGrid() {
        // Initialize new grid cells.
        for (int x = 0; x < grid.GetLength(0); x++) {
            for (int y = 0; y < grid.GetLength(1); y++) {
                if (grid[x, y] != Subgame01GridState.Void && cellScripts[x, y] == null) {
                    Subgame01GridCellScript cellScript = Instantiate(prefabGridCell, transformGrid).GetComponent<Subgame01GridCellScript>();
                    cellScript.Init(this, new Vector2Int(x, y));
                    cellScripts[x, y] = cellScript;
                }
            }
        }
    }

    void Restart() {
        audioSourceBGM.Stop();
        audioSourceBGM.volume = initialBGMVolume;
        dspStartTime = AudioSettings.dspTime + 3.0;
        audioSourceBGM.PlayScheduled(dspStartTime);
        lastBeat = NO_BEAT;

        fails = new Vector2Int(0, 3);
        selectorCoor = new Vector2Int(4, 3);
        InitGrid();
    }
}

public enum Subgame01GridState {
    Void, Empty, Pass, Fail, FailVoid
}