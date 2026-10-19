using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Assets.Code;

public class Subgame05Script : MonoBehaviour {
    static int TRACKER_WIDTH = 24;

    public AudioClip sfxPlay, sfxCollect, sfxTeardown;

    public Grid grid;
    public Tilemap tilemap, tracker;
    public Tile tile, tileProvisional;
    public Tile trackerTileEmpty, trackerTileFull, trackerTileEllipsis, trackerTileArrow;
    public SpriteRenderer srCursor;
    public TMP_Text tmpCPUScore;
    public int roundStartCount, roundEndCount;
    public int initialEnemyTarget;

    SubgameBaseScript subgame;
    bool[,] stones;
    Vector2Int cursor;
    Vector3 initialCursorPosition;
    int round;
    TictacticState state;
    float pause;
    Vector2Int enemyScore;
    HashSet<Vector2Int> capture;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        stones = new bool[7, 7];
        initialCursorPosition = srCursor.transform.localPosition;
        Restart();
    }

    void Update() {
        if (subgame.isStartBlocked) return;
        if (subgame.ShouldRestart()) {
            Restart();
            return;
        }
        if (subgame.isGameOver) return;
        pause -= Time.deltaTime;

        if (pause <= 0) {
            // Player and enemy.
            if (state == TictacticState.PlayerTurn) {
                // Player.
                cursor += InputDirection();
                cursor.x = Mathf.Clamp(cursor.x, 0, stones.GetLength(0) - 1);
                cursor.y = Mathf.Clamp(cursor.y, 0, stones.GetLength(1) - 1);
                srCursor.transform.localPosition = initialCursorPosition + new Vector3(cursor.x * grid.cellSize.x, cursor.y * grid.cellSize.y, 0);
                if (subgame.GetKeyDown(Key.Z)) Play(cursor);
            } else if (state == TictacticState.CPUTurn) {
                // CPU.
                List<Vector2Int> bestCoors = new();
                int bestCount = 0;
                for (int x = 0; x < stones.GetLength(0); x++) {
                    for (int y = 0; y < stones.GetLength(1); y++) {
                        if (stones[x, y]) continue;
                        Vector2Int coor = new Vector2Int(x, y);
                        int count = GetCaptureAt(coor)?.Count ?? 0;
                        if (count > bestCount) {
                            bestCoors.Clear();
                            bestCount = count;
                        }
                        if (count == bestCount) bestCoors.Add(coor);
                    }
                }
                Play(bestCoors[Random.Range(0, bestCoors.Count)]);
            } else if (state == TictacticState.PlayerCollect || state == TictacticState.CPUCollect) {
                // Collect.
                if (capture == null) {
                    if (CountStones() <= roundEndCount) {
                        if (state == TictacticState.PlayerCollect) {
                            round++;
                            int enemyTarget = Mathf.Max(3, initialEnemyTarget - round);
                            enemyScore = new Vector2Int(0, enemyTarget);
                        }
                        state = TictacticState.RoundTeardown;
                    } else {
                        NextState();
                        if (state == TictacticState.CPUTurn) pause = 1;
                    }
                } else {
                    foreach (Vector2Int coor in capture) {
                        stones[coor.x, coor.y] = false;
                    }
                    if (state == TictacticState.PlayerCollect) subgame.AddPoints(capture.Count);
                    else enemyScore.x += capture.Count;
                    if (enemyScore.x >= enemyScore.y) subgame.GameOver();
                    capture = null;
                    pause = 1;
                    subgame.PlaySFX(sfxCollect);
                }
            } else if (state == TictacticState.RoundTeardown) {
                if (CountStones() == 0) {
                    StartRound();
                    state = TictacticState.PlayerTurn;
                } else {
                    RemoveRandomStone();
                    subgame.PlaySFX(sfxTeardown);
                    pause = CountStones() == 0 ? 1 : 0.2f;
                }
            }
        }


        // Update visuals.
        for (int x = 0; x < stones.GetLength(0); x++) {
            for (int y = 0; y < stones.GetLength(1); y++) {
                if (stones[x, y]) {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                } else tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
        int stoneCount = CountStones();
        for (int i = 0; i < TRACKER_WIDTH; i++) {
            tracker.SetTile(new Vector3Int(i, 0, 0), i < stoneCount ? trackerTileFull : trackerTileEmpty);
            tracker.SetTile(new Vector3Int(i, 1, 0), i == roundEndCount - 1 ? trackerTileArrow : null);
        }
        tracker.SetTile(new Vector3Int(TRACKER_WIDTH, 0, 0), trackerTileEllipsis);
        if (state == TictacticState.PlayerTurn && !stones[cursor.x, cursor.y]) {
            tilemap.SetTile(new Vector3Int(cursor.x, cursor.y, 0), tileProvisional);
        }
        srCursor.enabled = state < TictacticState.CPUTurn;
        tmpCPUScore.text = $"{enemyScore.x}/{enemyScore.y}";
    }
    void NextState() {
        state = (TictacticState)(((int)state + 1) % 4);
    }

    Vector2Int InputDirection() {
        int x = 0, y = 0;
        if (subgame.GetKeyDown(Key.LeftArrow)) x = -1;
        else if (subgame.GetKeyDown(Key.RightArrow)) x = 1;
        if (subgame.GetKeyDown(Key.DownArrow)) y = -1;
        else if (subgame.GetKeyDown(Key.UpArrow)) y = 1;
        return new Vector2Int(x, y);
    }
    void Play(Vector2Int coor) {
        if (stones[coor.x, coor.y]) {
            if (state == TictacticState.CPUTurn) Debug.Log("Tictactic: CPU played an invalid move.");
            return;
        }

        capture = GetCaptureAt(coor);
        stones[coor.x, coor.y] = true;
        NextState();
        pause = capture == null ? 0 : 1;
        subgame.PlaySFX(sfxPlay);
    }
    HashSet<Vector2Int> GetCaptureAt(Vector2Int coor) {
        HashSet<Vector2Int> captureAtCoor = null;
        List<Vector2Int> neighbors = new(GetOccupiedNeighbors(coor));
        if (neighbors.Count < 2) return null;
        HashSet<Vector2Int> allConnected = new();
        foreach (Vector2Int neighbor in neighbors) {
            int prevSize = allConnected.Count;
            allConnected.UnionWith(GetConnected(neighbor));
            if (prevSize > 0 && allConnected.Count > prevSize) {
                captureAtCoor = allConnected;
            }
        }
        captureAtCoor?.Add(coor);
        return captureAtCoor;
    }
    IEnumerable<Vector2Int> GetOccupiedNeighbors(Vector2Int coor) {
        if (coor.x > 0 && stones[coor.x - 1, coor.y]) yield return new Vector2Int(coor.x - 1, coor.y);
        if (coor.x < stones.GetLength(0) - 1 && stones[coor.x + 1, coor.y]) yield return new Vector2Int(coor.x + 1, coor.y);
        if (coor.y > 0 && stones[coor.x, coor.y - 1]) yield return new Vector2Int(coor.x, coor.y - 1);
        if (coor.y < stones.GetLength(1) - 1 && stones[coor.x, coor.y + 1]) yield return new Vector2Int(coor.x, coor.y + 1);
    }
    HashSet<Vector2Int> GetConnected(Vector2Int coor) {
        HashSet<Vector2Int> connected = new();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        connected.Add(coor);
        queue.Enqueue(coor);
        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int neighbor in GetOccupiedNeighbors(current)) {
                if (!connected.Contains(neighbor)) {
                    connected.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return connected;
    }
    int CountStones() {
        int stoneCount = 0;
        for (int x = 0; x < stones.GetLength(0); x++) {
            for (int y = 0; y < stones.GetLength(1); y++) {
                if (stones[x, y]) stoneCount++;
            }
        }
        return stoneCount;
    }
    void RemoveRandomStone() {
        List<int> indices = Enumerable.Range(0, stones.Length).ToList();
        indices.Shuffle();
        foreach (int i in indices) {
            int x = i % stones.GetLength(0);
            int y = i / stones.GetLength(0);
            if (stones[x, y]) {
                stones[x, y] = false;
                return;
            }
        }
    }

    void Restart() {
        round = 0;
        pause = 0;
        enemyScore = new Vector2Int(0, initialEnemyTarget);
        StartRound();
    }
    void StartRound() {
        capture = null;
        UtilTictactic.FillWithoutCaptures(stones, roundStartCount);
    }
}

enum TictacticState {
    PlayerTurn, PlayerCollect, CPUTurn, CPUCollect, RoundTeardown
}