using System.Collections.Generic;
using UnityEngine;

// Despawns a RebeccaCoffeePlant when it touches the intake trigger and spawns a BeanPack at BeanPackAnchor.
public class PlantToBeanPack : MonoBehaviour
{
    public Transform beanPackAnchor;
    public ShopItem beanPackItem;
    [Tooltip("A pack further than this from the anchor no longer occupies the slot.")]
    public float slotRadius = 0.05f;

    GridOccupant occupant;
    MoveLock moveLock;
    GrabbableItem currentPack;
    readonly HashSet<RebeccaCoffeePlant> consumed = new HashSet<RebeccaCoffeePlant>();

    void Awake()
    {
        if (!beanPackAnchor) beanPackAnchor = transform.Find("BeanPackAnchor");
        occupant = GetComponent<GridOccupant>();
        // The grab collider encloses the slot and would block the ray to the pack, so it's off while locked.
        moveLock = MoveLock.For(gameObject, true);
    }

    void Update()
    {
        if (currentPack && (currentPack.Grab.isSelected || !beanPackAnchor || (currentPack.transform.position - beanPackAnchor.position).sqrMagnitude > slotRadius * slotRadius))
            currentPack = null;

        moveLock.SetLocked(currentPack != null);
    }

    void OnTriggerEnter(Collider other)
    {
        if (occupant && !occupant.isPlaced) return;

        var plant = other.GetComponentInParent<RebeccaCoffeePlant>();
        if (!plant || !consumed.Add(plant)) return;

        Destroy(plant.gameObject);
        SpawnBeanPack();
    }

    void SpawnBeanPack()
    {
        if (!beanPackItem || !beanPackAnchor) return;

        var puller = FindFirstObjectByType<InventoryPuller>();
        var grid = occupant && occupant.grid ? occupant.grid : (puller ? puller.grid : FindFirstObjectByType<CafeGrid>());
        var inventory = puller ? puller.inventory : FindFirstObjectByType<PlayerInventory>();
        var previewMaterial = puller ? puller.previewMaterial : null;

        currentPack = GrabbableItem.Create(beanPackItem, beanPackAnchor.position, grid, inventory, previewMaterial);
        currentPack.transform.SetPositionAndRotation(beanPackAnchor.position, beanPackAnchor.rotation);
        moveLock.SetLocked(true);
        TutorialPopup.NotifyUsed(this);
    }
}
