using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Scene-view painting of raised cells and cell coordinate labels for CafeGrid.
[CustomEditor(typeof(CafeGrid))]
public class CafeGridEditor : Editor
{
    static bool painting;
    static bool showLabels = true;

    // Cells toggled this drag, so dragging doesn't flip a cell back and forth.
    readonly HashSet<Vector2Int> strokeCells = new HashSet<Vector2Int>();
    bool strokeAdds;

    public override void OnInspectorGUI()
    {
        var grid = (CafeGrid)target;
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid", $"{grid.Columns} x {grid.Rows} cells, floor Y {grid.FloorY:F2}", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene View Painting", EditorStyles.boldLabel);
        painting = GUILayout.Toolbar(painting ? 1 : 0, new[] { "Off", "Paint Raised" }) == 1;
        showLabels = EditorGUILayout.Toggle("Show Cell Coordinates", showLabels);
        if (painting)
            EditorGUILayout.HelpBox("Click (or drag) over cells in the Scene view to toggle them. Hold Shift to only clear.", MessageType.Info);

        if (GUILayout.Button("Clear Raised Cells")) { Undo.RecordObject(grid, "Clear Raised Cells"); grid.raisedCells.Clear(); }
        SceneView.RepaintAll();
    }

    void OnDisable() => painting = false;

    void OnSceneGUI()
    {
        var grid = (CafeGrid)target;
        if (showLabels) DrawLabels(grid);
        if (!painting) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        var e = Event.current;
        if (e.alt || e.button != 0) return;

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && TryGetHoveredCell(grid, e.mousePosition, out var cell))
        {
            var list = grid.raisedCells;
            if (e.type == EventType.MouseDown)
            {
                strokeCells.Clear();
                strokeAdds = !e.shift && !list.Contains(cell);
            }
            if (strokeCells.Add(cell))
            {
                Undo.RecordObject(grid, "Paint Raised Cell");
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
                float s = grid.cellSize;
                Handles.color = Color.cyan;
                Handles.DrawWireCube(grid.CellToWorld(hover) + Vector3.up * s * 0.02f, new Vector3(s, s * 0.04f, s));
            }
            if (e.type == EventType.MouseMove) SceneView.RepaintAll();
        }
    }

    // Picks on the counter-top plane so cells line up with the counter being clicked.
    static bool TryGetHoveredCell(CafeGrid grid, Vector2 mousePos, out Vector2Int cell)
    {
        cell = default;
        var ray = HandleUtility.GUIPointToWorldRay(mousePos);
        var plane = new Plane(Vector3.up, new Vector3(0f, grid.raisedFloorY, 0f));
        if (!plane.Raycast(ray, out float dist)) return false;
        cell = grid.WorldToCell(ray.GetPoint(dist));
        return grid.InBounds(cell);
    }

    static void DrawLabels(CafeGrid grid)
    {
        var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        for (int x = 0; x < grid.Columns; x++)
            for (int y = 0; y < grid.Rows; y++)
                Handles.Label(grid.CellToWorld(new Vector2Int(x, y)) + Vector3.up * grid.cellSize * 0.04f, $"{x},{y}", style);
    }
}
