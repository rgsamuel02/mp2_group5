using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places furniture on the grid when the game starts, so those cells are already taken.
/// </summary>
[RequireComponent(typeof(CafeGrid))]
public class StartingFurniture : MonoBehaviour
{
    [Serializable]
    public class Placement
    {
        public ShopItem item;
        [Tooltip("Minimum (lowest X/Z) cell of the footprint.")]
        public Vector2Int anchor;
        [Range(0, 3)] public int quarterTurns;
    }

    public List<Placement> placements = new List<Placement>();
    public Color previewColor = new Color(0.2f, 1f, 0.3f, 0.8f);

    void Start()
    {
        var grid = GetComponent<CafeGrid>();
        foreach (var p in placements)
        {
            if (!grid.Spawn(p.item, p.anchor, p.quarterTurns))
                Debug.LogWarning($"StartingFurniture: couldn't place {(p.item ? p.item.displayName : "<none>")} at {p.anchor}", this);
        }
    }

    // Outline each footprint in the editor so placements can be tuned without entering Play mode.
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        var grid = GetComponent<CafeGrid>();
        Gizmos.color = previewColor;
        foreach (var p in placements)
        {
            if (!p.item) continue;
            var size = CafeGrid.RotatedSize(p.item.footprint, p.quarterTurns);
            var center = grid.FootprintToWorld(p.anchor, size);
            Gizmos.DrawWireCube(center + Vector3.up * 2f, new Vector3(size.x * grid.cellSize, 4f, size.y * grid.cellSize) * 0.98f);
        }
    }
}
