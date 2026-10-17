using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Seats on a Sofa &amp; Table. Customers reserve a seat when they spawn and sit on its anchor when they arrive.
/// While anyone is seated or any cup is on it, the table can't be picked up. Each seat has a matching cup anchor: a cup there is
/// drunk by whoever sits in that seat.
/// </summary>
public class SeatingTable : MonoBehaviour
{
    [Tooltip("Where a seated customer's feet go; the anchor's forward is the direction they face.")]
    public Transform[] seats = new Transform[2];

    [Tooltip("Where a served cup sits for the seat with the same index. The cup's pivot (its base) goes here.")]
    public Transform[] cupAnchors = new Transform[2];
    [Tooltip("A cup let go within this distance (meters) of a free cup anchor snaps onto it.")]
    public float cupSnapRadius = 0.25f;

    public TMP_Text statsLabel;
    public string statsFormat = "Seats Filled: {0}/{1}";

    static readonly List<SeatingTable> all = new List<SeatingTable>();
    public static IReadOnlyList<SeatingTable> All => all;

    Customer[] reservedBy;
    bool[] isSeated;
    CoffeeCup[] cups;
    GridOccupant occupant;

    /// <summary>True while the table sits on the grid (not held or in the inventory).</summary>
    public bool IsPlaced => Occupant && occupant.isPlaced && occupant.grid;

    // Looked up lazily: CafeGrid.Spawn adds the GridOccupant after this component's Awake has run.
    public GridOccupant Occupant => occupant ? occupant : occupant = GetComponent<GridOccupant>();

    /// <summary>True while a customer is seated or a cup is waiting; the table must be empty to be moved.</summary>
    public bool IsLocked
    {
        get
        {
            foreach (var s in isSeated) if (s) return true;
            foreach (var c in cups) if (c) return true;
            return false;
        }
    }

    /// <summary>Seats with a customer actually sitting in them.</summary>
    public int SeatsFilled
    {
        get
        {
            int n = 0;
            foreach (var s in isSeated) if (s) n++;
            return n;
        }
    }

    public int FreeSeatCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < seats.Length; i++) if (seats[i] && !reservedBy[i]) n++;
            return n;
        }
    }

    void Awake()
    {
        reservedBy = new Customer[seats.Length];
        isSeated = new bool[seats.Length];
        cups = new CoffeeCup[cupAnchors.Length];
        RefreshLabel();
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    /// <summary>Claims a random free seat for <paramref name="customer"/>.</summary>
    public bool TryReserve(Customer customer, out int seat)
    {
        var free = new List<int>();
        for (int i = 0; i < seats.Length; i++) if (seats[i] && !reservedBy[i]) free.Add(i);
        seat = free.Count > 0 ? free[Random.Range(0, free.Count)] : -1;
        if (seat < 0) return false;
        reservedBy[seat] = customer;
        return true;
    }

    public void SitDown(Customer customer, int seat)
    {
        if (seat < 0 || seat >= seats.Length || reservedBy[seat] != customer) return;
        isSeated[seat] = true;
        RefreshLock();
        TryServe(seat);
    }

    /// <summary>The customer sitting (not just walking to) seat <paramref name="seat"/>, if any.</summary>
    public Customer SeatedAt(int seat) =>
        seat >= 0 && seat < seats.Length && isSeated[seat] ? reservedBy[seat] : null;

    public CoffeeCup CupAt(int anchor) => anchor >= 0 && anchor < cups.Length ? cups[anchor] : null;

    /// <summary>
    /// Snaps <paramref name="cup"/> onto the nearest free cup anchor within reach. If someone is sitting in the
    /// matching seat they start drinking straight away. Returns false if no anchor is close enough.
    /// </summary>
    public bool TrySnapCup(CoffeeCup cup)
    {
        if (!IsPlaced) return false;
        int best = -1;
        float bestDist = cupSnapRadius * cupSnapRadius;
        for (int i = 0; i < cupAnchors.Length; i++)
        {
            if (!cupAnchors[i] || cups[i]) continue;
            float d = (cupAnchors[i].position - cup.transform.position).sqrMagnitude;
            if (d <= bestDist) { bestDist = d; best = i; }
        }
        if (best < 0) return false;

        cups[best] = cup;
        cup.AttachTo(this, best);
        RefreshLock();
        TryServe(best);
        return true;
    }

    /// <summary>Frees the anchor holding <paramref name="cup"/> (it was picked up or finished).</summary>
    public void RemoveCup(CoffeeCup cup)
    {
        for (int i = 0; i < cups.Length; i++)
            if (cups[i] == cup) cups[i] = null;
        RefreshLock();
    }

    void TryServe(int index)
    {
        var customer = SeatedAt(index);
        var cup = CupAt(index);
        if (customer && cup && !cup.IsBeingDrunk) cup.BeginDrinking(customer);
    }

    /// <summary>Frees whatever seat <paramref name="customer"/> held.</summary>
    public void Release(Customer customer)
    {
        for (int i = 0; i < seats.Length; i++)
        {
            if (reservedBy[i] != customer) continue;
            reservedBy[i] = null;
            isSeated[i] = false;
        }
        RefreshLock();
    }

    /// <summary>Walkable cells directly beside the table's footprint, where a customer can step up to it.</summary>
    public List<Vector2Int> ApproachCells()
    {
        var result = new List<Vector2Int>();
        if (!IsPlaced) return result;
        var grid = Occupant.grid;
        var size = CafeGrid.RotatedSize(occupant.footprint, occupant.quarterTurns);
        var a = occupant.anchor;
        for (int x = a.x - 1; x <= a.x + size.x; x++)
            for (int y = a.y - 1; y <= a.y + size.y; y++)
            {
                bool inside = x >= a.x && x < a.x + size.x && y >= a.y && y < a.y + size.y;
                bool corner = (x == a.x - 1 || x == a.x + size.x) && (y == a.y - 1 || y == a.y + size.y);
                var cell = new Vector2Int(x, y);
                if (!inside && !corner && GridPathfinder.IsWalkable(grid, cell)) result.Add(cell);
            }
        return result;
    }

    void RefreshLabel()
    {
        if (statsLabel) statsLabel.text = string.Format(statsFormat, SeatsFilled, seats.Length);
    }

    void RefreshLock()
    {
        RefreshLabel();
        // The table's colliders stay on so dropped cups can land on it; only grabbing is blocked.
        MoveLock.For(gameObject, false).SetLocked(IsLocked);
    }

    void OnDrawGizmos()
    {
        if (seats == null) return;
        Gizmos.color = Color.cyan;
        foreach (var s in seats)
        {
            if (!s) continue;
            Gizmos.DrawWireSphere(s.position, 0.08f);
            Gizmos.DrawLine(s.position, s.position + s.forward * 0.3f);
        }
        if (cupAnchors == null) return;
        Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
        foreach (var a in cupAnchors)
            if (a) Gizmos.DrawWireCube(a.position + Vector3.up * 0.05f, new Vector3(0.12f, 0.1f, 0.12f));
    }
}
