using UnityEngine;

public class Scr_FertilizerBin : MonoBehaviour
{
    [Header("Refill Settings")]
    [Min(1)]
    public int fertilizerPerRefill = 5;

    public void Refill(Scr_Inventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning("Cannot refill: inventory was not found.");
            return;
        }

        inventory.AddFertilizer(fertilizerPerRefill);

        Debug.Log(
            "Fertilizer bin used. Added " +
            fertilizerPerRefill + " fertilizer."
        );
    }
}