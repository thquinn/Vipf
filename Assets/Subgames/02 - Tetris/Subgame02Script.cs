using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Subgame02Script : MonoBehaviour {
    static int INDEX_PREVIEW = 8;

    const int PIECE_I = 1;
    const int PIECE_O = 4;
    const int PIECE_X = 9;
    const int PIECE_LONG_I = 10;

    static readonly int[] LINE_CLEAR_SCORES = { 0, 1, 3, 6, 10, 20 };
    static readonly int[] BOX_SIZES = { 0, 4, 3, 3, 2, 3, 3, 3, 0, 3, 5 };

    static readonly Vector2Int[][] SHAPES = {
        null,
        new[] { V(0, 1), V(1, 1), V(2, 1), V(3, 1) },
        new[] { V(0, 0), V(0, 1), V(1, 1), V(2, 1) },
        new[] { V(2, 0), V(0, 1), V(1, 1), V(2, 1) },
        new[] { V(0, 0), V(1, 0), V(0, 1), V(1, 1) },
        new[] { V(1, 0), V(2, 0), V(0, 1), V(1, 1) },
        new[] { V(1, 0), V(0, 1), V(1, 1), V(2, 1) },
        new[] { V(0, 0), V(1, 0), V(1, 1), V(2, 1) },
        null,
        new[] { V(1, 0), V(0, 1), V(1, 1), V(2, 1), V(1, 2) },
        new[] { V(0, 2), V(1, 2), V(2, 2), V(3, 2), V(4, 2) },
    };

    static readonly int[] KICKS_NONE = { 0, 0 };

    static readonly int[] KICKS_PENTOMINO = {
        0, 0, -1, 0, 1, 0, -2, 0, 2, 0, 0, 1, 0, 2, -1, 1, 1, 1, -1, 2, 1, 2
    };

    static readonly int[][] KICKS_JLSTZ = {
        new[] { 0, 0, -1, 0, -1, 1, 0, -2, -1, -2 },
        new[] { 0, 0, 1, 0, 1, 1, 0, -2, 1, -2 },
        new[] { 0, 0, 1, 0, 1, -1, 0, 2, 1, 2 },
        new[] { 0, 0, 1, 0, 1, -1, 0, 2, 1, 2 },
        new[] { 0, 0, 1, 0, 1, 1, 0, -2, 1, -2 },
        new[] { 0, 0, -1, 0, -1, 1, 0, -2, -1, -2 },
        new[] { 0, 0, -1, 0, -1, -1, 0, 2, -1, 2 },
        new[] { 0, 0, -1, 0, -1, -1, 0, 2, -1, 2 },
        null,
    };

    static readonly int[][] KICKS_I = {
        new[] { 0, 0, -2, 0, 1, 0, -2, -1, 1, 2 },
        new[] { 0, 0, -1, 0, 2, 0, -1, 2, 2, -1 },
        new[] { 0, 0, -1, 0, 2, 0, -1, 2, 2, -1 },
        new[] { 0, 0, 2, 0, -1, 0, 2, 1, -1, -2 },
        new[] { 0, 0, 2, 0, -1, 0, 2, 1, -1, -2 },
        new[] { 0, 0, 1, 0, -2, 0, 1, -2, -2, 1 },
        new[] { 0, 0, 1, 0, -2, 0, 1, -2, -2, 1 },
        new[] { 0, 0, -2, 0, 1, 0, -2, -1, 1, 2 },
        null,
    };

    public AudioClip bgm;
    public AudioClip sfxRotateCW, sfxRotateCCW, sfxPieceLand, sfxLineClear;

    public Tilemap tilemap;
    public TileBase[] tiles;
    public Transform transformNextPreview;

    SubgameBaseScript subgame;
    int[,] grid;
    List<int> bag;
    float dropTimer;

    int pieceType;
    int pieceRotation;
    Vector2Int piecePosition;

    int width => grid.GetLength(0);
    int height => grid.GetLength(1);

    static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        grid = new int[11, 22];
        bag = new();
        SpawnPiece();
        subgame.PlayBGM(bgm);
    }

    void Update() {
        UpdateNextPreview();
        if (subgame.ShouldRestart()) {
            Restart();
            return;
        }
        if (subgame.isGameOver) {
            subgame.StopBGM();
            return;
        }

        dropTimer -= Time.deltaTime;

        int dx = 0, dy = 0;
        if (subgame.GetKeyDown(Key.LeftArrow)) dx = -1;
        if (subgame.GetKeyDown(Key.RightArrow)) dx = 1;
        if (subgame.GetKeyDown(Key.DownArrow) || dropTimer <= 0) {
            dy = 1;
            dropTimer = 0.1f + 9f / (9 + subgame.score);
        }
        bool hardDrop = subgame.GetKeyDown(Key.UpArrow);
        if (subgame.GetKeyDown(Key.Z)) TryRotate(1);
        if (subgame.GetKeyDown(Key.X)) TryRotate(-1);

        if (dx != 0) TryMove(dx, 0);
        if (hardDrop) {
            piecePosition = LandingPosition();
            LockPiece();
        } else if (dy != 0 && !TryMove(0, dy)) {
            LockPiece();
        }

        // Update tilemap.
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                tilemap.SetTile(new Vector3Int(x, y, 0), tiles[grid[x, y]]);
            }
        }

        DrawPiece(pieceType, LandingPosition(), pieceRotation, INDEX_PREVIEW);
        DrawPiece(pieceType, piecePosition, pieceRotation, pieceType);
    }

    void FillBag() {
        int lastInBag = bag.Count > 0 ? bag[0] : -1;
        bag.Clear();
        for (int i = 1; i <= 7; i++) bag.Add(i);
        bag.Shuffle();
        if (lastInBag > -1) {
            bag.Add(lastInBag);
        }
    }

    void SpawnPiece() {
        if (bag.Count <= 1) FillBag();
        pieceType = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        pieceRotation = 0;
        piecePosition = new Vector2Int((width - BOX_SIZES[pieceType]) / 2, SpawnRow(pieceType));
        if (!Fits(pieceType, piecePosition, pieceRotation)) GameOver();
    }

    int SpawnRow(int type) {
        int topRow = SHAPES[type].Min(cell => cell.y);
        return Mathf.Min(0, 1 - topRow);
    }

    void GameOver() {
        subgame.GameOver();
    }

    void Restart() {
        System.Array.Clear(grid, 0, grid.Length);
        bag.Clear();
        FillBag();
        dropTimer = 1;
        subgame.PlayBGM(bgm);
    }

    Vector2Int[] PieceCells(int type, Vector2Int position, int rotation) {
        int size = BOX_SIZES[type];
        Vector2Int[] shape = SHAPES[type];
        var cells = new Vector2Int[shape.Length];
        for (int i = 0; i < shape.Length; i++) {
            Vector2Int cell = shape[i];
            for (int r = 0; r < rotation; r++) {
                cell = new Vector2Int(size - 1 - cell.y, cell.x);
            }
            cells[i] = cell + position;
        }
        return cells;
    }

    bool Fits(int type, Vector2Int position, int rotation) {
        foreach (Vector2Int cell in PieceCells(type, position, rotation)) {
            if (cell.x < 0 || cell.x >= width || cell.y >= height) return false;
            if (cell.y >= 0 && grid[cell.x, cell.y] != 0) return false;
        }
        return true;
    }

    bool TryMove(int dx, int dy) {
        var target = piecePosition + new Vector2Int(dx, dy);
        if (!Fits(pieceType, target, pieceRotation)) return false;
        piecePosition = target;
        return true;
    }

    int[] KickTable(int type, int fromRotation, int direction) {
        if (type == PIECE_O || type == PIECE_X) return KICKS_NONE;
        if (type == PIECE_LONG_I) return KICKS_PENTOMINO;
        int index = fromRotation * 2 + (direction > 0 ? 0 : 1);
        return type == PIECE_I ? KICKS_I[index] : KICKS_JLSTZ[index];
    }

    bool TryRotate(int direction) {
        int newRotation = (pieceRotation + direction + 4) % 4;
        int[] kicks = KickTable(pieceType, pieceRotation, direction);
        for (int i = 0; i < kicks.Length / 2; i++) {
            var target = piecePosition + new Vector2Int(kicks[2 * i], -kicks[2 * i + 1]);
            if (Fits(pieceType, target, newRotation)) {
                piecePosition = target;
                pieceRotation = newRotation;
                subgame.PlaySFX(direction == -1 ? sfxRotateCW : sfxRotateCCW);
                return true;
            }
        }
        return false;
    }

    Vector2Int LandingPosition() {
        var position = piecePosition;
        while (Fits(pieceType, position + Vector2Int.up, pieceRotation)) {
            position += Vector2Int.up;
        }
        return position;
    }

    void LockPiece() {
        bool toppedOut = false;
        foreach (Vector2Int cell in PieceCells(pieceType, piecePosition, pieceRotation)) {
            if (cell.y < 0) toppedOut = true;
            else grid[cell.x, cell.y] = pieceType;
        }
        subgame.PlaySFX(sfxPieceLand);
        if (toppedOut) GameOver();
        else ClearFullRows();
        SpawnPiece();
    }

    void ClearFullRows() {
        int cleared = 0;
        bool containedX = GridContains(PIECE_X);
        for (int y = height - 1; y >= 0; y--) {
            if (IsRowFull(y)) {
                cleared++;
            } else if (cleared > 0) {
                CopyRow(y, y + cleared);
            }
        }
        for (int y = 0; y < cleared; y++) ClearRow(y);
        if (cleared > 0) {
            subgame.score += LINE_CLEAR_SCORES[Mathf.Min(cleared, LINE_CLEAR_SCORES.Length - 1)];
            subgame.PlaySFX(sfxLineClear);
        }
        if (cleared >= 4) subgame.EmitEvent(SubgameEvent.TetrisTetris);
        if (cleared >= 5) subgame.EmitEvent(SubgameEvent.TetrisPentris);
        if (cleared > 0 && containedX && !GridContains(PIECE_X)) subgame.EmitEvent(SubgameEvent.TetrisXClear);
    }

    bool IsRowFull(int y) {
        for (int x = 0; x < width; x++) {
            if (grid[x, y] == 0) return false;
        }
        return true;
    }

    void CopyRow(int fromY, int toY) {
        for (int x = 0; x < width; x++) grid[x, toY] = grid[x, fromY];
    }

    void ClearRow(int y) {
        for (int x = 0; x < width; x++) grid[x, y] = 0;
    }

    void DrawPiece(int type, Vector2Int position, int rotation, int tileIndex) {
        foreach (Vector2Int cell in PieceCells(type, position, rotation)) {
            if (cell.y < 0) continue;
            tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), tiles[tileIndex]);
        }
    }

    void UpdateNextPreview() {
        transformNextPreview.Rotate(0, Time.deltaTime * 60, 0, Space.World);
        Vector2Int[] shape = SHAPES[bag[bag.Count - 1]];
        int minX = shape.Min(coor => coor.x);
        int maxX = shape.Max(coor => coor.x);
        int minY = shape.Min(coor => coor.y);
        int maxY = shape.Max(coor => coor.y);
        float offX = -(minX + maxX) / 2f;
        float offY = -(minY + maxY) / 2f;
        for (int i = 0; i < transformNextPreview.childCount; i++) {
            if (i < shape.Length) {
                Vector3 pos = new Vector3(shape[i].y + offY, shape[i].x + offX);
                transformNextPreview.GetChild(i).transform.localPosition = pos;
            } else {
                transformNextPreview.GetChild(i).transform.localPosition = new Vector3(0, -20, 0);
            }
        }
    }

    public bool ReceiveBroadcastTetrisX() {
        if (pieceType == PIECE_X) return false;
        if (bag[bag.Count - 1] == PIECE_X) return false;
        if (GridContains(PIECE_X)) return false;
        bag[bag.Count - 1] = PIECE_X;
        return true;
    }

    bool GridContains(int value) {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] == value) return true;
            }
        }
        return false;
    }
}