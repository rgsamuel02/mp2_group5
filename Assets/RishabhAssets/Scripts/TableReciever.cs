using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Receives Coffee Plants from Rebecca's Garden and Violas from Bryce's Orchestra
// in the 9 grid slots on the receiver table. Active from startup; items on the table
// cannot be picked up or moved until their corresponding Cafe Goal is unlocked.
public class TableReciever : MonoBehaviour
{
    public CafeGrid grid;
    public Material previewMaterial;
    public GameObject friesPrefab;
    public GameObject coffeePlantPrefab;
    public GameObject violaPrefab;
    [Min(0.1f)] public float replenishInterval = 10f;

    public List<Vector2Int> slots = new List<Vector2Int>
    {
        new Vector2Int(0, 5), new Vector2Int(0, 6), new Vector2Int(0, 7),
        new Vector2Int(1, 5), new Vector2Int(1, 6), new Vector2Int(1, 7),
        new Vector2Int(2, 5), new Vector2Int(2, 6), new Vector2Int(2, 7)
    };

    class SlotState
    {
        public Vector2Int cell;
        public GameObject item;
        public XRGrabInteractable grab;
        public UnityEngine.Events.UnityAction<SelectEnterEventArgs> grabListener;
    }

    static int s_PendingCoffeePlants;
    static int s_PendingViolas;
    static TableReciever s_Instance;

    readonly List<SlotState> slotStates = new List<SlotState>();
    bool initialized;
    bool friesUnlocked;
    bool coffeePlantsUnlocked;
    bool violaUnlocked;

    public static void EnqueueCoffeePlants(int count = 1)
    {
        if (count <= 0) return;
        s_PendingCoffeePlants += count;
        if (s_Instance != null && s_Instance.isActiveAndEnabled)
            s_Instance.ProcessPendingDeliveries();
    }

    public static void EnqueueViola(int count = 1)
    {
        if (count <= 0) return;
        s_PendingViolas += count;
        if (s_Instance != null && s_Instance.isActiveAndEnabled)
            s_Instance.ProcessPendingDeliveries();
    }

    void Awake()
    {
        EnsureInitialized();
    }

    void OnEnable()
    {
        EnsureInitialized();
        ProcessPendingDeliveries();
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        if (s_Instance == null) s_Instance = this;

        if (!grid) grid = FindFirstObjectByType<CafeGrid>();
        if (grid) grid.BlockCells(slots);

        // Allow Bryce's Orchestra to send Violas to the Cafe from the start
        // (picking up/placing the Viola in the Cafe is gated by Goal 3).
        Integrator.UnlockCafe();

        slotStates.Clear();
        for (int i = 0; i < slots.Count; i++)
            slotStates.Add(new SlotState { cell = slots[i] });
    }

    void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
        foreach (var s in slotStates)
            Unsubscribe(s);
    }

    public void UnlockFries()
    {
        EnsureInitialized();
        friesUnlocked = true;
        gameObject.SetActive(true);
        StevenFries.SetUnlocked(true);
        ProcessPendingDeliveries();
    }

    public void UnlockCoffeePlants()
    {
        EnsureInitialized();
        coffeePlantsUnlocked = true;
        gameObject.SetActive(true);
        RebeccaCoffeePlant.SetUnlocked(true);
        ProcessPendingDeliveries();
    }

    public void UnlockViola()
    {
        EnsureInitialized();
        violaUnlocked = true;
        gameObject.SetActive(true);
        Integrator.UnlockCafe();
        BryceViola.SetUnlocked(true);
        ProcessPendingDeliveries();
    }

    void Update()
    {
        if (!grid) return;

        // Check if any item was grabbed, moved, or destroyed off its slot.
        float maxDist = grid.cellSize * 0.45f;
        foreach (var s in slotStates)
        {
            if (!s.item) continue;
            var cellWorld = grid.CellToWorld(s.cell);
            float dx = s.item.transform.position.x - cellWorld.x;
            float dz = s.item.transform.position.z - cellWorld.z;
            if ((s.grab && s.grab.isSelected) || (dx * dx + dz * dz > maxDist * maxDist))
                MarkTakenOff(s);
        }

        if (s_PendingViolas > 0 || s_PendingCoffeePlants > 0)
            ProcessPendingDeliveries();
    }

    public void ProcessPendingDeliveries()
    {
        EnsureInitialized();
        if (s_PendingViolas <= 0 && s_PendingCoffeePlants <= 0) return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!grid) grid = FindFirstObjectByType<CafeGrid>();
        if (!grid) return;

        var freeSlots = new List<SlotState>();
        foreach (var s in slotStates)
        {
            if (s.item == null)
                freeSlots.Add(s);
        }

        if (freeSlots.Count == 0) return;

        // Shuffle free slots so deliveries appear cleanly across the table surface.
        for (int i = freeSlots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (freeSlots[i], freeSlots[j]) = (freeSlots[j], freeSlots[i]);
        }

        int slotIdx = 0;

        while (s_PendingViolas > 0 && slotIdx < freeSlots.Count && violaPrefab != null)
        {
            SpawnInSlot(freeSlots[slotIdx++], violaPrefab);
            s_PendingViolas--;
        }

        while (s_PendingCoffeePlants > 0 && slotIdx < freeSlots.Count && coffeePlantPrefab != null)
        {
            SpawnInSlot(freeSlots[slotIdx++], coffeePlantPrefab);
            s_PendingCoffeePlants--;
        }
    }

    void SpawnInSlot(SlotState s, GameObject prefab)
    {
        if (!prefab || !grid) return;
        Unsubscribe(s);

        var parent = transform.parent ? transform.parent : transform;
        var target = grid.CellToWorld(s.cell);

        // Instantiate directly at target (NOT Vector3.zero) so Rigidbodies with interpolation
        // never initialize their physics pose at the world origin (0,0,0) in the center of the room.
        var go = Instantiate(prefab, target, prefab.transform.rotation, parent);

        Vector3 finalPos = target;
        if (RendererBounds.TryGet(go, out var b))
            finalPos = new Vector3(target.x + (target.x - b.center.x), target.y + (target.y - b.min.y), target.z + (target.z - b.center.z));

        go.transform.position = finalPos;

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = finalPos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (go.TryGetComponent<GrabbableItem>(out var grabbable))
        {
            grabbable.grid = grid;
            if (previewMaterial && !grabbable.previewMaterial)
                grabbable.previewMaterial = previewMaterial;
        }

        // Re-apply lock state after positioning so locked items stay kinematic and ungrabbable on their table slot.
        if (go.TryGetComponent<RebeccaCoffeePlant>(out var plant))
            plant.ApplyLockState();
        if (go.TryGetComponent<BryceViola>(out var viola))
            viola.ApplyLockState();
        if (go.TryGetComponent<StevenFries>(out var fries))
            fries.ApplyLockState();

        s.item = go;
        s.grab = go.GetComponent<XRGrabInteractable>();
        if (s.grab)
        {
            s.grabListener = _ => MarkTakenOff(s);
            s.grab.selectEntered.AddListener(s.grabListener);
        }
    }

    void MarkTakenOff(SlotState s)
    {
        Unsubscribe(s);
        s.item = null;
    }

    void Unsubscribe(SlotState s)
    {
        if (s.grab && s.grabListener != null)
            s.grab.selectEntered.RemoveListener(s.grabListener);
        s.grab = null;
        s.grabListener = null;
    }
}
