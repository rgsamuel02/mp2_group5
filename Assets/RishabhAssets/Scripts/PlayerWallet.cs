using System;
using TMPro;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int startingBalance = 500;
    public TMP_Text balanceText;
    public string balanceFormat = "Current Balance: {0}¥";

    public event Action BalanceChanged;

    int balance;

    public int Balance => balance;

    void Awake()
    {
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
