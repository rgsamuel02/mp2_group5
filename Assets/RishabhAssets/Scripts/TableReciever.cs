using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Spawns chocolate souffles, coffee plants, and violas in the 9 grid slots on the receiver table as ranks unlock them.
// Replenishes empty slots every 20 seconds, except violas which do not replenish once taken off.
public class TableReciever : MonoBehaviour
{
    public CafeGrid grid;
    public Material previewMaterial;
    public GameObject soufflePrefab;
    public GameObject coffeePlantPrefab;
    public GameObject violaPrefab;
    [Min(0.1f)] public float replenishInterval = 20f;

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
        public bool isViola;
        public bool canReplenish = true;
    }

    readonly List<SlotState> slotStates = new List<SlotState>();
    float timer;
    bool initialized;
    bool soufflesUnlocked;
    bool coffeePlantsUnlocked;
    bool violaUnlocked;
    bool hasSpawnedViola;

    void Awake()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        if (!grid) grid = FindFirstObjectByType<CafeGrid>();
        timer = replenishInterval;

        slotStates.Clear();
        for (int i = 0; i < slots.Count; i++)
            slotStates.Add(new SlotState { cell = slots[i] });
    }

    void OnDestroy()
    {
        foreach (var s in slotStates)
            Unsubscribe(s);
        ClearSpawnedItems();
    }

    void ClearSpawnedItems()
    {
        foreach (var s in slotStates)
        {
            if (s.item) Destroy(s.item);
            s.item = null;
        }
    }

    public void UnlockSouffles()
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        if (soufflesUnlocked) return;
        soufflesUnlocked = true;
        timer = replenishInterval;
        ReplenishSlots();
    }

    public void UnlockCoffeePlants()
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        if (coffeePlantsUnlocked) return;
        coffeePlantsUnlocked = true;

        // Fill any currently empty slots first.
        ReplenishSlots();

        // If no coffee plants are on the table yet (e.g., all 9 slots still held untouched souffles),
        // replace a few random untouched slots with coffee plants so they appear immediately.
        if (!coffeePlantPrefab) return;
        bool anyPlant = false;
        var replaceable = new List<SlotState>();
        foreach (var s in slotStates)
        {
            if (!s.item) continue;
            if (s.item.GetComponent<RebeccaCoffeePlant>()) anyPlant = true;
            else if (!s.isViola) replaceable.Add(s);
        }

        if (!anyPlant && replaceable.Count > 0)
        {
            int count = Mathf.Clamp(replaceable.Count / 2, 1, replaceable.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(i, replaceable.Count);
                (replaceable[i], replaceable[idx]) = (replaceable[idx], replaceable[i]);
                Destroy(replaceable[i].item);
                replaceable[i].item = null;
                SpawnInSlot(replaceable[i], coffeePlantPrefab);
            }
        }
    }

    public void UnlockViola()
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        if (violaUnlocked) return;
        violaUnlocked = true;
        TrySpawnViola();
    }

    void TrySpawnViola()
    {
        if (hasSpawnedViola || !violaPrefab) return;

        var freeSlots = new List<SlotState>();
        var replaceableSlots = new List<SlotState>();
        foreach (var s in slotStates)
        {
            if (grid && grid.IsOccupied(s.cell)) continue;
            if (s.item == null && s.canReplenish)
                freeSlots.Add(s);
            else if (s.item != null && !s.isViola)
                replaceableSlots.Add(s);
        }

        if (freeSlots.Count > 0)
        {
            var chosen = freeSlots[Random.Range(0, freeSlots.Count)];
            SpawnInSlot(chosen, violaPrefab);
            hasSpawnedViola = true;
        }
        else if (replaceableSlots.Count > 0)
        {
            var chosen = replaceableSlots[Random.Range(0, replaceableSlots.Count)];
            Destroy(chosen.item);
            chosen.item = null;
            SpawnInSlot(chosen, violaPrefab);
            hasSpawnedViola = true;
        }
    }

    void Update()
    {
        if (!grid || !soufflesUnlocked) return;

        // Check if any item was moved or destroyed off its slot.
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

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = replenishInterval;

        ReplenishSlots();
    }

    void ReplenishSlots()
    {
        if (violaUnlocked && !hasSpawnedViola)
            TrySpawnViola();

        var replenishPool = new List<GameObject>();
        if (soufflesUnlocked && soufflePrefab) replenishPool.Add(soufflePrefab);
        if (coffeePlantsUnlocked && coffeePlantPrefab) replenishPool.Add(coffeePlantPrefab);
        if (replenishPool.Count == 0) return;

        foreach (var s in slotStates)
        {
            if (s.item != null || !s.canReplenish) continue;
            if (grid && grid.IsOccupied(s.cell)) continue;

            var prefab = replenishPool[Random.Range(0, replenishPool.Count)];
            SpawnInSlot(s, prefab);
        }
    }

    void SpawnInSlot(SlotState s, GameObject prefab)
    {
        if (!prefab || !grid) return;
        Unsubscribe(s);

        var parent = transform.parent ? transform.parent : transform;
        var go = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, parent);

        var target = grid.CellToWorld(s.cell);
        if (RendererBounds.TryGet(go, out var b))
            go.transform.position = new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);
        else
            go.transform.position = target;

        if (go.TryGetComponent<GrabbableItem>(out var grabbable))
        {
            grabbable.grid = grid;
            if (previewMaterial && !grabbable.previewMaterial)
                grabbable.previewMaterial = previewMaterial;
        }

        s.item = go;
        s.isViola = (prefab == violaPrefab) || go.GetComponent<BryceViola>() != null;
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
        if (s.isViola)
            s.canReplenish = false;
    }

    void Unsubscribe(SlotState s)
    {
        if (s.grab && s.grabListener != null)
            s.grab.selectEntered.RemoveListener(s.grabListener);
        s.grab = null;
        s.grabListener = null;
    }
}
