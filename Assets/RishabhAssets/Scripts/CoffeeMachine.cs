using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Brews a cup into a free anchor slot whenever a <see cref="BeanPack"/> touches the machine.
/// Needs a trigger collider on this object or a child (the "BeanIntake" volume) and a Rigidbody on this object.
/// Stats: Brew Progress (advances every frame while brewing) and Cups Ready, shown on <see cref="statsLabel"/>.
/// </summary>
public class CoffeeMachine : MonoBehaviour
{
    [Tooltip("Where brewed cups appear. Each one holds a single cup.")]
    public Transform[] cupAnchors = new Transform[0];
    public CoffeeCup cupPrefab;
    [Tooltip("Seconds for Brew Progress to go from 0% to 100%. The slot is reserved meanwhile.")]
    [Min(0.01f)] public float brewSeconds = 5f;
    [Tooltip("A cup further than this from its anchor no longer blocks the slot.")]
    public float slotRadius = 0.05f;

    public TMP_Text statsLabel;
    public string statsFormat = "Brew Progress: {0}\nCups Ready: {1}/{2}";

    class Slot
    {
        public CoffeeCup cup;
        public bool brewing;
        public float progress; // 0..1
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

    /// <summary>Cups sitting in their slots waiting to be taken.</summary>
    public int CupsReady
    {
        get
        {
            int n = 0;
            foreach (var s in slots) if (s.cup) n++;
            return n;
        }
    }

    /// <summary>Progress (0..1) of the furthest-along brew, or -1 when nothing is brewing.</summary>
    public float BrewProgress
    {
        get
        {
            float p = -1f;
            foreach (var s in slots) if (s.brewing) p = Mathf.Max(p, s.progress);
            return p;
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
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];

            // Brew Progress: Euler step every frame; at 100% the cup is done and the stat resets.
            if (s.brewing)
            {
                s.progress += Time.deltaTime / brewSeconds;
                if (s.progress >= 1f)
                {
                    var anchor = cupAnchors[i];
                    s.cup = Instantiate(cupPrefab, anchor.position, anchor.rotation, transform.parent);
                    s.brewing = false;
                    s.progress = 0f;
                }
            }

            // A cup that's been picked up or moved away frees its slot.
            if (s.cup && (s.cup.IsHeld || (s.cup.transform.position - cupAnchors[i].position).sqrMagnitude > slotRadius * slotRadius))
                s.cup = null;
        }
        touching.RemoveWhere(b => !b);
        RefreshLabel();
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

        slots[free].brewing = true;
        slots[free].progress = 0f;
        TutorialPopup.NotifyUsed(this);
        return true;
    }

    void RefreshLabel()
    {
        if (!statsLabel) return;
        float p = BrewProgress;
        string progress = p < 0f ? "Idle" : Mathf.FloorToInt(p * 100f) + "%";
        statsLabel.text = string.Format(statsFormat, progress, CupsReady, slots.Length);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
        foreach (var a in cupAnchors)
            if (a) Gizmos.DrawWireSphere(a.position, slotRadius);
    }
}
