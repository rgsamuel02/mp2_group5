using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Drop-off bucket next to TableReciever that accepts brewed CoffeeCup instances
// and sends them to Bryce's Orchestra simulation via GameState.Instance.AddCoffee().
public class CafeCoffeeBucket : MonoBehaviour
{
    public CafeGrid grid;
    public List<Vector2Int> blockedCells = new List<Vector2Int>
    {
        new Vector2Int(0, 8),
        new Vector2Int(1, 8)
    };

    static int s_PendingOrchestraCoffee;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterOrchestraFlush()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FlushPendingToOrchestra();
    }

    public static void FlushPendingToOrchestra()
    {
        if (s_PendingOrchestraCoffee <= 0 || GameState.Instance == null) return;
        int count = s_PendingOrchestraCoffee;
        s_PendingOrchestraCoffee = 0;
        for (int i = 0; i < count; i++)
            GameState.Instance.AddCoffee();
    }

    void Awake()
    {
        if (!grid) grid = FindFirstObjectByType<CafeGrid>();
        if (grid) grid.BlockCells(blockedCells);
    }

    void OnTriggerEnter(Collider other) => TryAcceptCup(other);

    void OnTriggerStay(Collider other) => TryAcceptCup(other);

    void TryAcceptCup(Collider other)
    {
        if (!other) return;
        var cup = other.GetComponentInParent<CoffeeCup>();
        if (!cup || cup.IsHeld || cup.IsBeingDrunk) return;

        DeliverCoffeeToOrchestra();
        cup.gameObject.SetActive(false);
        Destroy(cup.gameObject);
    }

    static void DeliverCoffeeToOrchestra()
    {
        if (GameState.Instance == null || !GameState.Instance.TrySpendMoney(2))
        {
            //TODO pop up (Orchestra doesn't have money currently)
        }
        else
            GameState.Instance.AddCoffee();
    }
}
