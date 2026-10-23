using UnityEngine;

public class Scr_GardenSellingStation : MonoBehaviour
{
    public GardenResource cropToSell = GardenResource.Tomatoes;

    public Scr_GardenEconomy economy;

    // Call this method when the player interacts with this buyer.
    public void SellOneBatch()
    {
        if (economy == null)
        {
            economy = FindFirstObjectByType<Scr_GardenEconomy>();
        }

        int available = economy.inventory.GetAmount(cropToSell);

        if (available <= 0)
        {
            Debug.Log("You don't have any " + cropToSell + " to sell.");
            return;
        }

        bool sold = economy.SellCrop(cropToSell, available);

        if (sold)
        {
            Debug.Log(
                "Sale completed at " + gameObject.name +
                ". Sold " + available + " " + cropToSell + "."
            );
        }
    }
}