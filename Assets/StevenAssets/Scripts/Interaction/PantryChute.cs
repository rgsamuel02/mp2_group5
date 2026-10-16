using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// The walk-in pantry chute: drop (or toss) loose ingredients into it to harvest them into the Pantry Ledger.
    /// Lives on the same GameObject as the chute's trigger collider.
    /// </summary>
    public class PantryChute : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var e = KitchenEntity.FromCollider(other);
            if (e == null || e.Consumed)
                return;
            if (e.kind != EntityKind.Ingredient && e.kind != EntityKind.Mushroom)
                return;
            var h = e.GetComponent<Harvestable>();
            if (h != null)
                h.TryHarvest();
        }
    }
}
