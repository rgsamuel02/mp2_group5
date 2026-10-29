using System;
using Unity.VisualScripting;
using UnityEngine;
using Steven.MP2A;

public class Scr_GardenEconomy : MonoBehaviour
{
    public PlayerResources restaurantResources;

    public Scr_GardenWallet wallet;
    public Scr_Inventory inventory;
    public Scr_Builder builder;

    public int tomatoesToUnlockRestaurant = 30;
    public int coffeeBeansToUnlockCafe = 60;
    public int flowersToUnlockOrchestra = 5;

    public int tomatoPriceCents = 100;
    public int coffeeBeanPriceCents = 100;
    public int flowerPriceCents = 1000;

    public int moneyGoalCents = 30000;

    [SerializeField] private int tomatoesHarvested = 15;
    [SerializeField] private int coffeeBeansHarvested = 60;
    [SerializeField] private int flowersHarvested = 5;

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
            restaurantUnlocked = true;

        if (!cafeUnlocked &&
            coffeeBeansHarvested >= coffeeBeansToUnlockCafe)
            cafeUnlocked = true;

        if (!orchestraUnlocked &&
            flowersHarvested >= flowersToUnlockOrchestra && GameState.Instance != null && GameState.Instance.UnlockedGarden())
            orchestraUnlocked = true;
    }

    public bool SellCrop(GardenResource crop, int quantity)
    {
        if (quantity <= 0)
        {
            builder.ShowResourcePopup("Sale quantity must be greater than zero.");
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
            if (crop == GardenResource.Flowers && (GameState.Instance == null || !GameState.Instance.UnlockedGarden()))
                builder.ShowResourcePopup("Unlock the garden in the orchestra simulation.");
            else
                builder.ShowResourcePopup("This buyer is still locked.");

            return false;
        }

        if (inventory.GetAmount(crop) < quantity)
        {
            builder.ShowResourcePopup("Not enough crops to complete the sale.");
            return false;
        }

        int revenueCents = pricePerCrop * quantity;

        // Remove crops from the garden inventory.
        if (!inventory.UseResource(crop, quantity))
            return false;

        // Charge the cafe for coffee beans.
        if (crop == GardenResource.Corn)
        {
            int cafeCost = revenueCents / 100;

            if (!PlayerWallet.Instance.TrySpend(cafeCost))
            {
                inventory.AddResource(crop, quantity);
                builder.ShowResourcePopup("The cafe cannot afford this purchase.");
                return false;
            }
        }

        // Charge the orchestra when selling flowers.
        if (crop == GardenResource.Flowers)
        {
            if (GameState.Instance == null || !GameState.Instance.TrySpendMoney(revenueCents / 100f))
            {
                // Restore the flowers if the orchestra cannot pay.
                inventory.AddResource(crop, quantity);

                builder.ShowResourcePopup("The orchestra cannot afford this purchase.");
                return false;
            }
        }

        // Charge the restaurant when selling tomatoes.
        if (crop == GardenResource.Tomatoes)
        {
            float restaurantCost = revenueCents / 100f;

            if (!restaurantResources.TrySpend(ResourceId.Gold, restaurantCost))
            {
                // Refund the tomatoes if the restaurant cannot afford them.
                inventory.AddResource(crop, quantity);

                builder.ShowResourcePopup("The restaurant cannot afford this purchase.");
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
                victoryPopup.ShowVictoryPopup();
            else
            {
                Debug.LogWarning(
                    "Garden Victory Popup was not found in the scene."
                );
            }
        }
    }

    public GardenEconomySaveData CaptureSaveData()
    {
        return new GardenEconomySaveData
        {
            tomatoesHarvested = tomatoesHarvested,
            coffeeBeansHarvested = coffeeBeansHarvested,
            flowersHarvested = flowersHarvested,

            restaurantUnlocked = restaurantUnlocked,
            cafeUnlocked = cafeUnlocked,
            orchestraUnlocked = orchestraUnlocked,
            challengeCompleted = challengeCompleted
        };
    }

    public void RestoreSaveData(GardenEconomySaveData data)
    {
        tomatoesHarvested = data.tomatoesHarvested;
        coffeeBeansHarvested = data.coffeeBeansHarvested;
        flowersHarvested = data.flowersHarvested;

        restaurantUnlocked = data.restaurantUnlocked;
        cafeUnlocked = data.cafeUnlocked;
        orchestraUnlocked = data.orchestraUnlocked;
        challengeCompleted = data.challengeCompleted;

        Debug.Log("Garden economy progress restored.");
    }
}
