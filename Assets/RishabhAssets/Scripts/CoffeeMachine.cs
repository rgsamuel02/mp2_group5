using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Brews a cup into a free slot when a BeanPack touches the BeanIntake trigger.
// Can't be moved while brewing or holding a cup.
public class CoffeeMachine : MonoBehaviour
{
    public static event Action<CoffeeCup> AnyBrewed;

    public Transform[] cupAnchors = new Transform[0];
    public CoffeeCup cupPrefab;
    [Min(0.01f)] public float brewSeconds = 5f;
    [Tooltip("A cup further than this from its anchor no longer occupies the slot.")]
    public float slotRadius = 0.05f;

    public TMP_Text statsLabel;
    public string statsFormat = "Brew Progress: {0}\nCups Ready: {1}/{2}";

    class Slot
    {
        public CoffeeCup cup;
        public bool brewing;
        public float progress;
        public bool IsFree => !brewing && !cup;
    }

    Slot[] slots;
    GridOccupant occupant;
    MoveLock moveLock;
    // Each bean pack must leave the intake before it can be used again.
    readonly HashSet<BeanPack> touching = new HashSet<BeanPack>();

    int CupsReady
    {
        get
        {
            int n = 0;
            foreach (var s in slots) if (s.cup) n++;
            return n;
        }
    }

    // Furthest-along brew (0..1), or -1 when idle.
    float BrewProgress
    {
        get
        {
            float p = -1f;
            foreach (var s in slots) if (s.brewing) p = Mathf.Max(p, s.progress);
            return p;
        }
    }

    bool IsBusy
    {
        get
        {
            foreach (var s in slots) if (s.brewing || s.cup) return true;
            return false;
        }
    }

    void Awake()
    {
        slots = new Slot[cupAnchors.Length];
        for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
        occupant = GetComponent<GridOccupant>();
        // The grab collider encloses the slots and would block the ray to the cups, so it's off while locked.
        moveLock = MoveLock.For(gameObject, true);
    }

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s.brewing)
            {
                s.progress += Time.deltaTime / brewSeconds;
                if (s.progress >= 1f)
                {
                    var anchor = cupAnchors[i];
                    s.cup = Instantiate(cupPrefab, anchor.position, anchor.rotation, transform.parent);
                    s.brewing = false;
                    s.progress = 0f;
                    AnyBrewed?.Invoke(s.cup);
                }
            }

            if (s.cup && (s.cup.IsHeld || (s.cup.transform.position - cupAnchors[i].position).sqrMagnitude > slotRadius * slotRadius))
                s.cup = null;
        }
        touching.RemoveWhere(b => !b);
        moveLock.SetLocked(IsBusy);
        RefreshLabel();
    }

    void OnTriggerEnter(Collider other)
    {
        var beans = other.GetComponentInParent<BeanPack>();
        if (beans && touching.Add(beans)) TryBrew(beans);
    }

    void OnTriggerExit(Collider other)
    {
        var beans = other.GetComponentInParent<BeanPack>();
        if (beans) touching.Remove(beans);
    }

    void TryBrew(BeanPack beans)
    {
        if (occupant && !occupant.isPlaced) return;

        int free = System.Array.FindIndex(slots, s => s.IsFree);
        if (free < 0 || !beans.Consume()) return;

        slots[free].brewing = true;
        slots[free].progress = 0f;
        TutorialPopup.NotifyUsed(this);
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
