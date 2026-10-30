using System.Collections.Generic;
using Steven.MP2A;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attached to the Cafe's DeliveryCrate_Receiver instance. Blocks the grid cells under the crate
// and swaps incoming Kitchen Dish_Fries instances for the Cafe's StevenFries prefab.
// Active on startup; fries inside cannot be picked up until Goal 1 unlocks them.
public class CafeFriesReceiver : MonoBehaviour
{
    public CafeGrid grid;
    public TransportReceiver receiver;
    public GameObject friesPrefab;

    public List<Vector2Int> blockedCells = new List<Vector2Int>
    {
        new Vector2Int(0, 3),
        new Vector2Int(1, 3),
        new Vector2Int(0, 4),
        new Vector2Int(1, 4)
    };

    bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterKitchenTunnelFix()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    // Ensures Steven's "Delivery Crate - to Cafe" tunnel in StevenScene has PlateToGo wired
    // just like his Orchestra crate so plated fries convert into Dish_Fries on drop.
    static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "StevenScene") return;

        PlateToGo sourcePlateToGo = null;
        TransportTunnel cafeTunnel = null;

        foreach (var tunnel in Object.FindObjectsByType<TransportTunnel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!tunnel) continue;
            if (tunnel.TryGetComponent<PlateToGo>(out var ptg) && ptg.dishPrefabs != null && ptg.dishPrefabs.Count > 0)
                sourcePlateToGo = ptg;
            if (tunnel.receiverId == "Cafe")
                cafeTunnel = tunnel;
        }

        if (cafeTunnel != null && sourcePlateToGo != null && !cafeTunnel.TryGetComponent<PlateToGo>(out _))
        {
            var cafePtg = cafeTunnel.gameObject.AddComponent<PlateToGo>();
            cafePtg.partner = "Cafe";
            cafePtg.goldPerMeal = sourcePlateToGo.goldPerMeal;
            cafePtg.dishPrefabs = new List<GameObject>(sourcePlateToGo.dishPrefabs);
        }
    }

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        if (!grid) grid = FindFirstObjectByType<CafeGrid>();
        if (grid) grid.BlockCells(blockedCells);

        if (!receiver) receiver = GetComponent<TransportReceiver>();
        if (receiver)
        {
            receiver.receiverId = "Cafe";
            receiver.onArrived.RemoveListener(OnObjectArrived);
            receiver.onArrived.AddListener(OnObjectArrived);
        }
    }

    void OnDestroy()
    {
        if (receiver)
            receiver.onArrived.RemoveListener(OnObjectArrived);
    }

    public void Unlock()
    {
        Initialize();
        gameObject.SetActive(true);
        StevenFries.SetUnlocked(true);
    }

    void OnObjectArrived(GameObject arrived)
    {
        if (!arrived) return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Transform parent = CafePersistence.Instance != null
            ? CafePersistence.Instance.transform
            : (transform.parent != null ? transform.parent : null);

        if (arrived.TryGetComponent<StevenFries>(out var existingFries))
        {
            if (parent != null)
                arrived.transform.SetParent(parent, true);
            existingFries.ApplyLockState();
            return;
        }

        if (!friesPrefab) return;

        Vector3 spawnPos = arrived.transform.position;
        Quaternion spawnRot = friesPrefab.transform.rotation;

        arrived.SetActive(false);
        Destroy(arrived);

        var fries = Instantiate(friesPrefab, spawnPos, spawnRot, parent);
        if (fries.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = spawnPos;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (fries.TryGetComponent<StevenFries>(out var friesComp))
            friesComp.ApplyLockState();
    }
}
