using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brews a cup into a free anchor slot whenever a <see cref="BeanPack"/> touches the machine.
/// Needs a trigger collider on this object or a child (the "BeanIntake" volume) and a Rigidbody on this object.
/// </summary>
public class CoffeeMachine : MonoBehaviour
{
    [Tooltip("Where brewed cups appear. Each one holds a single cup.")]
    public Transform[] cupAnchors = new Transform[0];
    public CoffeeCup cupPrefab;
    [Tooltip("Seconds between the beans going in and the cup appearing. The slot is reserved meanwhile.")]
    [Min(0f)] public float brewSeconds = 5f;
    [Tooltip("A cup further than this from its anchor no longer blocks the slot.")]
    public float slotRadius = 0.05f;

    class Slot
    {
        public CoffeeCup cup;
        public bool brewing;
        public bool IsFree => !brewing && !cup;
    }

    Slot[] slots;
    GridOccupant occupant;
    // Bean packs currently touching us; each must leave before it can be used again.
    readonly HashSet<BeanPack> touching = new HashSet<BeanPack>();

    public int FreeSlots
    {
        get
        {
            int n = 0;
            foreach (var s in slots) if (s.IsFree) n++;
            return n;
        }
    }

    void Awake()
    {
        slots = new Slot[cupAnchors.Length];
        for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
        occupant = GetComponent<GridOccupant>();
    }

    void Update()
    {
        // A cup that's been picked up or moved away frees its slot.
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s.cup && (s.cup.IsHeld || (s.cup.transform.position - cupAnchors[i].position).sqrMagnitude > slotRadius * slotRadius))
                s.cup = null;
        }
        touching.RemoveWhere(b => !b);
    }

    void OnTriggerEnter(Collider other)
    {
        var beans = other.GetComponentInParent<BeanPack>();
        if (!beans || !touching.Add(beans)) return;
        TryBrew(beans);
    }

    void OnTriggerExit(Collider other)
    {
        var beans = other.GetComponentInParent<BeanPack>();
        if (beans) touching.Remove(beans);
    }

    /// <summary>Uses one scoop of <paramref name="beans"/> to start a cup, if a slot is free.</summary>
    public bool TryBrew(BeanPack beans)
    {
        // Don't brew while the machine itself is being carried or sitting in the inventory.
        if (occupant && !occupant.isPlaced) return false;

        int free = System.Array.FindIndex(slots, s => s.IsFree);
        if (free < 0 || !beans.Consume()) return false;

        StartCoroutine(Brew(free));
        return true;
    }

    IEnumerator Brew(int index)
    {
        var slot = slots[index];
        slot.brewing = true;
        if (brewSeconds > 0f) yield return new WaitForSeconds(brewSeconds);

        var anchor = cupAnchors[index];
        slot.cup = Instantiate(cupPrefab, anchor.position, anchor.rotation, transform.parent);
        slot.brewing = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
        foreach (var a in cupAnchors)
            if (a) Gizmos.DrawWireSphere(a.position, slotRadius);
    }
}
