using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Pressing the pump head fully down with a controller fingertip flavors an unflavored cup under the spout.
public class SyrupPump : Consumable
{
    [Tooltip("Moves along this object's local Y between pressedY and restY.")]
    public Transform pumpHead;
    public float pressedY = 9.15f;
    public float restY = 9.48f;
    [Tooltip("Local units per second.")]
    public float returnSpeed = 1.5f;

    public Transform spout;
    [Tooltip("Max horizontal distance (m) from the spout for a cup to count as underneath.")]
    public float cupRadius = 0.08f;
    public float maxDropHeight = 0.5f;
    [Tooltip("Sideways slack (local units) around the head that still counts as touching it.")]
    public float touchMargin = 0.4f;

    readonly List<Transform> pressers = new List<Transform>();
    readonly HashSet<Transform> contacts = new HashSet<Transform>();
    float topOffset;
    Vector2 headMinXZ, headMaxXZ;
    bool armed = true;
    float findTimer;

    float HeadY => pumpHead.localPosition.y;

    protected override void Awake()
    {
        base.Awake();
        if (!pumpHead) pumpHead = transform.Find("Cylinder");

        // Measure the head at rest in bottle-local space.
        SetHeadY(restY);
        if (pumpHead.TryGetComponent<Renderer>(out var r))
        {
            var lo = transform.InverseTransformPoint(r.bounds.min);
            var hi = transform.InverseTransformPoint(r.bounds.max);
            var mn = Vector3.Min(lo, hi);
            var mx = Vector3.Max(lo, hi);
            topOffset = mx.y - restY;
            headMinXZ = new Vector2(mn.x, mn.z);
            headMaxXZ = new Vector2(mx.x, mx.z);
        }
    }

    void Update()
    {
        if ((findTimer -= Time.deltaTime) <= 0f) { findTimer = 1f; FindPressers(); }

        float target = restY;
        float headTop = HeadY + topOffset;
        foreach (var p in pressers)
        {
            if (!p || !p.gameObject.activeInHierarchy) { contacts.Remove(p); continue; }
            var local = transform.InverseTransformPoint(p.position);
            bool overHead = local.x >= headMinXZ.x - touchMargin && local.x <= headMaxXZ.x + touchMargin &&
                            local.z >= headMinXZ.y - touchMargin && local.z <= headMaxXZ.y + touchMargin;

            // Contact must start at the top surface (not from below), then sticks until the tip lifts or slides off.
            if (!overHead || local.y > headTop + 0.05f) contacts.Remove(p);
            else if (local.y >= headTop - 0.3f) contacts.Add(p);

            if (contacts.Contains(p)) target = Mathf.Min(target, local.y - topOffset);
        }

        float y = HeadY;
        SetHeadY(target < y ? target : Mathf.MoveTowards(y, restY, returnSpeed * Time.deltaTime));

        // One full press = one pump; re-armed once the head is back up.
        if (armed && HeadY <= pressedY + 0.005f)
        {
            armed = false;
            Pump();
        }
        else if (!armed && HeadY >= restY - 0.02f)
        {
            armed = true;
        }
    }

    void SetHeadY(float y)
    {
        var p = pumpHead.localPosition;
        p.y = Mathf.Clamp(y, pressedY, restY);
        pumpHead.localPosition = p;
    }

    void Pump()
    {
        var cup = FindCupUnderSpout();
        if (!cup) return;
        cup.SetFlavored();
        Consume();
    }

    CoffeeCup FindCupUnderSpout()
    {
        var s = spout ? spout.position : pumpHead.position;
        CoffeeCup best = null;
        float bestDist = cupRadius * cupRadius;
        foreach (var cup in CoffeeCup.All)
        {
            if (cup.IsFlavored || !cup.TryGetBounds(out var b)) continue;
            if (b.max.y > s.y + 0.02f || b.max.y < s.y - maxDropHeight) continue;
            float d = new Vector2(b.center.x - s.x, b.center.z - s.z).sqrMagnitude;
            if (d <= bestDist) { bestDist = d; best = cup; }
        }
        return best;
    }

    void FindPressers()
    {
        pressers.Clear();
        foreach (var poke in FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None))
            pressers.Add(poke.attachTransform ? poke.attachTransform : poke.transform);
    }

    void OnDrawGizmosSelected()
    {
        if (!spout) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(spout.position, spout.position + Vector3.down * maxDropHeight);
        Gizmos.DrawWireSphere(spout.position + Vector3.down * maxDropHeight, cupRadius);
    }
}
