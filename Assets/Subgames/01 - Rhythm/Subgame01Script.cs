using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Subgame01Script : MonoBehaviour {
    static float NO_BEAT = -999;
    static float BGM_TEMPO = 85;
    static double[] BEATS = new double[] {
        0, 2, 4, 5, 6,
        8, 10, 12, 13, 14,
        16, 17, 18, 19, 20, 22,
        24, 24.5, 25, 25.5, 26, 28, 29, 30,
    };
    static int LOOKAHEAD_BEATS = 3;

    public GameObject prefabGridCell, prefabFailIcon, prefabNote;

    public Transform transformGrid, transformFails, transformNotes, transformSelector;
    public GameObject gracePeriodCover;
    public AudioClip bgm;
    public AudioClip sfxClick;
    public double audioOffset;

    SubgameBaseScript subgame;

    [HideInInspector] public Vector2Int fails;
    [HideInInspector] public Subgame01GridState[,] grid;
    Subgame01GridCellScript[,] cellScripts;
    Vector2Int selectorCoor;
    List<Subgame01FailIconScript> failIcons;
    List<Subgame01NoteScript> notes;
    float gracePeriod;
    
    AudioSource audioSourceBGM;
    float initialBGMVolume;
    double loopLength, dspStartTime;
    double lastBeat;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        grid = new Subgame01GridState[10, 8];
        cellScripts = new Subgame01GridCellScript[10, 8];
        failIcons = new();
        notes = new();
        
        audioSourceBGM = subgame.handheldScript?.audioSourceBGM;
        initialBGMVolume = audioSourceBGM.volume;
        audioSourceBGM.clip = bgm;
        loopLength = (double)bgm.samples / bgm.frequency;
        Restart();
        selectorCoor = new Vector2Int(4, 3);
    }

    void Update() {
        if (!subgame.isGameOver) {
            UpdateSelector();
            UpdateBeats();
        }
        UpdateGrid();
        UpdateFails();

        gracePeriod -= Time.deltaTime;
        gracePeriodCover.SetActive(gracePeriod > 0);
        if (subgame.isGameOver) {
            audioSourceBGM.volume -= Time.deltaTime * 0.5f;
        }
        if (audioSourceBGM.volume <= 0 && subgame.ShouldRestart()) {
            Restart();
        }
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
        // Spawn notes.
        if (lastBeat != NO_BEAT) {
            double secondsPerBeat = 60.0 / BGM_TEMPO;
            double loopBeats = loopLength / secondsPerBeat;
            double lastLookahead = (lastBeat + LOOKAHEAD_BEATS) % (loopBeats - 0.00001);
            double lookahead = (beat + LOOKAHEAD_BEATS) % (loopBeats - 0.00001);
            foreach (double songBeat in BEATS) {
                if (lastLookahead > lookahead && lookahead >= songBeat) {
                    // Wraparound beats at start of song.
                    float timeUntilBeat = (float)((loopBeats - beat + songBeat) * secondsPerBeat);
                    Subgame01NoteScript note = Instantiate(prefabNote, transformNotes).GetComponent<Subgame01NoteScript>();
                    note.Init(Time.time + timeUntilBeat);
                    notes.Add(note);
                } else if (lastLookahead < songBeat && lookahead >= songBeat) {
                    float timeUntilBeat = (float)((songBeat - beat) * secondsPerBeat);
                    if (timeUntilBeat < 0) timeUntilBeat += (float)loopLength;
                    if (timeUntilBeat > 10) timeUntilBeat -= (float)loopLength;
                    Subgame01NoteScript note = Instantiate(prefabNote, transformNotes).GetComponent<Subgame01NoteScript>();
                    note.Init(Time.time + timeUntilBeat);
                    notes.Add(note);
                }
            }
        }
        lastBeat = beat;
    }
    double GetOffsetDSPTime() {
        return AudioSettings.dspTime + audioOffset;
    }
    void Beat() {
        if (gracePeriod > 0) return;
        Subgame01GridState state = grid[selectorCoor.x, selectorCoor.y];
        if (state == Subgame01GridState.Empty) {
            grid[selectorCoor.x, selectorCoor.y] = Subgame01GridState.Pass;
            subgame.AddPoints(1);
            if (!GridHasEmpties()) {
                // If you clear a grid without messing up, you get a life back.
                if (!GridHasFails()) {
                    fails.x = Mathf.Max(0, fails.x - 1);
                }
                InitGrid(subgame, grid);
                gracePeriod = 2;
            }
        } else {
            bool isVoid = state == Subgame01GridState.Void || state == Subgame01GridState.FailVoid;
            grid[selectorCoor.x, selectorCoor.y] = isVoid ? Subgame01GridState.FailVoid : Subgame01GridState.Fail;
            fails.x++;
            if (fails.x == fails.y) {
                subgame.GameOver();
            }
        }
    }
    bool GridHasEmpties() {
        for (int x = 0; x < grid.GetLength(0); x++) {
            for (int y = 0; y < grid.GetLength(1); y++) {
                if (grid[x, y] == Subgame01GridState.Empty) return true;
            }
        }
        return false;
    }
    bool GridHasFails() {
        for (int x = 0; x < grid.GetLength(0); x++) {
            for (int y = 0; y < grid.GetLength(1); y++) {
                if (grid[x, y] == Subgame01GridState.Fail) return true;
                if (grid[x, y] == Subgame01GridState.FailVoid) return true;
            }
        }
        return false;
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

    void UpdateFails() {
        for (int i = failIcons.Count; i < fails.y; i++) {
            Subgame01FailIconScript failIconScript = Instantiate(prefabFailIcon, transformFails).GetComponent<Subgame01FailIconScript>();
            failIconScript.Init(this, i);
            failIcons.Add(failIconScript);
        }
    }

    void Restart() {
        audioSourceBGM.Stop();
        audioSourceBGM.volume = initialBGMVolume;
        dspStartTime = AudioSettings.dspTime + 3.0;
        audioSourceBGM.PlayScheduled(dspStartTime);
        lastBeat = NO_BEAT;

        fails = new Vector2Int(0, 3);
        ClearNotes();
        InitGrid(subgame, grid);
    }
    void ClearNotes() {
        foreach (Subgame01NoteScript note in notes) {
            if (note != null) Destroy(note.gameObject);
        }
        notes.Clear();
    }

    // Grid filling logic.
    static readonly int[] DX = { 1, -1, 0, 0 };
    static readonly int[] DY = { 0, 0, 1, -1 };

    static void InitGrid(SubgameBaseScript subgame, Subgame01GridState[,] grid) {
        System.Array.Clear(grid, 0, grid.Length);

        int cellCount = Mathf.FloorToInt(5 + Mathf.Pow(subgame.score, 0.75f) * 0.5f);
        cellCount = Mathf.Min(cellCount, grid.Length);
        int islandCount = Mathf.FloorToInt(1 + Mathf.Pow(subgame.score, 0.5f) * 0.25f);
        islandCount = Mathf.Min(islandCount, cellCount);

        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int i = 0; i < cellCount; i++) {
            int x, y;
            if (i < islandCount) {
                // Retry until we land on a Void cell so islands never overlap.
                do {
                    x = Random.Range(0, width);
                    y = Random.Range(0, height);
                } while (grid[x, y] != Subgame01GridState.Void);
            } else if (!TryPickWeightedVoid(grid, out x, out y)) {
                break; // No Void cells left (or none adjacent to an Empty).
            }
            grid[x, y] = Subgame01GridState.Empty;
        }
    }

    static int CountEmptyNeighbors(Subgame01GridState[,] grid, int x, int y) {
        int count = 0;
        for (int d = 0; d < 4; d++) {
            int nx = x + DX[d];
            int ny = y + DY[d];
            if (nx < 0 || ny < 0 || nx >= grid.GetLength(0) || ny >= grid.GetLength(1)) continue;
            if (grid[nx, ny] == Subgame01GridState.Empty) count++;
        }
        return count;
    }

    static bool TryPickWeightedVoid(Subgame01GridState[,] grid, out int px, out int py) {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        // Pass 1: total weight.
        int total = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == Subgame01GridState.Void)
                    total += CountEmptyNeighbors(grid, x, y);

        px = py = -1;
        if (total == 0) return false;

        // Pass 2: find the cell the roll lands in.
        int roll = Random.Range(0, total); // max is exclusive for ints
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] != Subgame01GridState.Void) continue;
                roll -= CountEmptyNeighbors(grid, x, y);
                if (roll < 0) {
                    px = x;
                    py = y;
                    return true;
                }
            }
        }
        return false; // Unreachable, but keeps the compiler happy.
    }
}

public enum Subgame01GridState {
    Void, Empty, Pass, Fail, FailVoid
}