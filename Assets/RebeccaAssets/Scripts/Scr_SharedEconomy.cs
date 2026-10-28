using UnityEngine;

public class Scr_SharedEconomy : MonoBehaviour
{
    public static Scr_SharedEconomy Instance { get; private set; }

    [SerializeField] private int restaurantStartingDollars = 500;
    [SerializeField] private int cafeStartingDollars = 500;
    [SerializeField] private int orchestraStartingDollars = 500;

    public int RestaurantBalanceCents { get; private set; }
    public int CafeBalanceCents { get; private set; }
    public int OrchestraBalanceCents { get; private set; }

    private const string RestaurantKey = "Shared_RestaurantCents";
    private const string CafeKey = "Shared_CafeCents";
    private const string OrchestraKey = "Shared_OrchestraCents";
    private const string InitializedKey = "Shared_EconomyInitialized";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadBalances();
    }

    private void LoadBalances()
    {
        if (PlayerPrefs.GetInt(InitializedKey, 0) == 0)
        {
            RestaurantBalanceCents = Mathf.Max(0, restaurantStartingDollars) * 100;
            CafeBalanceCents = Mathf.Max(0, cafeStartingDollars) * 100;
            OrchestraBalanceCents = Mathf.Max(0, orchestraStartingDollars) * 100;

            SaveBalances();
            PlayerPrefs.SetInt(InitializedKey, 1);
            PlayerPrefs.Save();
            return;
        }

        RestaurantBalanceCents = PlayerPrefs.GetInt(RestaurantKey, 50000);
        CafeBalanceCents = PlayerPrefs.GetInt(CafeKey, 50000);
        OrchestraBalanceCents = PlayerPrefs.GetInt(OrchestraKey, 50000);
    }

    private void SaveBalances()
    {
        PlayerPrefs.SetInt(RestaurantKey, RestaurantBalanceCents);
        PlayerPrefs.SetInt(CafeKey, CafeBalanceCents);
        PlayerPrefs.SetInt(OrchestraKey, OrchestraBalanceCents);
        PlayerPrefs.Save();
    }

    public bool TryPurchaseFromBuyer(
        GardenResource crop, int quantity, int pricePerCropCents)
    {
        if (quantity <= 0 || pricePerCropCents <= 0)
            return false;

        long cost = (long)quantity * pricePerCropCents;

        if (cost > int.MaxValue)
            return false;

        int totalCostCents = (int)cost;

        switch (crop)
        {
            case GardenResource.Tomatoes:
                if (RestaurantBalanceCents < totalCostCents)
                    return false;

                RestaurantBalanceCents -= totalCostCents;
                break;

            case GardenResource.Corn:
                if (CafeBalanceCents < totalCostCents)
                    return false;

                CafeBalanceCents -= totalCostCents;
                break;

            case GardenResource.Flowers:
                if (OrchestraBalanceCents < totalCostCents)
                    return false;

                OrchestraBalanceCents -= totalCostCents;
                break;

            default:
                return false;
        }

        SaveBalances();

        Debug.Log(
            $"{crop} purchase: buyer paid ${totalCostCents / 100f:F2}."
        );

        return true;
    }

    public bool TrySpendFromCafe(int amountDollars)
    {
        if (amountDollars <= 0)
            return false;

        int cost = amountDollars * 100;

        if (CafeBalanceCents < cost)
            return false;

        CafeBalanceCents -= cost;
        SaveBalances();
        return true;
    }

    public void AddToCafe(int amountDollars)
    {
        if (amountDollars <= 0)
            return;

        CafeBalanceCents += amountDollars * 100;
        SaveBalances();
    }

    public bool TrySpendFromOrchestra(int amountDollars)
    {
        if (amountDollars <= 0)
            return false;

        int cost = amountDollars * 100;

        if (OrchestraBalanceCents < cost)
            return false;

        OrchestraBalanceCents -= cost;
        SaveBalances();
        return true;
    }

    public void AddToOrchestra(int amountDollars)
    {
        if (amountDollars <= 0)
            return;

        OrchestraBalanceCents += amountDollars * 100;
        SaveBalances();
    }

    public void AddToRestaurant(int amountDollars)
    {
        if (amountDollars <= 0)
            return;

        RestaurantBalanceCents += amountDollars * 100;
        SaveBalances();
    }

    public bool TrySpendFromRestaurant(int amountDollars)
    {
        if (amountDollars <= 0)
            return false;

        int cost = amountDollars * 100;

        if (RestaurantBalanceCents < cost)
            return false;

        RestaurantBalanceCents -= cost;
        SaveBalances();
        return true;
    }

    // Use this only when intentionally resetting the demo economy.
    [ContextMenu("Reset Shared Economy")]
    private void ResetSharedEconomy()
    {
        RestaurantBalanceCents = Mathf.Max(0, restaurantStartingDollars) * 100;
        CafeBalanceCents = Mathf.Max(0, cafeStartingDollars) * 100;
        OrchestraBalanceCents = Mathf.Max(0, orchestraStartingDollars) * 100;

        PlayerPrefs.SetInt(InitializedKey, 1);
        SaveBalances();

        Debug.Log("Shared economy balances reset.");
    }
}
