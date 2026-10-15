using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-axis-aligned placement grid spanning the area between the four corner pillars.
/// Cell (0,0) is at the minimum X/Z corner; X runs along world X and Y along world Z.
/// </summary>
public class CafeGrid : MonoBehaviour
{
    [Tooltip("The four corner pillars. Their renderer bounds (or positions, if no renderer) define the grid area.")]
    public Transform[] corners = new Transform[4];

    [Min(0.01f)] public float cellSize = 56f;

    [Tooltip("Shifts the grid origin (X, Z) away from the minimum corner.")]
    public Vector2 originOffset;

    [Tooltip("Extra height added to the floor level derived from the corners.")]
    public float floorOffsetY;

    [Tooltip("Absolute world Y of the floor for raised (counter) cells.")]
    public float raisedFloorY = 81f;

    [Tooltip("Derive the column/row counts from the corner area. Disable to set them by hand.")]
    public bool autoSize = true;
    [Min(1)] public int columns = 21;
    [Min(1)] public int rows = 22;

    [Tooltip("Cells nothing can be placed on.")]
    public List<Vector2Int> blockedCells = new List<Vector2Int>();
    [Tooltip("Cells whose floor is raisedFloorY (e.g. the counter).")]
    public List<Vector2Int> raisedCells = new List<Vector2Int>();

    [Header("Gizmos")]
    public bool alwaysShowGrid = true;
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    public Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.35f);
    public Color raisedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    // ---- Geometry ----

    /// <summary>Min/max X/Z of the corner area and the floor Y.</summary>
    public bool TryGetCornerArea(out Vector2 min, out Vector2 max, out float floorY)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        floorY = float.MaxValue;
        int found = 0;
        foreach (var c in corners)
        {
            if (!c) continue;
            var r = c.GetComponent<Renderer>();
            Vector3 p = r ? r.bounds.center : c.position;
            float bottom = r ? r.bounds.min.y : c.position.y;
            min = Vector2.Min(min, new Vector2(p.x, p.z));
            max = Vector2.Max(max, new Vector2(p.x, p.z));
            floorY = Mathf.Min(floorY, bottom);
            found++;
        }
        return found >= 2;
    }

    public Vector2 Origin => TryGetCornerArea(out var min, out _, out _) ? min + originOffset : originOffset;

    public float FloorY => (TryGetCornerArea(out _, out _, out var y) ? y : transform.position.y) + floorOffsetY;

    public int Columns => autoSize && TryGetCornerArea(out var min, out var max, out _)
        ? Mathf.Max(1, Mathf.FloorToInt((max.x - min.x - originOffset.x) / cellSize)) : columns;

    public int Rows => autoSize && TryGetCornerArea(out var min, out var max, out _)
        ? Mathf.Max(1, Mathf.FloorToInt((max.y - min.y - originOffset.y) / cellSize)) : rows;

    // ---- Queries ----

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;
    public bool IsBlocked(Vector2Int cell) => blockedCells.Contains(cell);
    public bool IsRaised(Vector2Int cell) => raisedCells.Contains(cell);
    public bool CanPlace(Vector2Int cell) => InBounds(cell) && !IsBlocked(cell);

    public float CellFloorY(Vector2Int cell) => IsRaised(cell) ? raisedFloorY : FloorY;

    /// <summary>Cell containing the world position (may be out of bounds).</summary>
    public Vector2Int WorldToCell(Vector3 world)
    {
        var o = Origin;
        return new Vector2Int(Mathf.FloorToInt((world.x - o.x) / cellSize), Mathf.FloorToInt((world.z - o.y) / cellSize));
    }

    /// <summary>Center of the cell, standing on that cell's floor.</summary>
    public Vector3 CellToWorld(Vector2Int cell)
    {
        var o = Origin;
        return new Vector3(o.x + (cell.x + 0.5f) * cellSize, CellFloorY(cell), o.y + (cell.y + 0.5f) * cellSize);
    }

    /// <summary>Nearest placeable cell to a world position; false if it is out of bounds or blocked.</summary>
    public bool TrySnap(Vector3 world, out Vector2Int cell, out Vector3 snapped)
    {
        cell = WorldToCell(world);
        snapped = CellToWorld(cell);
        return CanPlace(cell);
    }

    // ---- Gizmos ----

    void OnDrawGizmos()
    {
        if (alwaysShowGrid) DrawGrid();
    }

    void OnDrawGizmosSelected()
    {
        if (!alwaysShowGrid) DrawGrid();
    }

    void DrawGrid()
    {
        var o = Origin;
        float y = FloorY + 0.5f;
        int cols = Columns, rws = Rows;
        float w = cols * cellSize, d = rws * cellSize;

        Gizmos.color = lineColor;
        for (int i = 0; i <= cols; i++)
            Gizmos.DrawLine(new Vector3(o.x + i * cellSize, y, o.y), new Vector3(o.x + i * cellSize, y, o.y + d));
        for (int j = 0; j <= rws; j++)
            Gizmos.DrawLine(new Vector3(o.x, y, o.y + j * cellSize), new Vector3(o.x + w, y, o.y + j * cellSize));

        Gizmos.color = raisedColor;
        foreach (var c in raisedCells) DrawCell(c, raisedFloorY);
        Gizmos.color = blockedColor;
        foreach (var c in blockedCells) DrawCell(c, CellFloorY(c));

        if (TryGetCornerArea(out var min, out var max, out var floorY))
        {
            Gizmos.color = Color.yellow;
            var center = new Vector3((min.x + max.x) / 2f, floorY, (min.y + max.y) / 2f);
            Gizmos.DrawWireCube(center, new Vector3(max.x - min.x, 0f, max.y - min.y));
        }
    }

    void DrawCell(Vector2Int cell, float floorY)
    {
        var o = Origin;
        var center = new Vector3(o.x + (cell.x + 0.5f) * cellSize, floorY + 1f, o.y + (cell.y + 0.5f) * cellSize);
        Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 2f, cellSize * 0.9f));
    }
}
