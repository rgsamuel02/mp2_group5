
using UnityEngine;

public class Scr_GardenSellingStation : MonoBehaviour
{
    [Header("Buyer Settings")]
    public GardenResource cropToSell = GardenResource.Tomatoes;

    [Header("References")]
    public Scr_GardenEconomy economy;

    private void Awake()
    {
        if (economy == null)
        {
            economy = FindFirstObjectByType<Scr_GardenEconomy>();
        }
    }

    public void SellOneBatch()
    {
        Debug.Log(
            $"Selling station interacted with: {gameObject.name}"
        );

        if (economy == null)
        {
            economy = FindFirstObjectByType<Scr_GardenEconomy>();
        }

        if (economy == null)
        {
            Debug.LogError(
                "Selling station: GardenEconomyManager was not found."
            );
            return;
        }

        if (economy.inventory == null)
        {
            Debug.LogError(
                "Selling station: Inventory is not assigned."
            );
            return;
        }

        int available =
            economy.inventory.GetAmount(cropToSell);

        Debug.Log(
            $"Station sells {cropToSell}. Available: {available}"
        );

        bool sold = economy.SellCrop(cropToSell, available);

        if (sold)
        {
            Debug.Log(
                $"Sale completed at {gameObject.name}."
            );
        }
    }
}
