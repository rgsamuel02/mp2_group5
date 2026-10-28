using System;
using Unity.VisualScripting;
using UnityEngine;

public class Scr_GardenEconomy : MonoBehaviour
{
    public Scr_GardenWallet wallet;
    public Scr_Inventory inventory;

    public int tomatoesToUnlockRestaurant = 30;
    public int coffeeBeansToUnlockCafe = 60;
    public int flowersToUnlockOrchestra = 5;

    public int tomatoPriceCents = 100;
    public int coffeeBeanPriceCents = 100;
    public int flowerPriceCents = 1000;

    public int moneyGoalCents = 30000;

    [SerializeField] private int tomatoesHarvested;
    [SerializeField] private int coffeeBeansHarvested;
    [SerializeField] private int flowersHarvested;

    [SerializeField] private bool restaurantUnlocked;
    [SerializeField] private bool cafeUnlocked;
    [SerializeField] private bool orchestraUnlocked;
    [SerializeField] private bool challengeCompleted;

    public bool RestaurantUnlocked => restaurantUnlocked;
    public bool CafeUnlocked => cafeUnlocked;
    public bool OrchestraUnlocked => orchestraUnlocked;
    public bool ChallengeCompleted => challengeCompleted;

    private void Awake()
    {
        if (wallet == null)
            wallet = Scr_GardenWallet.Instance;

        if (inventory == null)
            inventory = FindFirstObjectByType<Scr_Inventory>();
    }

    private void Start()
    {
        CheckWinCondition();
    }

    // Call this after a successful crop harvest.
    public void RegisterHarvest(GardenResource crop, int amount)
    {
        if (amount <= 0)
            return;

        switch (crop)
        {
            case GardenResource.Tomatoes:
                tomatoesHarvested += amount;
                break;

            case GardenResource.Corn:
                coffeeBeansHarvested += amount;
                break;

            case GardenResource.Flowers:
                flowersHarvested += amount;
                break;

            default:
                Debug.LogWarning(
                    "Scr_GardenEconomy: This resource is not a sellable crop."
                );
                return;
        }

        CheckUnlocks();
    }

    private void CheckUnlocks()
    {
        if (!restaurantUnlocked &&
            tomatoesHarvested >= tomatoesToUnlockRestaurant)
        {
            restaurantUnlocked = true;
        }

        if (!cafeUnlocked &&
            coffeeBeansHarvested >= coffeeBeansToUnlockCafe)
        {
            cafeUnlocked = true;
        }

        if (!orchestraUnlocked &&
            flowersHarvested >= flowersToUnlockOrchestra)
        {
            orchestraUnlocked = true;
        }
    }

    public bool SellCrop(GardenResource crop, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("Sale quantity must be greater than zero.");
            return false;
        }

        int pricePerCrop;
        bool buyerUnlocked;

        switch (crop)
        {
            case GardenResource.Tomatoes:
                pricePerCrop = tomatoPriceCents;
                buyerUnlocked = restaurantUnlocked;
                break;

            case GardenResource.Corn:
                pricePerCrop = coffeeBeanPriceCents;
                buyerUnlocked = cafeUnlocked;
                break;

            case GardenResource.Flowers:
                pricePerCrop = flowerPriceCents;
                buyerUnlocked = orchestraUnlocked;
                break;

            default:
                Debug.LogWarning("That resource cannot be sold.");
                return false;
        }

        if (!buyerUnlocked)
        {
            Debug.Log("This buyer is still locked.");
            return false;
        }

        if (inventory.GetAmount(crop) < quantity)
        {
            Debug.Log("Not enough " + crop + " to complete the sale.");
            return false;
        }

        int revenueCents = pricePerCrop * quantity;

        // Remove crops from the garden inventory.
        if (!inventory.UseResource(crop, quantity))
            return false;

        // Charge the cafe for coffee beans.
        if (crop == GardenResource.Corn)
        {
            float cafeCost = revenueCents / 100f;

            if (!PlayerWallet.Instance.TrySpend(cafeCost))
            {
                inventory.AddResource(crop, quantity);
                Debug.Log("The cafe cannot afford this purchase.");
                return false;
            }
        }

        // Charge the orchestra when selling flowers.
        if (crop == GardenResource.Flowers)
        {
            if (GameState.Instance == null ||
                !GameState.Instance.TrySpendMoney(revenueCents / 100f))
            {
                // Restore the flowers if the orchestra cannot pay.
                inventory.AddResource(crop, quantity);

                Debug.Log("The orchestra cannot afford this purchase.");
                return false;
            }
        }

        // Credit the garden wallet.
        wallet.AddSaleRevenue(revenueCents);

        Debug.Log(
            $"Sold {quantity} {crop} for ${revenueCents / 100f:F2}."
        );

        CheckWinCondition();
        return true;
    }

    private void CheckWinCondition()
    {
        if (challengeCompleted || wallet == null)
            return;

        if (wallet.TotalEarnedFromSalesCents >= moneyGoalCents)
        {
            challengeCompleted = true;

            Debug.Log(
                "GARDEN CHALLENGE COMPLETE! You earned $300 from sales!"
            );

            Scr_Victory victoryPopup =
                FindFirstObjectByType<Scr_Victory>();

            if (victoryPopup != null)
            {
                victoryPopup.ShowVictoryPopup();
            }
            else
            {
                Debug.LogWarning(
                    "Garden Victory Popup was not found in the scene."
                );
            }
        }
    }
}
