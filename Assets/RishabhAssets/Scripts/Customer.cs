using System;
using TMPro;
using UnityEngine;

// Walks in from the door, paths to its reserved seat, waits to be served (or runs out of patience), then walks out.
public class Customer : MonoBehaviour
{
    enum State { Entering, WalkingToSeat, Seated, Leaving, Exiting }

    public float walkSpeed = 1.2f;
    public float turnSpeed = 540f;
    [Tooltip("Seconds between path retries, e.g. while the table is being carried.")]
    public float retryDelay = 0.5f;

    [Min(0.1f)] public float patienceSeconds = 30f;
    public TMP_Text patienceLabel;
    public string patienceFormat = "Patience: {0}s";

    public float Patience { get; private set; }

    public static event Action<Customer, CoffeeCup> AnyServed;

    State state;
    SeatingTable table;
    int seatIndex = -1;
    bool beingServed;

    CafeGrid grid;
    Vector3 doorPoint, exitPoint;
    Vector3? stepTarget;
    Vector2Int? goalCell;
    Vector2Int goalTableAnchor;
    int goalTableTurns;
    float retryTimer;
    bool walkedThroughDoor;

    // Where a served cup slides to.
    public Vector3 DrinkPoint => RendererBounds.TryGet(gameObject, out var b) ? b.center : transform.position + Vector3.up * 0.5f;

    void Awake()
    {
        Patience = patienceSeconds;
        SetPatienceVisible(false);
    }

    // The table's seat must already be reserved for this customer.
    public void Begin(CafeGrid grid, Vector3 doorPoint, Vector3 outward, SeatingTable table, int seat)
    {
        this.grid = grid;
        this.doorPoint = doorPoint;
        exitPoint = doorPoint + outward.normalized;
        this.table = table;
        seatIndex = seat;
        transform.position = doorPoint;
        Face(-outward);
        state = State.Entering;
    }

    public void Serve(CoffeeCup cup)
    {
        AnyServed?.Invoke(this, cup);
        Leave();
    }

    [ContextMenu("Leave")]
    public void Leave()
    {
        if (state != State.Seated) return;
        SetPatienceVisible(false);

        // Step off the sofa onto the nearest free cell.
        var standCell = GridPathfinder.NearestWalkable(grid, transform.position);
        if (table) table.Release(this);
        if (standCell.HasValue) transform.position = grid.CellToWorld(standCell.Value);

        stepTarget = null;
        goalCell = null;
        state = State.Leaving;
    }

    // Stops the patience countdown once a cup starts sliding over.
    public void BeginBeingServed()
    {
        beingServed = true;
        SetPatienceVisible(false);
    }

    void OnDestroy()
    {
        if (table) table.Release(this);
    }

    void Update()
    {
        if (!grid) return;

        switch (state)
        {
            case State.Entering:
                var entry = GridPathfinder.NearestWalkable(grid, doorPoint);
                if (!entry.HasValue || MoveTowards(grid.CellToWorld(entry.Value)))
                    state = State.WalkingToSeat;
                break;
            case State.WalkingToSeat:
                UpdateWalkToSeat();
                break;
            case State.Seated:
                UpdatePatience();
                break;
            case State.Leaving:
                UpdateWalkToDoor();
                break;
            case State.Exiting:
                if (!walkedThroughDoor) walkedThroughDoor = MoveTowards(doorPoint);
                else if (MoveTowards(exitPoint)) Destroy(gameObject);
                break;
        }
    }

    void UpdatePatience()
    {
        if (beingServed) return;
        Patience = Mathf.Max(0f, Patience - Time.deltaTime);
        RefreshPatienceLabel();
        if (Patience <= 0f) Leave();
    }

    void SetPatienceVisible(bool visible)
    {
        // TMP_Text.canvas can't find a hidden canvas, so search inactive parents.
        var c = patienceLabel ? patienceLabel.GetComponentInParent<Canvas>(true) : null;
        if (c) c.gameObject.SetActive(visible);
    }

    void RefreshPatienceLabel()
    {
        if (patienceLabel) patienceLabel.text = string.Format(patienceFormat, Mathf.CeilToInt(Patience));
    }

    void UpdateWalkToSeat()
    {
        if (!table) { Destroy(gameObject); return; }
        if (stepTarget.HasValue && !MoveTowards(stepTarget.Value)) return;
        stepTarget = null;
        if ((retryTimer -= Time.deltaTime) > 0f) return;

        if (!table.IsPlaced) { retryTimer = retryDelay; return; }

        var here = grid.WorldToCell(transform.position);
        var seat = table.seats[seatIndex];
        if (!GoalStillValid()) goalCell = PickApproachCell(here, seat.position);
        if (!goalCell.HasValue) { retryTimer = retryDelay; return; }

        if (here == goalCell.Value)
        {
            Sit(seat);
            return;
        }

        // Re-plan every cell so the route follows furniture moved mid-walk.
        StepToward(here, goalCell.Value, () => goalCell = null);
    }

    void UpdateWalkToDoor()
    {
        if (stepTarget.HasValue && !MoveTowards(stepTarget.Value)) return;
        stepTarget = null;
        if ((retryTimer -= Time.deltaTime) > 0f) return;

        var doorCell = GridPathfinder.NearestWalkable(grid, doorPoint);
        if (!doorCell.HasValue) { retryTimer = retryDelay; return; }

        var here = grid.WorldToCell(transform.position);
        if (here == doorCell.Value)
        {
            walkedThroughDoor = false;
            state = State.Exiting;
            return;
        }
        StepToward(here, doorCell.Value, null);
    }

    void StepToward(Vector2Int here, Vector2Int goal, Action onNoPath)
    {
        var path = GridPathfinder.FindPath(grid, here, goal);
        if (path == null || path.Count < 2)
        {
            onNoPath?.Invoke();
            retryTimer = retryDelay;
            return;
        }
        stepTarget = grid.CellToWorld(path[1]);
    }

    bool GoalStillValid()
    {
        if (!goalCell.HasValue) return false;
        var occ = table.Occupant;
        return occ.anchor == goalTableAnchor && occ.quarterTurns == goalTableTurns && GridPathfinder.IsWalkable(grid, goalCell.Value);
    }

    // Reachable cell beside the table closest to the reserved seat.
    Vector2Int? PickApproachCell(Vector2Int from, Vector3 seatPosition)
    {
        var occ = table.Occupant;
        goalTableAnchor = occ.anchor;
        goalTableTurns = occ.quarterTurns;

        var cells = table.ApproachCells();
        cells.Sort((a, b) => FlatDistance(grid.CellToWorld(a), seatPosition).CompareTo(FlatDistance(grid.CellToWorld(b), seatPosition)));
        foreach (var c in cells)
            if (c == from || GridPathfinder.FindPath(grid, from, c) != null) return c;
        return null;
    }

    void Sit(Transform seat)
    {
        transform.position = seat.position;
        Face(seat.forward);
        Patience = patienceSeconds;
        // Must be Seated before SitDown, which may immediately serve a waiting cup.
        state = State.Seated;
        SetPatienceVisible(true);
        RefreshPatienceLabel();
        table.SitDown(this, seatIndex);
    }

    bool MoveTowards(Vector3 target)
    {
        var pos = transform.position;
        var delta = target - pos;
        delta.y = 0f;
        if (delta.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), turnSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(pos, target, walkSpeed * Time.deltaTime);
        return (transform.position - target).sqrMagnitude < 1e-6f;
    }

    void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude;
}
