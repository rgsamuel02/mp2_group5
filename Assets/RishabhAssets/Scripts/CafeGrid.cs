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

    [Min(0.001f)] public float cellSize = 0.56f;

    [Tooltip("Shifts the grid origin (X, Z) away from the minimum corner.")]
    public Vector2 originOffset;

    [Tooltip("Extra height added to the floor level derived from the corners.")]
    public float floorOffsetY;

    [Tooltip("Absolute world Y of the floor for raised (counter) cells.")]
    public float raisedFloorY = 0.81f;

    [Tooltip("Derive the column/row counts from the corner area. Disable to set them by hand.")]
    public bool autoSize = true;
    [Min(1)] public int columns = 21;
    [Min(1)] public int rows = 22;

    [Tooltip("Cells whose floor is raisedFloorY (e.g. the counter).")]
    public List<Vector2Int> raisedCells = new List<Vector2Int>();

    [Header("Gizmos")]
    public bool alwaysShowGrid = true;
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    public Color occupiedColor = new Color(1f, 0.5f, 0.1f, 0.35f);
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

    // ---- Occupancy (runtime) ----

    readonly Dictionary<Vector2Int, GridOccupant> occupied = new Dictionary<Vector2Int, GridOccupant>();

    // ---- Queries ----

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;
    public bool IsOccupied(Vector2Int cell) => occupied.ContainsKey(cell);
    public GridOccupant OccupantAt(Vector2Int cell) => occupied.TryGetValue(cell, out var o) ? o : null;
    public bool IsRaised(Vector2Int cell) => raisedCells.Contains(cell);
    public bool CanPlace(Vector2Int cell) => InBounds(cell) && !IsOccupied(cell);

    /// <summary>Footprint size after rotating by <paramref name="quarterTurns"/> x 90 degrees about Y.</summary>
    public static Vector2Int RotatedSize(Vector2Int size, int quarterTurns) =>
        (quarterTurns & 1) == 0 ? size : new Vector2Int(size.y, size.x);

    /// <summary>Every cell covered by a footprint whose minimum cell is <paramref name="anchor"/>.</summary>
    public IEnumerable<Vector2Int> FootprintCells(Vector2Int anchor, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return new Vector2Int(anchor.x + x, anchor.y + y);
    }

    /// <summary>True if every cell of the footprint is in bounds and free (cells owned by <paramref name="ignore"/> count as free).</summary>
    public bool CanPlace(Vector2Int anchor, Vector2Int size, GridOccupant ignore = null)
    {
        foreach (var c in FootprintCells(anchor, size))
            if (!InBounds(c) || (occupied.TryGetValue(c, out var o) && o != ignore)) return false;
        return true;
    }

    public float CellFloorY(Vector2Int cell) => IsRaised(cell) ? raisedFloorY : FloorY;

    /// <summary>Highest floor under the footprint, so objects never sink into a raised cell.</summary>
    public float FootprintFloorY(Vector2Int anchor, Vector2Int size)
    {
        float y = float.MinValue;
        foreach (var c in FootprintCells(anchor, size)) y = Mathf.Max(y, CellFloorY(c));
        return y;
    }

    /// <summary>Center of a footprint, standing on its floor.</summary>
    public Vector3 FootprintToWorld(Vector2Int anchor, Vector2Int size)
    {
        var o = Origin;
        return new Vector3(o.x + (anchor.x + size.x * 0.5f) * cellSize, FootprintFloorY(anchor, size), o.y + (anchor.y + size.y * 0.5f) * cellSize);
    }

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

    /// <summary>
    /// Anchor cell for a footprint centered as close as possible to <paramref name="world"/>.
    /// Returns false if that footprint is out of bounds or overlaps something.
    /// </summary>
    public bool TrySnap(Vector3 world, Vector2Int size, out Vector2Int anchor, out Vector3 snapped, GridOccupant ignore = null)
    {
        var o = Origin;
        anchor = new Vector2Int(
            Mathf.RoundToInt((world.x - o.x) / cellSize - size.x * 0.5f),
            Mathf.RoundToInt((world.z - o.y) / cellSize - size.y * 0.5f));
        snapped = FootprintToWorld(anchor, size);
        return CanPlace(anchor, size, ignore);
    }

    // ---- Placement ----

    /// <summary>Marks the occupant's footprint as used. Fails without changes if any cell is taken.</summary>
    public bool Occupy(GridOccupant occupant, Vector2Int anchor, int quarterTurns)
    {
        var size = RotatedSize(occupant.footprint, quarterTurns);
        if (!CanPlace(anchor, size, occupant)) return false;
        Release(occupant);
        foreach (var c in FootprintCells(anchor, size)) occupied[c] = occupant;
        occupant.grid = this;
        occupant.anchor = anchor;
        occupant.quarterTurns = quarterTurns;
        occupant.isPlaced = true;
        return true;
    }

    /// <summary>Frees every cell held by the occupant.</summary>
    public void Release(GridOccupant occupant)
    {
        if (!occupant || !occupant.isPlaced) return;
        foreach (var c in FootprintCells(occupant.anchor, RotatedSize(occupant.footprint, occupant.quarterTurns)))
            if (occupied.TryGetValue(c, out var o) && o == occupant) occupied.Remove(c);
        occupant.isPlaced = false;
    }

    /// <summary>
    /// Instantiates <paramref name="item"/>'s prefab, occupies its footprint and moves it onto the cells.
    /// Returns null (spawning nothing) if the footprint doesn't fit.
    /// </summary>
    public GridOccupant Spawn(ShopItem item, Vector2Int anchor, int quarterTurns = 0, Transform parent = null)
    {
        if (!item || !item.prefab) return null;
        quarterTurns = ((quarterTurns % 4) + 4) % 4;
        if (!CanPlace(anchor, RotatedSize(item.footprint, quarterTurns))) return null;

        var go = Instantiate(item.prefab, parent ? parent : transform);
        var occupant = go.GetComponent<GridOccupant>();
        if (!occupant) occupant = go.AddComponent<GridOccupant>();
        occupant.item = item;
        occupant.footprint = item.footprint;

        Occupy(occupant, anchor, quarterTurns);
        AlignToFootprint(occupant);
        return occupant;
    }

    /// <summary>Rotates and moves the occupant so its render bounds sit centered on its footprint's floor.</summary>
    public void AlignToFootprint(GridOccupant occupant)
    {
        var t = occupant.transform;
        var size = RotatedSize(occupant.footprint, occupant.quarterTurns);
        t.rotation = Quaternion.Euler(0f, 90f * occupant.quarterTurns, 0f) * occupant.BaseRotation;

        // Prefab pivots aren't consistent, so align by render bounds instead of the pivot.
        t.position = Vector3.zero;
        var target = FootprintToWorld(occupant.anchor, size);
        if (occupant.TryGetBounds(out var b))
            t.position = new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);
        else
            t.position = target;
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
        float y = FloorY + cellSize * 0.01f;
        int cols = Columns, rws = Rows;
        float w = cols * cellSize, d = rws * cellSize;

        Gizmos.color = lineColor;
        for (int i = 0; i <= cols; i++)
            Gizmos.DrawLine(new Vector3(o.x + i * cellSize, y, o.y), new Vector3(o.x + i * cellSize, y, o.y + d));
        for (int j = 0; j <= rws; j++)
            Gizmos.DrawLine(new Vector3(o.x, y, o.y + j * cellSize), new Vector3(o.x + w, y, o.y + j * cellSize));

        Gizmos.color = raisedColor;
        foreach (var c in raisedCells) DrawCell(c, raisedFloorY);
        Gizmos.color = occupiedColor;
        foreach (var c in occupied.Keys) DrawCell(c, CellFloorY(c));

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
        float thickness = cellSize * 0.04f;
        var center = new Vector3(o.x + (cell.x + 0.5f) * cellSize, floorY + thickness * 0.5f, o.y + (cell.y + 0.5f) * cellSize);
        Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, thickness, cellSize * 0.9f));
    }
}
