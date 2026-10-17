using System.Collections.Generic;
using UnityEngine;

// Shortest paths over floor-level, unoccupied grid cells. 8-way moves; diagonals can't cut blocked corners.
public static class GridPathfinder
{
    static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    public static bool IsWalkable(CafeGrid grid, Vector2Int cell) =>
        grid.InBounds(cell) && !grid.IsRaised(cell) && !grid.IsOccupied(cell);

    // Path including both ends, or null. The start cell needn't be walkable (something may have been placed on it).
    public static List<Vector2Int> FindPath(CafeGrid grid, Vector2Int start, Vector2Int goal)
    {
        if (start == goal) return new List<Vector2Int> { start };

        var cost = new Dictionary<Vector2Int, float> { [start] = 0f };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        var open = new List<Vector2Int> { start };

        while (open.Count > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (cost[open[i]] < cost[open[best]]) best = i;
            var current = open[best];
            open.RemoveAt(best);
            if (!closed.Add(current)) continue;
            if (current == goal) return Reconstruct(cameFrom, current);

            foreach (var d in Directions)
            {
                var next = current + d;
                if (closed.Contains(next) || !IsWalkable(grid, next)) continue;
                bool diagonal = d.x != 0 && d.y != 0;
                if (diagonal && (!IsWalkable(grid, new Vector2Int(current.x + d.x, current.y)) ||
                                 !IsWalkable(grid, new Vector2Int(current.x, current.y + d.y))))
                    continue;

                float c = cost[current] + (diagonal ? 1.41421356f : 1f);
                if (cost.TryGetValue(next, out var old) && old <= c) continue;
                cost[next] = c;
                cameFrom[next] = current;
                open.Add(next);
            }
        }
        return null;
    }

    public static Vector2Int? NearestWalkable(CafeGrid grid, Vector3 world)
    {
        Vector2Int? best = null;
        float bestDist = float.MaxValue;
        for (int x = 0; x < grid.Columns; x++)
            for (int y = 0; y < grid.Rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!IsWalkable(grid, cell)) continue;
                var p = grid.CellToWorld(cell);
                float d = new Vector2(p.x - world.x, p.z - world.z).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = cell; }
            }
        return best;
    }

    static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int end)
    {
        var path = new List<Vector2Int> { end };
        while (cameFrom.TryGetValue(end, out var prev)) { end = prev; path.Add(end); }
        path.Reverse();
        return path;
    }
}
