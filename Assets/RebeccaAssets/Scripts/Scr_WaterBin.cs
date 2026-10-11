using UnityEngine;

public class Scr_WaterBin : MonoBehaviour
{
    [Header("Refill Settings")]
    [Min(1)]
    public int waterPerRefill = 5;

    public void Refill(Scr_Inventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning("Cannot refill: inventory was not found.");
            return;
        }

        inventory.AddWater(waterPerRefill);

        Debug.Log(
            "Water bin used. Added " +
            waterPerRefill + " water."
        );
    }
}