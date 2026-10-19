using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code {
    public static class UtilTictactic {
        // Offsets within Manhattan distance 2. The first four are the orthogonal neighbours.
        static readonly Vector2Int[] NEAR = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
            new(2, 0), new(-2, 0), new(0, 2), new(0, -2),
        };
        const int NEW_GROUP = -1, INVALID = -2;

        /// Fills `stones` with exactly n stones such that no empty cell touches two
        /// different groups, and there are at least 2 groups. Returns false if it
        /// couldn't (n < 2, n too large for the board, or repeated dead ends).
        public static bool FillWithoutCaptures(bool[,] stones, int n, int mixSteps = -1, int maxAttempts = 100) {
            int w = stones.GetLength(0), h = stones.GetLength(1);
            if (n < 2 || n > w * h) return false;
            if (mixSteps < 0) mixSteps = 20 * n;

            int[,] group = new int[w, h];
            List<Vector2Int> candidates = new(w * h);
            List<Vector2Int> placed = new(n);
            Stack<Vector2Int> stack = new();

            for (int attempt = 0; attempt < maxAttempts; attempt++) {
                // --- Phase 1: constructive growth. ---
                System.Array.Clear(stones, 0, stones.Length);
                for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) group[x, y] = -1;
                placed.Clear();
                int groupCount = 0;

                while (placed.Count < n) {
                    candidates.Clear();
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            if (!stones[x, y] && TargetGroup(group, x, y, w, h) != INVALID) {
                                candidates.Add(new Vector2Int(x, y));
                            }
                        }
                    }
                    if (candidates.Count == 0) break; // Dead end.

                    Vector2Int c = candidates[Random.Range(0, candidates.Count)];
                    int target = TargetGroup(group, c.x, c.y, w, h);
                    group[c.x, c.y] = target == NEW_GROUP ? groupCount++ : target;
                    stones[c.x, c.y] = true;
                    placed.Add(c);
                }
                if (placed.Count < n || groupCount < 2) continue;

                // --- Phase 2: Metropolis mixing (symmetric proposal => uniform stationary distribution). ---
                for (int step = 0; step < mixSteps; step++) {
                    int i = Random.Range(0, n);
                    Vector2Int from = placed[i];
                    stones[from.x, from.y] = false;

                    Vector2Int to;
                    do {
                        to = new Vector2Int(Random.Range(0, w), Random.Range(0, h));
                    } while (stones[to.x, to.y]);

                    if (to == from) {
                        stones[from.x, from.y] = true;
                        continue;
                    }

                    stones[to.x, to.y] = true;
                    if (IsCaptureFree(stones, group, stack, out int groups) && groups >= 2) {
                        placed[i] = to;
                    } else {
                        stones[to.x, to.y] = false;
                        stones[from.x, from.y] = true;
                    }
                }
                return true;
            }
            return false;
        }

        /// Which group would a stone at (x, y) join? NEW_GROUP if it starts a new one,
        /// INVALID if placing it would leave a capturable cell.
        static int TargetGroup(int[,] group, int x, int y, int w, int h) {
            int target = NEW_GROUP;
            for (int i = 0; i < NEAR.Length; i++) {
                int nx = x + NEAR[i].x, ny = y + NEAR[i].y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int g = group[nx, ny];
                if (g < 0) continue;                      // Empty.
                if (i < 4) target = g;                    // Orthogonal neighbour: we join its group.
                else if (g != target) return INVALID;     // Another group within distance 2.
            }
            return target;
        }

        /// Full check: labels groups, then verifies every pair of stones within distance 2 shares a group.
        static bool IsCaptureFree(bool[,] stones, int[,] label, Stack<Vector2Int> stack, out int groupCount) {
            int w = stones.GetLength(0), h = stones.GetLength(1);
            groupCount = 0;
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) label[x, y] = -1;

            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    if (!stones[x, y] || label[x, y] >= 0) continue;
                    label[x, y] = groupCount;
                    stack.Push(new Vector2Int(x, y));
                    while (stack.Count > 0) {
                        Vector2Int p = stack.Pop();
                        for (int i = 0; i < 4; i++) {
                            int nx = p.x + NEAR[i].x, ny = p.y + NEAR[i].y;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            if (stones[nx, ny] && label[nx, ny] < 0) {
                                label[nx, ny] = groupCount;
                                stack.Push(new Vector2Int(nx, ny));
                            }
                        }
                    }
                    groupCount++;
                }
            }

            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    if (!stones[x, y]) continue;
                    for (int i = 4; i < NEAR.Length; i++) {
                        int nx = x + NEAR[i].x, ny = y + NEAR[i].y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (stones[nx, ny] && label[nx, ny] != label[x, y]) return false;
                    }
                }
            }
            return true;
        }
    }
}
