using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector + Scene view tools for <see cref="CafeGrid"/>: paint blocked / raised cells by clicking them.
/// </summary>
[CustomEditor(typeof(CafeGrid))]
public class CafeGridEditor : Editor
{
    enum PaintMode { Off, Blocked, Raised }

    static PaintMode mode;
    static bool showLabels = true;

    public override void OnInspectorGUI()
    {
        var grid = (CafeGrid)target;
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid", $"{grid.Columns} x {grid.Rows} cells, floor Y {grid.FloorY:F2}", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene View Painting", EditorStyles.boldLabel);
        mode = (PaintMode)GUILayout.Toolbar((int)mode, new[] { "Off", "Paint Blocked", "Paint Raised" });
        showLabels = EditorGUILayout.Toggle("Show Cell Coordinates", showLabels);
        if (mode != PaintMode.Off)
            EditorGUILayout.HelpBox("Click (or drag) over cells in the Scene view to toggle them. Hold Shift to only clear.", MessageType.Info);

        if (GUILayout.Button("Clear Blocked Cells")) { Undo.RecordObject(grid, "Clear Blocked Cells"); grid.blockedCells.Clear(); }
        if (GUILayout.Button("Clear Raised Cells")) { Undo.RecordObject(grid, "Clear Raised Cells"); grid.raisedCells.Clear(); }
        SceneView.RepaintAll();
    }

    void OnDisable()
    {
        mode = PaintMode.Off;
    }

    // Cells toggled during the current drag, so dragging doesn't flip a cell back and forth.
    readonly System.Collections.Generic.HashSet<Vector2Int> strokeCells = new System.Collections.Generic.HashSet<Vector2Int>();
    bool strokeAdds;

    void OnSceneGUI()
    {
        var grid = (CafeGrid)target;

        if (showLabels) DrawLabels(grid);
        if (mode == PaintMode.Off) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        var e = Event.current;
        if (e.alt || e.button != 0) return; // leave orbiting / panning alone

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && TryGetHoveredCell(grid, e.mousePosition, out var cell))
        {
            var list = mode == PaintMode.Blocked ? grid.blockedCells : grid.raisedCells;
            if (e.type == EventType.MouseDown)
            {
                strokeCells.Clear();
                strokeAdds = !e.shift && !list.Contains(cell);
            }
            if (strokeCells.Add(cell))
            {
                Undo.RecordObject(grid, mode == PaintMode.Blocked ? "Paint Blocked Cell" : "Paint Raised Cell");
                if (strokeAdds) { if (!list.Contains(cell)) list.Add(cell); }
                else list.Remove(cell);
                EditorUtility.SetDirty(grid);
            }
            e.Use();
        }

        if (e.type == EventType.MouseMove || e.type == EventType.Repaint)
        {
            if (TryGetHoveredCell(grid, e.mousePosition, out var hover))
            {
                var c = grid.CellToWorld(hover);
                float s = grid.cellSize;
                Handles.color = mode == PaintMode.Blocked ? Color.red : Color.cyan;
                Handles.DrawWireCube(c + Vector3.up, new Vector3(s, 2f, s));
            }
            if (e.type == EventType.MouseMove) SceneView.RepaintAll();
        }
    }

    static bool TryGetHoveredCell(CafeGrid grid, Vector2 mousePos, out Vector2Int cell)
    {
        cell = default;
        var ray = HandleUtility.GUIPointToWorldRay(mousePos);
        // Try the raised plane first so counter cells can be picked where the counter is.
        foreach (float y in new[] { grid.raisedFloorY, grid.FloorY })
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
            if (!plane.Raycast(ray, out float dist)) continue;
            var c = grid.WorldToCell(ray.GetPoint(dist));
            if (!grid.InBounds(c)) continue;
            // In Blocked mode only raised cells live on the raised plane; in Raised mode you pick on the counter top.
            if (mode != PaintMode.Raised && y == grid.raisedFloorY && y != grid.FloorY && !grid.IsRaised(c)) continue;
            cell = c;
            return true;
        }
        return false;
    }

    static void DrawLabels(CafeGrid grid)
    {
        var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        for (int x = 0; x < grid.Columns; x++)
            for (int y = 0; y < grid.Rows; y++)
            {
                var cell = new Vector2Int(x, y);
                Handles.Label(grid.CellToWorld(cell) + Vector3.up * 2f, $"{x},{y}", style);
            }
    }
}
