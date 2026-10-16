using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A purchased item that can be held with an XR controller and dropped onto the grid.
/// While held, a translucent box shows the footprint it will snap to (green = valid, red = blocked).
/// </summary>
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

    // Where it sat before being picked up, so an invalid drop can put it back.
    bool wasPlaced;
    Vector2Int previousAnchor;
    int previousTurns;

    /// <summary>Adds the physics, collider and grab components an item prefab needs to be held.</summary>
    public static GrabbableItem Create(ShopItem item, Vector3 position, CafeGrid grid, PlayerInventory inventory, Material previewMaterial)
    {
        var go = Instantiate(item.prefab, position, item.prefab.transform.rotation, grid ? grid.transform : null);

        var occupant = go.GetComponent<GridOccupant>();
        if (!occupant) occupant = go.AddComponent<GridOccupant>();
        occupant.item = item;
        occupant.footprint = item.footprint;

        // XRGrabInteractable collects colliders in Awake, so they must exist before it is added.
        // Trigger volumes (e.g. the coffee machine's bean intake) don't count: grabbing needs a solid collider.
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
        if (!occupant.TryGetBounds(out var world)) { go.AddComponent<BoxCollider>(); return; }
        var box = go.AddComponent<BoxCollider>();
        var t = go.transform;
        box.center = t.InverseTransformPoint(world.center);
        var s = t.lossyScale;
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

        // Snap the held yaw to the nearest quarter turn relative to the prefab's own rotation.
        var relative = transform.rotation * Quaternion.Inverse(occupant.BaseRotation);
        targetTurns = ((Mathf.RoundToInt(relative.eulerAngles.y / 90f) % 4) + 4) % 4;
        var size = CafeGrid.RotatedSize(item.footprint, targetTurns);

        var center = occupant.TryGetBounds(out var b) ? b.center : transform.position;
        targetValid = grid.TrySnap(center, size, out targetAnchor, out var snapped);

        // Only show the box while the footprint is at least partly over the grid.
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
            // Tools like the Broom just drop where they're let go.
            body.isKinematic = false;
            body.useGravity = true;
            return;
        }

        if (hasTarget && targetValid && grid.Occupy(occupant, targetAnchor, targetTurns))
        {
            grid.AlignToFootprint(occupant);
        }
        else if (wasPlaced && grid.Occupy(occupant, previousAnchor, previousTurns))
        {
            grid.AlignToFootprint(occupant);
        }
        else
        {
            // Never placed and dropped somewhere invalid: send it back into the wall.
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
