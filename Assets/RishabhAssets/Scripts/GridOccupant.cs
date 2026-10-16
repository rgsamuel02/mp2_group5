using UnityEngine;

/// <summary>
/// Marks an object that takes up cells on a <see cref="CafeGrid"/>. Added automatically by <see cref="CafeGrid.Spawn"/>.
/// </summary>
public class GridOccupant : MonoBehaviour
{
    public ShopItem item;
    [Tooltip("Cells covered at 0 rotation: X along local X, Y along local Z.")]
    public Vector2Int footprint = Vector2Int.one;

    // Set by CafeGrid when placed.
    [HideInInspector] public CafeGrid grid;
    [HideInInspector] public Vector2Int anchor;
    [HideInInspector] public int quarterTurns;
    [HideInInspector] public bool isPlaced;

    Quaternion? baseRotation;

    /// <summary>The prefab's own rotation, before any grid quarter turns are applied.</summary>
    public Quaternion BaseRotation => baseRotation ??= transform.rotation;

    void Awake()
    {
        _ = BaseRotation;
    }

    void OnDestroy()
    {
        if (grid) grid.Release(this);
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }
}
