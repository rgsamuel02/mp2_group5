using TMPro;
using UnityEngine;

// Pays the player and raises Customer Satisfaction when a cup is drunk, and keeps the TV's tallies.
// Satisfaction gained = base - splats / 2, clamped to [min, base].
public class ServingRewards : MonoBehaviour
{
    public PlayerWallet wallet;
    public TMP_Text satisfactionText;
    public string satisfactionFormat = "Customer Satisfaction: {0}";
    public TMP_Text servedText;
    public string servedFormat = "Customers Served: {0}";
    public TMP_Text splatsSweptText;
    public string splatsSweptFormat = "Splats Swept: {0}";

    [Header("Money")]
    public int plainPayment = 200;
    public int flavoredPayment = 250;

    [Header("Satisfaction")]
    public int plainSatisfaction = 10;
    public int plainMinSatisfaction = 1;
    public int flavoredSatisfaction = 20;
    public int flavoredMinSatisfaction = 2;

    int satisfaction, customersServed, splatsSwept;

    void OnEnable()
    {
        Customer.AnyServed += OnServed;
        Splat.AnySwept += OnSwept;
        RefreshLabels();
    }

    void OnDisable()
    {
        Customer.AnyServed -= OnServed;
        Splat.AnySwept -= OnSwept;
    }

    void OnServed(Customer customer, CoffeeCup cup)
    {
        bool flavored = cup && cup.IsFlavored;
        wallet.Add(flavored ? flavoredPayment : plainPayment);

        int splats = FindObjectsByType<Splat>(FindObjectsSortMode.None).Length;
        int max = flavored ? flavoredSatisfaction : plainSatisfaction;
        int min = flavored ? flavoredMinSatisfaction : plainMinSatisfaction;
        satisfaction += Mathf.Clamp(max - splats / 2, min, max);
        customersServed++;
        RefreshLabels();
    }

    void OnSwept(Splat splat)
    {
        splatsSwept++;
        RefreshLabels();
    }

    void RefreshLabels()
    {
        if (satisfactionText) satisfactionText.text = string.Format(satisfactionFormat, satisfaction);
        if (servedText) servedText.text = string.Format(servedFormat, customersServed);
        if (splatsSweptText) splatsSweptText.text = string.Format(splatsSweptFormat, splatsSwept);
    }
}
