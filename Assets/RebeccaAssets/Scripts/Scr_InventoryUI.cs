using UnityEngine;
using TMPro;

public class Scr_InventoryUI : MonoBehaviour
{
    public Scr_Inventory inventory;
    public TMP_Text inventoryText;

    void Start()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Scr_Inventory>();
        }

        UpdateInventoryDisplay();
    }

    void Update()
    {
        UpdateInventoryDisplay();
    }

    void UpdateInventoryDisplay()
    {
        if (inventory == null || inventoryText == null)
            return;

        inventoryText.text =
            "GARDEN INVENTORY\n" +
            "Water: " + inventory.water + "\n" +
            "Fertilizer: " + inventory.fertilizer + "\n" +
            "Seeds: " + inventory.seeds + "\n" +
            "Tomatoes: " + inventory.tomatoes + "\n" +
            "Corn: " + inventory.corn + "\n" +
            "Flowers: " + inventory.flowers;
    }
}
