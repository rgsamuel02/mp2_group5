using UnityEngine;

// Something taking up cells on a CafeGrid.
public class GridOccupant : MonoBehaviour
{
    [Tooltip("Cells covered at 0 rotation: X along local X, Y along local Z.")]
    public Vector2Int footprint = Vector2Int.one;

    [HideInInspector] public CafeGrid grid;
    [HideInInspector] public Vector2Int anchor;
    [HideInInspector] public int quarterTurns;
    [HideInInspector] public bool isPlaced;

    Quaternion? baseRotation;

    // The prefab's rotation before any grid quarter turns.
    public Quaternion BaseRotation => baseRotation ??= transform.rotation;

    void Awake() => _ = BaseRotation;

    void OnDestroy()
    {
        if (grid) grid.Release(this);
    }

    public bool TryGetBounds(out Bounds bounds) => RendererBounds.TryGet(gameObject, out bounds);
}
