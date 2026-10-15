using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Holds the player's money and keeps the balance label up to date.
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    public int startingBalance = 500;
    public TMP_Text balanceText;
    public string balanceFormat = "Current Balance: {0}¥";

    public int Balance { get; private set; }
    public event Action<int> BalanceChanged;

    void Awake()
    {
        Balance = startingBalance;
        RefreshLabel();
    }

    public bool CanAfford(int amount) => Balance >= amount;

    public bool TrySpend(int amount)
    {
        if (amount < 0 || !CanAfford(amount)) return false;
        Balance -= amount;
        Notify();
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
        Notify();
    }

    void Notify()
    {
        RefreshLabel();
        BalanceChanged?.Invoke(Balance);
    }

    void RefreshLabel()
    {
        if (balanceText) balanceText.text = string.Format(balanceFormat, Balance);
    }
}
