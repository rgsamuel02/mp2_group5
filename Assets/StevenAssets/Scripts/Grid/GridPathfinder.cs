using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// A* path planning over the kitchen grid (4-connected, Manhattan heuristic).
    /// PathwaysOnly plans exclusively across Walkway Mat buildings; AvoidBuildings plans around blocking buildings.
    /// </summary>
    public static class GridPathfinder
    {
        // Scratch buffers reused between calls (single-threaded game code).
        static float[] s_G = new float[0];
        static int[] s_Parent = new int[0];
        static bool[] s_Closed = new bool[0];
        static bool[] s_Open = new bool[0];
        static readonly List<int> s_OpenList = new List<int>(256);

        /// <summary>
        /// Plans from start to goal. The start and goal cells themselves may be unwalkable
        /// (agents start on spawn points and "bump" into their target building).
        /// </summary>
        public static bool FindPath(KitchenGrid grid, Vector2Int start, Vector2Int goal, PathMode mode, List<Vector2Int> result)
        {
            result.Clear();
            if (grid == null || !grid.InBounds(start) || !grid.InBounds(goal))
                return false;
            if (start == goal)
            {
                result.Add(start);
                return true;
            }

            var w = grid.width;
            var n = grid.width * grid.depth;
            EnsureBuffers(n);
            for (var i = 0; i < n; i++)
            {
                s_G[i] = float.MaxValue;
                s_Parent[i] = -1;
                s_Closed[i] = false;
                s_Open[i] = false;
            }
            s_OpenList.Clear();

            var startIndex = start.y * w + start.x;
            var goalIndex = goal.y * w + goal.x;
            s_G[startIndex] = 0f;
            s_Open[startIndex] = true;
            s_OpenList.Add(startIndex);

            while (s_OpenList.Count > 0)
            {
                // Pick the open node with the lowest f = g + h (open list is small: <= 200 cells).
                var bestSlot = 0;
                var bestF = float.MaxValue;
                for (var i = 0; i < s_OpenList.Count; i++)
                {
                    var idx = s_OpenList[i];
                    var f = s_G[idx] + Heuristic(idx % w, idx / w, goal);
                    if (f < bestF)
                    {
                        bestF = f;
                        bestSlot = i;
                    }
                }

                var current = s_OpenList[bestSlot];
                s_OpenList[bestSlot] = s_OpenList[s_OpenList.Count - 1];
                s_OpenList.RemoveAt(s_OpenList.Count - 1);
                s_Open[current] = false;
                s_Closed[current] = true;

                if (current == goalIndex)
                {
                    Reconstruct(current, w, result);
                    return true;
                }

                var cx = current % w;
                var cz = current / w;
                foreach (var d in KitchenGrid.Directions4)
                {
                    var nc = new Vector2Int(cx + d.x, cz + d.y);
                    if (!grid.InBounds(nc))
                        continue;
                    var ni = nc.y * w + nc.x;
                    if (s_Closed[ni])
                        continue;
                    if (ni != goalIndex && !grid.IsWalkable(nc, mode))
                        continue;
                    var g = s_G[current] + 1f;
                    if (g < s_G[ni])
                    {
                        s_G[ni] = g;
                        s_Parent[ni] = current;
                        if (!s_Open[ni])
                        {
                            s_Open[ni] = true;
                            s_OpenList.Add(ni);
                        }
                    }
                }
            }

            return false;
        }

        static float Heuristic(int x, int z, Vector2Int goal) => Mathf.Abs(goal.x - x) + Mathf.Abs(goal.y - z);

        static void Reconstruct(int node, int w, List<Vector2Int> result)
        {
            while (node >= 0)
            {
                result.Add(new Vector2Int(node % w, node / w));
                node = s_Parent[node];
            }
            result.Reverse();
        }

        static void EnsureBuffers(int n)
        {
            if (s_G.Length >= n)
                return;
            s_G = new float[n];
            s_Parent = new int[n];
            s_Closed = new bool[n];
            s_Open = new bool[n];
        }
    }
}
