using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sends a customer in through the door every so often, as long as some placed table has a free seat.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    public CafeGrid grid;
    [Tooltip("Customers enter and leave through the middle of this object's bounds.")]
    public Renderer door;
    public Customer customerPrefab;

    [Min(0.1f)] public float interval = 10f;
    [Min(0f)] public float jitter = 2f;

    float timer;
    readonly List<SeatingTable> candidates = new List<SeatingTable>();

    void Start()
    {
        timer = NextWait();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = NextWait();
        TrySpawn();
    }

    float NextWait() => Mathf.Max(0.1f, interval + Random.Range(-jitter, jitter));

    /// <summary>Total seats not yet taken or claimed on tables that are placed on the grid.</summary>
    public int FreeSeats
    {
        get
        {
            int n = 0;
            foreach (var t in SeatingTable.All) if (t.IsPlaced) n += t.FreeSeatCount;
            return n;
        }
    }

    /// <summary>Spawns a customer bound for a random table with a free seat. Returns null if every seat is taken.</summary>
    public Customer TrySpawn()
    {
        candidates.Clear();
        foreach (var t in SeatingTable.All)
            if (t.IsPlaced && t.FreeSeatCount > 0) candidates.Add(t);
        if (candidates.Count == 0) return null;

        var table = candidates[Random.Range(0, candidates.Count)];
        var customer = Instantiate(customerPrefab, transform);
        if (!table.TryReserve(customer, out int seat))
        {
            Destroy(customer.gameObject);
            return null;
        }

        GetDoor(out var doorPoint, out var outward);
        customer.Begin(grid, doorPoint, outward, table, seat);
        return customer;
    }

    /// <summary>Door position on the floor, and the horizontal direction that leads out of the cafe.</summary>
    public void GetDoor(out Vector3 point, out Vector3 outward)
    {
        var b = door.bounds;
        point = new Vector3(b.center.x, grid.FloorY, b.center.z);

        // Out is away from the middle of the grid, along whichever axis the door sits on.
        var o = grid.Origin;
        var middle = new Vector3(o.x + grid.Columns * grid.cellSize * 0.5f, point.y, o.y + grid.Rows * grid.cellSize * 0.5f);
        var away = point - middle;
        outward = Mathf.Abs(away.x) > Mathf.Abs(away.z) ? new Vector3(Mathf.Sign(away.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(away.z));
    }
}
