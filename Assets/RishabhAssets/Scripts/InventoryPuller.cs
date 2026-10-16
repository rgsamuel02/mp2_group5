using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Lets the right controller pull items out of the wall: point at an inventory row and press grip.
/// The item spawns at the screen and stays held until grip is released.
/// </summary>
public class InventoryPuller : MonoBehaviour
{
    public PlayerInventory inventory;
    public InventoryUI inventoryUI;
    public CafeGrid grid;
    public Material previewMaterial;

    [Tooltip("Controller used to pull items out. Found automatically (right-hand Near-Far Interactor) if left empty.")]
    public NearFarInteractor interactor;

    [Tooltip("How far in front of the screen the pulled item appears.")]
    public float spawnDistance = 0.2f;

    GrabbableItem pulling;
    bool isPulling;

    void Update()
    {
        if (!interactor && !TryFindInteractor()) return;

        // A pulled item is held by a manual interaction, so end it ourselves when grip is let go
        // (or when the item despawns in hand, e.g. a bean pack using its last scoop).
        if (isPulling)
        {
            if (!pulling || !interactor.logicalSelectState.active)
            {
                if (interactor.isPerformingManualInteraction) interactor.EndManualInteraction();
                pulling = null;
                isPulling = false;
            }
            return;
        }

        if (!interactor.logicalSelectState.wasPerformedThisFrame || interactor.hasSelection) return;
        if (!interactor.TryGetCurrentUIRaycastResult(out var hit) || !hit.gameObject) return;

        var row = hit.gameObject.GetComponentInParent<ItemRow>();
        if (!row || !inventoryUI.Owns(row) || !row.Item || !row.Item.prefab) return;
        if (!inventory.Remove(row.Item)) return;

        // Out of the wall means toward the viewer, i.e. against the canvas's forward.
        var position = hit.worldPosition - hit.gameObject.transform.forward * spawnDistance;
        pulling = GrabbableItem.Create(row.Item, position, grid, inventory, previewMaterial);
        isPulling = true;
        interactor.StartManualInteraction((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)pulling.Grab);
    }

    bool TryFindInteractor()
    {
        foreach (var candidate in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
        {
            if (candidate.handedness == InteractorHandedness.Right)
            {
                interactor = candidate;
                return true;
            }
        }
        return false;
    }
}
