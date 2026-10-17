using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Seats and cup spots on a Sofa & Table. A cup on spot i is drunk by whoever sits in seat i.
// The table can only be moved while nobody is seated and no cup is on it.
public class SeatingTable : MonoBehaviour
{
    [Tooltip("A seated customer's feet go here, facing the anchor's forward.")]
    public Transform[] seats = new Transform[2];
    [Tooltip("Cup spot for the seat with the same index.")]
    public Transform[] cupAnchors = new Transform[2];
    public float cupSnapRadius = 0.25f;

    public TMP_Text statsLabel;
    public string statsFormat = "Seats Filled: {0}/{1}";

    static readonly List<SeatingTable> all = new List<SeatingTable>();
    public static IReadOnlyList<SeatingTable> All => all;

    Customer[] reservedBy;
    bool[] isSeated;
    CoffeeCup[] cups;
    GridOccupant occupant;

    public bool IsPlaced => Occupant && occupant.isPlaced && occupant.grid;

    // Lazy: CafeGrid.Spawn adds the GridOccupant after Awake.
    public GridOccupant Occupant => occupant ? occupant : occupant = GetComponent<GridOccupant>();

    bool IsLocked
    {
        get
        {
            foreach (var s in isSeated) if (s) return true;
            foreach (var c in cups) if (c) return true;
            return false;
        }
    }

    int SeatsFilled
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

    // Snaps the cup to the nearest free spot in reach; false if none is close enough.
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

    public void RemoveCup(CoffeeCup cup)
    {
        for (int i = 0; i < cups.Length; i++)
            if (cups[i] == cup) cups[i] = null;
        RefreshLock();
    }

    void TryServe(int index)
    {
        if (index >= seats.Length || index >= cups.Length || !isSeated[index]) return;
        var cup = cups[index];
        if (cup && !cup.IsBeingDrunk) cup.BeginDrinking(reservedBy[index]);
    }

    // Walkable cells directly beside the footprint (no corners).
    public List<Vector2Int> ApproachCells()
    {
        var result = new List<Vector2Int>();
        if (!IsPlaced) return result;
        var grid = occupant.grid;
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
        // Colliders stay on so dropped cups can land on the table; only grabbing is blocked.
        MoveLock.For(gameObject, false).SetLocked(IsLocked);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var s in seats)
        {
            if (!s) continue;
            Gizmos.DrawWireSphere(s.position, 0.08f);
            Gizmos.DrawLine(s.position, s.position + s.forward * 0.3f);
        }
        Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
        foreach (var a in cupAnchors)
            if (a) Gizmos.DrawWireCube(a.position + Vector3.up * 0.05f, new Vector3(0.12f, 0.1f, 0.12f));
    }
}
