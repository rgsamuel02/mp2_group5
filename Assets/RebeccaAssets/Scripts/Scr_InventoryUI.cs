
using UnityEngine;
using TMPro;

public class Scr_InventoryUI : MonoBehaviour
{
    public Scr_Inventory inventory;
    public TMP_Text inventoryText;

    [Header("Font Settings")]
    public TMP_FontAsset inventoryFont;

    private string previousInventoryText = "";

    void Start()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Scr_Inventory>();
        }

        if (inventoryText == null)
        {
            Debug.LogError("Inventory Text is not assigned.");
            return;
        }

        // Apply the chosen font once.
        if (inventoryFont != null)
        {
            inventoryText.font = inventoryFont;
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

        string newText =
            "GARDEN INVENTORY\n" +
            "Water: " + inventory.water + "\n" +
            "Fertilizer: " + inventory.fertilizer + "\n" +
            "Seeds: " + inventory.seeds + "\n" +
            "Tomatoes: " + inventory.tomatoes + "\n" +
            "Corn: " + inventory.corn + "\n" +
            "Flowers: " + inventory.flowers;

        // Only update the text when its contents actually change.
        if (newText != previousInventoryText)
        {
            inventoryText.text = newText;
            previousInventoryText = newText;
        }
    }
}
