using System.Collections.Generic;
using UnityEngine;

// World-axis-aligned placement grid between the corner pillars. Cell (0,0) is the min X/Z corner; cell.y runs along world Z.
public class CafeGrid : MonoBehaviour
{
    [Tooltip("Corner pillars; their renderer bounds (or positions) define the grid area.")]
    public Transform[] corners = new Transform[4];
    [Min(0.001f)] public float cellSize = 0.56f;
    public Vector2 originOffset;
    public float floorOffsetY;
    [Tooltip("World Y of the floor on raised (counter) cells.")]
    public float raisedFloorY = 0.81f;

    [Tooltip("Derive columns/rows from the corner area.")]
    public bool autoSize = true;
    [Min(1)] public int columns = 21;
    [Min(1)] public int rows = 22;

    public List<Vector2Int> raisedCells = new List<Vector2Int>();
    [Tooltip("Cells permanently blocked by non-grid fixtures (e.g. TableReciever, DeliveryCrate_Receiver, CoffeeBucket).")]
    public List<Vector2Int> blockedCells = new List<Vector2Int>();

    [Header("Gizmos")]
    public bool alwaysShowGrid = true;
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    public Color occupiedColor = new Color(1f, 0.5f, 0.1f, 0.35f);
    public Color raisedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    readonly Dictionary<Vector2Int, GridOccupant> occupied = new Dictionary<Vector2Int, GridOccupant>();

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
            min = Vector2.Min(min, new Vector2(p.x, p.z));
            max = Vector2.Max(max, new Vector2(p.x, p.z));
            floorY = Mathf.Min(floorY, r ? r.bounds.min.y : c.position.y);
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

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;
    public bool IsOccupied(Vector2Int cell) => occupied.ContainsKey(cell) || blockedCells.Contains(cell);
    public bool IsRaised(Vector2Int cell) => raisedCells.Contains(cell);
    public float CellFloorY(Vector2Int cell) => IsRaised(cell) ? raisedFloorY : FloorY;

    public void BlockCells(IEnumerable<Vector2Int> cells)
    {
        if (cells == null) return;
        foreach (var c in cells)
        {
            if (!blockedCells.Contains(c))
                blockedCells.Add(c);
        }
    }

    public static Vector2Int RotatedSize(Vector2Int size, int quarterTurns) =>
        (quarterTurns & 1) == 0 ? size : new Vector2Int(size.y, size.x);

    public IEnumerable<Vector2Int> FootprintCells(Vector2Int anchor, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return new Vector2Int(anchor.x + x, anchor.y + y);
    }

    // Cells owned by `ignore` count as free.
    public bool CanPlace(Vector2Int anchor, Vector2Int size, GridOccupant ignore = null)
    {
        foreach (var c in FootprintCells(anchor, size))
            if (!InBounds(c) || blockedCells.Contains(c) || (occupied.TryGetValue(c, out var o) && o != ignore)) return false;
        return true;
    }

    // Highest floor under the footprint, so nothing sinks into a raised cell.
    public float FootprintFloorY(Vector2Int anchor, Vector2Int size)
    {
        float y = float.MinValue;
        foreach (var c in FootprintCells(anchor, size)) y = Mathf.Max(y, CellFloorY(c));
        return y;
    }

    public Vector3 FootprintToWorld(Vector2Int anchor, Vector2Int size)
    {
        var o = Origin;
        return new Vector3(o.x + (anchor.x + size.x * 0.5f) * cellSize, FootprintFloorY(anchor, size), o.y + (anchor.y + size.y * 0.5f) * cellSize);
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        var o = Origin;
        return new Vector2Int(Mathf.FloorToInt((world.x - o.x) / cellSize), Mathf.FloorToInt((world.z - o.y) / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        var o = Origin;
        return new Vector3(o.x + (cell.x + 0.5f) * cellSize, CellFloorY(cell), o.y + (cell.y + 0.5f) * cellSize);
    }

    // Anchor for a footprint centered nearest `world`; false if it doesn't fit there.
    public bool TrySnap(Vector3 world, Vector2Int size, out Vector2Int anchor, out Vector3 snapped)
    {
        var o = Origin;
        anchor = new Vector2Int(
            Mathf.RoundToInt((world.x - o.x) / cellSize - size.x * 0.5f),
            Mathf.RoundToInt((world.z - o.y) / cellSize - size.y * 0.5f));
        snapped = FootprintToWorld(anchor, size);
        return CanPlace(anchor, size);
    }

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

    public void Release(GridOccupant occupant)
    {
        if (!occupant || !occupant.isPlaced) return;
        foreach (var c in FootprintCells(occupant.anchor, RotatedSize(occupant.footprint, occupant.quarterTurns)))
            if (occupied.TryGetValue(c, out var o) && o == occupant) occupied.Remove(c);
        occupant.isPlaced = false;
    }

    // Instantiates the item on the grid; null if it doesn't fit.
    public GridOccupant Spawn(ShopItem item, Vector2Int anchor, int quarterTurns = 0)
    {
        if (!item || !item.prefab) return null;
        quarterTurns = ((quarterTurns % 4) + 4) % 4;
        if (!CanPlace(anchor, RotatedSize(item.footprint, quarterTurns))) return null;

        var go = Instantiate(item.prefab, transform);
        var occupant = go.GetComponent<GridOccupant>();
        if (!occupant) occupant = go.AddComponent<GridOccupant>();
        occupant.footprint = item.footprint;

        Occupy(occupant, anchor, quarterTurns);
        AlignToFootprint(occupant);
        return occupant;
    }

    public void AlignToFootprint(GridOccupant occupant)
    {
        var t = occupant.transform;
        var size = RotatedSize(occupant.footprint, occupant.quarterTurns);
        t.rotation = Quaternion.Euler(0f, 90f * occupant.quarterTurns, 0f) * occupant.BaseRotation;

        // Prefab pivots vary, so align by render bounds.
        t.position = Vector3.zero;
        var target = FootprintToWorld(occupant.anchor, size);
        t.position = occupant.TryGetBounds(out var b)
            ? new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z)
            : target;
    }

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
        foreach (var c in blockedCells) DrawCell(c, CellFloorY(c));

        if (TryGetCornerArea(out var min, out var max, out var floorY))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3((min.x + max.x) / 2f, floorY, (min.y + max.y) / 2f), new Vector3(max.x - min.x, 0f, max.y - min.y));
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
