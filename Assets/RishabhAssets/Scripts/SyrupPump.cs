using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// A flavor-syrup bottle with a pump. Pressing the pump head (the "Cylinder") all the way down with a controller
/// flavors an unflavored <see cref="CoffeeCup"/> under the spout and spends one use.
/// </summary>
public class SyrupPump : Consumable
{
    [Tooltip("The pump head that moves. Positions below are this object's local Y, relative to the bottle.")]
    public Transform pumpHead;
    public float pressedY = 9.15f;
    public float restY = 9.48f;
    [Tooltip("How fast the head slides back up, in the bottle's local units per second.")]
    public float returnSpeed = 1.5f;

    [Tooltip("Where syrup comes out. A cup must be below this to be flavored.")]
    public Transform spout;
    [Tooltip("Horizontal distance (meters) from the spout within which a cup counts as underneath.")]
    public float cupRadius = 0.08f;
    [Tooltip("How far below the spout (meters) a cup can be and still catch the syrup.")]
    public float maxDropHeight = 0.5f;

    [Tooltip("Extra transforms that can press the pump (controller poke points are found automatically).")]
    public List<Transform> extraPressers = new List<Transform>();
    [Tooltip("Sideways slack (bottle-local units) around the pump head that still counts as touching it.")]
    public float touchMargin = 0.4f;

    readonly List<Transform> pressers = new List<Transform>();
    readonly HashSet<Transform> contacts = new HashSet<Transform>();
    float topOffset;       // From the head's pivot up to its top surface, in local units.
    Vector2 headMinXZ, headMaxXZ;
    bool armed = true;     // Re-armed once the head returns to the top, so one press = one pump.
    float findTimer;

    public float HeadY => pumpHead.localPosition.y;

    protected override void Awake()
    {
        base.Awake();
        if (!pumpHead) pumpHead = transform.Find("Cylinder");

        // Measure the head at rest in bottle-local space so pressing works however the bottle is posed.
        SetHeadY(restY);
        var r = pumpHead.GetComponent<Renderer>();
        if (r)
        {
            var b = r.bounds;
            var lo = transform.InverseTransformPoint(b.min);
            var hi = transform.InverseTransformPoint(b.max);
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

        // The lowest fingertip pressing the head decides how far it's pushed in.
        float target = restY;
        float headTop = HeadY + topOffset;
        foreach (var p in pressers)
        {
            if (!p || !p.gameObject.activeInHierarchy) { contacts.Remove(p); continue; }
            var local = transform.InverseTransformPoint(p.position);
            bool overHead = local.x >= headMinXZ.x - touchMargin && local.x <= headMaxXZ.x + touchMargin &&
                            local.z >= headMinXZ.y - touchMargin && local.z <= headMaxXZ.y + touchMargin;
            bool belowTop = local.y <= headTop + 0.05f;

            // Contact starts only at the top surface (so reaching up from under the bottle doesn't count),
            // then sticks however deep the tip goes until it lifts off or slides off the side.
            if (!overHead || !belowTop) contacts.Remove(p);
            else if (local.y >= headTop - 0.3f) contacts.Add(p);

            if (contacts.Contains(p)) target = Mathf.Min(target, local.y - topOffset);
        }

        float y = HeadY;
        y = target < y ? target : Mathf.MoveTowards(y, restY, returnSpeed * Time.deltaTime);
        SetHeadY(y);

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
        // Clamped so the head can't sink through the bottle or pop off the top.
        var p = pumpHead.localPosition;
        p.y = Mathf.Clamp(y, pressedY, restY);
        pumpHead.localPosition = p;
    }

    /// <summary>One full pump: flavors the nearest unflavored cup under the spout, if any, and spends a use.</summary>
    public bool Pump()
    {
        var cup = FindCupUnderSpout();
        if (!cup) return false;
        cup.SetFlavored();
        Consume();
        return true;
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
        pressers.AddRange(extraPressers);
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
