using TMPro;
using UnityEngine;

public class GameState : MonoBehaviour
{
    // The static reference that other scripts call
    public static GameState Instance { get; private set; }

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: Keeps it alive across scenes
    }

    static float enjoyment = 1f;
    public const float payRate = 10; // how long in seconds before a musician gives money
    float money = 0.0f;


    public TextMeshProUGUI moneyText;

    public float GetEnjoyment()
    {
        return enjoyment;
    }

    public void AddPay(float income)
    {
        money += income;
    }

    public static float GetMoney()
    {
        return Instance.money;
    }

    void Update()
    {
        moneyText.SetText($"Money: {money}");
    }
}
