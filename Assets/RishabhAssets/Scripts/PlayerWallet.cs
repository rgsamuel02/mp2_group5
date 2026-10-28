using System;
using TMPro;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance {private set; get;}
    public int startingBalance = 500;
    public TMP_Text balanceText;
    public string balanceFormat = "Current Balance: {0}¥";

    public event Action BalanceChanged;

    int balance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Transfer the cafe's UI reference to the persistent wallet.
            if (balanceText != null)
            {
                Instance.balanceText = balanceText;
                Instance.RefreshLabel();
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        balance = startingBalance;
        RefreshLabel();
    }

    public bool CanAfford(int amount) => balance >= amount;

    public bool TrySpend(int amount)
    {
        if (amount < 0 || !CanAfford(amount)) return false;
        balance -= amount;
        Notify();
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        balance += amount;
        Notify();
    }

    void Notify()
    {
        RefreshLabel();
        BalanceChanged?.Invoke();
    }

    void RefreshLabel()
    {
        if (balanceText) balanceText.text = string.Format(balanceFormat, balance);
    }
}
