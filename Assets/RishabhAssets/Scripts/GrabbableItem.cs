using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A purchased item that can be held and dropped onto the grid, with a translucent box previewing where it will snap.
public class GrabbableItem : MonoBehaviour
{
    public ShopItem item;
    public CafeGrid grid;
    public PlayerInventory inventory;
    public Material previewMaterial;
    public Color validColor = new Color(0.2f, 1f, 0.3f, 0.3f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.3f);

    public XRGrabInteractable Grab { get; private set; }

    GridOccupant occupant;
    Rigidbody body;
    Renderer preview;
    MaterialPropertyBlock previewProps;

    bool hasTarget, targetValid;
    Vector2Int targetAnchor;
    int targetTurns;

    bool wasPlaced;
    Vector2Int previousAnchor;
    int previousTurns;

    public static GrabbableItem Create(ShopItem item, Vector3 position, CafeGrid grid, PlayerInventory inventory, Material previewMaterial)
    {
        var go = Instantiate(item.prefab, position, item.prefab.transform.rotation, grid ? grid.transform : null);

        var occupant = go.GetComponent<GridOccupant>();
        if (!occupant) occupant = go.AddComponent<GridOccupant>();
        occupant.footprint = item.footprint;

        // XRGrabInteractable gathers colliders in Awake, and needs a solid (non-trigger) one.
        if (!System.Array.Exists(go.GetComponentsInChildren<Collider>(), c => !c.isTrigger)) AddBoundsCollider(go, occupant);

        var body = go.GetComponent<Rigidbody>();
        if (!body) body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        var grab = go.GetComponent<XRGrabInteractable>();
        if (!grab) grab = go.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = false;
        grab.useDynamicAttach = true;
        grab.retainTransformParent = true;

        var g = go.AddComponent<GrabbableItem>();
        g.item = item;
        g.grid = grid;
        g.inventory = inventory;
        g.previewMaterial = previewMaterial;
        return g;
    }

    static void AddBoundsCollider(GameObject go, GridOccupant occupant)
    {
        var box = go.AddComponent<BoxCollider>();
        if (!occupant.TryGetBounds(out var world)) return;
        var t = go.transform;
        var s = t.lossyScale;
        box.center = t.InverseTransformPoint(world.center);
        box.size = new Vector3(world.size.x / Mathf.Abs(s.x), world.size.y / Mathf.Abs(s.y), world.size.z / Mathf.Abs(s.z));
    }

    void Awake()
    {
        occupant = GetComponent<GridOccupant>();
        body = GetComponent<Rigidbody>();
        Grab = GetComponent<XRGrabInteractable>();
        Grab.selectEntered.AddListener(OnGrabbed);
        Grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (Grab)
        {
            Grab.selectEntered.RemoveListener(OnGrabbed);
            Grab.selectExited.RemoveListener(OnReleased);
        }
        if (preview) Destroy(preview.gameObject);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        wasPlaced = occupant.isPlaced;
        previousAnchor = occupant.anchor;
        previousTurns = occupant.quarterTurns;
        if (grid) grid.Release(occupant);

        body.isKinematic = true;
        body.useGravity = false;
    }

    void Update()
    {
        if (!Grab.isSelected || !item.gridPlaceable || !grid)
        {
            hasTarget = false;
            SetPreviewVisible(false);
            return;
        }

        // Held yaw snaps to the nearest quarter turn.
        var relative = transform.rotation * Quaternion.Inverse(occupant.BaseRotation);
        targetTurns = ((Mathf.RoundToInt(relative.eulerAngles.y / 90f) % 4) + 4) % 4;
        var size = CafeGrid.RotatedSize(item.footprint, targetTurns);

        var center = occupant.TryGetBounds(out var b) ? b.center : transform.position;
        targetValid = grid.TrySnap(center, size, out targetAnchor, out var snapped);

        hasTarget = false;
        foreach (var c in grid.FootprintCells(targetAnchor, size))
            if (grid.InBounds(c)) { hasTarget = true; break; }

        SetPreviewVisible(hasTarget);
        if (hasTarget)
        {
            float height = Mathf.Max(b.size.y, grid.cellSize * 0.25f);
            preview.transform.SetPositionAndRotation(snapped + Vector3.up * height * 0.5f, Quaternion.identity);
            preview.transform.localScale = new Vector3(size.x * grid.cellSize, height, size.y * grid.cellSize);
            previewProps.SetColor("_BaseColor", targetValid ? validColor : invalidColor);
            preview.SetPropertyBlock(previewProps);
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        SetPreviewVisible(false);

        if (!item.gridPlaceable)
        {
            body.isKinematic = false;
            body.useGravity = true;
            return;
        }

        if (hasTarget && targetValid && grid.Occupy(occupant, targetAnchor, targetTurns))
            grid.AlignToFootprint(occupant);
        else if (wasPlaced && grid.Occupy(occupant, previousAnchor, previousTurns))
            grid.AlignToFootprint(occupant);
        else
        {
            // Never placed and dropped somewhere invalid: back into the wall.
            if (inventory) inventory.Add(item);
            Destroy(gameObject);
        }
    }

    void SetPreviewVisible(bool visible)
    {
        if (visible && !preview)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name + " Placement Preview";
            Destroy(go.GetComponent<Collider>());
            preview = go.GetComponent<Renderer>();
            if (previewMaterial) preview.sharedMaterial = previewMaterial;
            preview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            previewProps = new MaterialPropertyBlock();
        }
        if (preview) preview.gameObject.SetActive(visible);
    }
}
