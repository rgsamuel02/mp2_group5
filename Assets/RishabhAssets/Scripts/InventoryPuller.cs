using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Point the right controller at an inventory row and grip to pull the item out of the wall into your hand.
public class InventoryPuller : MonoBehaviour
{
    public PlayerInventory inventory;
    public InventoryUI inventoryUI;
    public CafeGrid grid;
    public Material previewMaterial;
    [Tooltip("Found automatically (right-hand Near-Far Interactor) if empty.")]
    public NearFarInteractor interactor;
    public float spawnDistance = 0.2f;

    GrabbableItem pulling;
    bool isPulling;

    void Update()
    {
        if (!interactor && !TryFindInteractor()) return;

        // The pulled item is held by a manual interaction: end it on grip release, or if the item despawns in hand.
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
        var item = row.Item;
        if (!inventory.Remove(item)) return;

        var position = hit.worldPosition - hit.gameObject.transform.forward * spawnDistance;
        pulling = GrabbableItem.Create(item, position, grid, inventory, previewMaterial);
        isPulling = true;
        interactor.StartManualInteraction((IXRSelectInteractable)pulling.Grab);
    }

    bool TryFindInteractor()
    {
        foreach (var candidate in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
        {
            if (candidate.handedness != InteractorHandedness.Right) continue;
            interactor = candidate;
            return true;
        }
        return false;
    }
}
