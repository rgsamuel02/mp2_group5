using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Pays the player and raises Customer Satisfaction whenever a customer finishes a cup.
/// Flavored cups pay more; mess on the floor (live <see cref="Splat"/>s) lowers the satisfaction gained.
/// </summary>
public class ServingRewards : MonoBehaviour
{
    public PlayerWallet wallet;
    public TMP_Text satisfactionText;
    public string satisfactionFormat = "Customer Satisfaction: {0}";

    [Header("Money")]
    public int plainPayment = 200;
    public int flavoredPayment = 250;

    [Header("Satisfaction (base - splats / 2, clamped)")]
    public int plainSatisfaction = 10;
    public int plainMinSatisfaction = 1;
    public int flavoredSatisfaction = 20;
    public int flavoredMinSatisfaction = 2;

    public int Satisfaction { get; private set; }
    public event Action<int> SatisfactionChanged;

    void OnEnable()
    {
        Customer.AnyServed += OnServed;
        RefreshLabel();
    }

    void OnDisable()
    {
        Customer.AnyServed -= OnServed;
    }

    void OnServed(Customer customer, CoffeeCup cup)
    {
        bool flavored = cup && cup.IsFlavored;
        wallet.Add(flavored ? flavoredPayment : plainPayment);
        AddSatisfaction(SatisfactionFor(flavored));
    }

    /// <summary>Satisfaction earned for one cup given how many splats are currently on the floor.</summary>
    public int SatisfactionFor(bool flavored)
    {
        int splats = FindObjectsByType<Splat>(FindObjectsSortMode.None).Length;
        int max = flavored ? flavoredSatisfaction : plainSatisfaction;
        int min = flavored ? flavoredMinSatisfaction : plainMinSatisfaction;
        return Mathf.Clamp(max - splats / 2, min, max);
    }

    public void AddSatisfaction(int amount)
    {
        Satisfaction += amount;
        RefreshLabel();
        SatisfactionChanged?.Invoke(Satisfaction);
    }

    void RefreshLabel()
    {
        if (satisfactionText) satisfactionText.text = string.Format(satisfactionFormat, Satisfaction);
    }
}
