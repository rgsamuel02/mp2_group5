using UnityEngine;

public class Scr_GardenWallet : MonoBehaviour
{
    [SerializeField] private int startingBalanceCents = 0;

    public static Scr_GardenWallet Instance {private set; get;}

    private int balanceCents;
    private int totalEarnedFromSalesCents;

    public int BalanceCents => balanceCents;
    public int TotalEarnedFromSalesCents => totalEarnedFromSalesCents;

    public float BalanceDollars => balanceCents / 100f;
    public float TotalEarnedFromSalesDollars =>
        totalEarnedFromSalesCents / 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        balanceCents = Mathf.Max(0, startingBalanceCents);
    }

    public void AddSaleRevenue(int amountCents)
    {
        if (amountCents <= 0)
        {
            Debug.LogWarning(
                "Garden wallet: Sale revenue must be greater than zero."
            );
            return;
        }

        balanceCents += amountCents;
        totalEarnedFromSalesCents += amountCents;

        Debug.Log(
            $"Garden wallet: Earned ${amountCents / 100f:F2}. " +
            $"Balance: ${BalanceDollars:F2}. " +
            $"Total sales earnings: ${TotalEarnedFromSalesDollars:F2}."
        );
    }
}
