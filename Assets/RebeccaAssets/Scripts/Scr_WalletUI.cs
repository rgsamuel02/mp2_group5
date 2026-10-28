using TMPro;
using UnityEngine;

public class Scr_WalletUI : MonoBehaviour
{
    [Header("References")]
    public Scr_GardenWallet wallet;
    public Scr_GardenEconomy economy;
    public TMP_Text walletText;

    private void Start()
    {
        //idk
    }

    
    private void Update()
    {
        bool canSell =
            economy.RestaurantUnlocked ||
            economy.CafeUnlocked ||
            economy.OrchestraUnlocked;

        if (!canSell)
        {
            walletText.text = $"Wallet: ${wallet.BalanceDollars:F2}";
            return;
        }

        walletText.text =
            $"Wallet: ${wallet.TotalEarnedFromSalesDollars:F2} / $300.00";
    }


    private string GetStatus(bool unlocked)
    {
        return unlocked ? "Unlocked" : "Locked";
    }
}
