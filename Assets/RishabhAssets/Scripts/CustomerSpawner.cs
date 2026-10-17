using System.Collections.Generic;
using UnityEngine;

// Sends a customer through the door every so often while any placed table has a free seat.
public class CustomerSpawner : MonoBehaviour
{
    public CafeGrid grid;
    [Tooltip("Customers enter and leave through the middle of this renderer's bounds.")]
    public Renderer door;
    public Customer customerPrefab;
    [Min(0.1f)] public float interval = 10f;
    [Min(0f)] public float jitter = 2f;

    float timer;
    readonly List<SeatingTable> candidates = new List<SeatingTable>();

    void Start() => timer = NextWait();

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = NextWait();
        TrySpawn();
    }

    float NextWait() => Mathf.Max(0.1f, interval + Random.Range(-jitter, jitter));

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

    // Door position on the floor, and the direction out of the cafe (away from the grid's middle).
    void GetDoor(out Vector3 point, out Vector3 outward)
    {
        var b = door.bounds;
        point = new Vector3(b.center.x, grid.FloorY, b.center.z);
        var o = grid.Origin;
        var middle = new Vector3(o.x + grid.Columns * grid.cellSize * 0.5f, point.y, o.y + grid.Rows * grid.cellSize * 0.5f);
        var away = point - middle;
        outward = Mathf.Abs(away.x) > Mathf.Abs(away.z) ? new Vector3(Mathf.Sign(away.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(away.z));
    }
}
