using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A cafe patron: walks in from the door, paths to its reserved seat, sits until <see cref="Leave"/> is called,
/// then walks back out the door and despawns.
/// </summary>
public class Customer : MonoBehaviour
{
    public enum State { Entering, WalkingToSeat, Seated, Leaving, Exiting }

    public float walkSpeed = 1.2f;
    [Tooltip("Degrees per second the customer turns to face where they're walking.")]
    public float turnSpeed = 540f;
    [Tooltip("Seconds between retries when there's no path (e.g. the table is being carried).")]
    public float retryDelay = 0.5f;

    public State CurrentState { get; private set; }
    public SeatingTable Table { get; private set; }
    public int SeatIndex { get; private set; } = -1;

    public event Action<Customer> SatDown;
    public event Action<Customer> Left;
    /// <summary>Raised for every customer that sits down, for systems that don't track customers individually.</summary>
    public static event Action<Customer> AnySatDown;
    /// <summary>Raised when any customer finishes a cup, just before they leave. Hook rewards in here.</summary>
    public static event Action<Customer, CoffeeCup> AnyServed;

    /// <summary>Where a served cup slides to: the middle of the customer's body.</summary>
    public Vector3 DrinkPoint
    {
        get
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return transform.position + Vector3.up * 0.5f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.center;
        }
    }

    /// <summary>The customer has finished <paramref name="cup"/>: report it and head for the door.</summary>
    public void Serve(CoffeeCup cup)
    {
        AnyServed?.Invoke(this, cup);
        Leave();
    }

    CafeGrid grid;
    Vector3 doorPoint, exitPoint;

    Vector3? stepTarget;
    Vector2Int? goalCell;
    Vector2Int goalTableAnchor;
    int goalTableTurns;
    float retryTimer;
    bool walkedThroughDoor;

    /// <summary>Starts the visit. <paramref name="table"/>'s seat must already be reserved for this customer.</summary>
    public void Begin(CafeGrid grid, Vector3 doorPoint, Vector3 outward, SeatingTable table, int seat)
    {
        this.grid = grid;
        this.doorPoint = doorPoint;
        exitPoint = doorPoint + outward.normalized;
        Table = table;
        SeatIndex = seat;
        transform.position = doorPoint;
        Face(-outward);
        CurrentState = State.Entering;
    }

    /// <summary>Ends the wait: the customer stands up and walks out.</summary>
    [ContextMenu("Leave")]
    public void Leave()
    {
        if (CurrentState != State.Seated) return;

        // Step off the sofa onto the free cell nearest the seat.
        var standCell = GridPathfinder.NearestWalkable(grid, transform.position);
        if (Table) Table.Release(this);
        if (standCell.HasValue) transform.position = grid.CellToWorld(standCell.Value);

        stepTarget = null;
        goalCell = null;
        CurrentState = State.Leaving;
        Left?.Invoke(this);
    }

    void OnDestroy()
    {
        if (Table) Table.Release(this);
    }

    void Update()
    {
        if (!grid) return;

        switch (CurrentState)
        {
            case State.Entering:
                // Walk from the doorway onto the grid, then start pathing.
                var entry = GridPathfinder.NearestWalkable(grid, doorPoint);
                if (!entry.HasValue || MoveTowards(grid.CellToWorld(entry.Value)))
                    CurrentState = State.WalkingToSeat;
                break;

            case State.WalkingToSeat:
                UpdateWalkToSeat();
                break;

            case State.Leaving:
                UpdateWalkToDoor();
                break;

            case State.Exiting:
                if (!walkedThroughDoor)
                {
                    if (MoveTowards(doorPoint)) walkedThroughDoor = true;
                }
                else if (MoveTowards(exitPoint))
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    void UpdateWalkToSeat()
    {
        if (!Table) { Destroy(gameObject); return; }
        if (stepTarget.HasValue && !MoveTowards(stepTarget.Value)) return;
        stepTarget = null;

        if ((retryTimer -= Time.deltaTime) > 0f) return;

        // The table is being carried: wait for it to be put down again.
        if (!Table.IsPlaced) { retryTimer = retryDelay; return; }

        var here = grid.WorldToCell(transform.position);
        var seat = Table.seats[SeatIndex];

        if (!GoalStillValid())
            goalCell = PickApproachCell(here, seat.position);
        if (!goalCell.HasValue) { retryTimer = retryDelay; return; }

        if (here == goalCell.Value)
        {
            Sit(seat);
            return;
        }

        // Re-plan every cell so the route follows furniture that moves while we walk.
        var path = GridPathfinder.FindPath(grid, here, goalCell.Value);
        if (path == null || path.Count < 2) { goalCell = null; retryTimer = retryDelay; return; }
        stepTarget = grid.CellToWorld(path[1]);
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
            CurrentState = State.Exiting;
            return;
        }

        var path = GridPathfinder.FindPath(grid, here, doorCell.Value);
        if (path == null || path.Count < 2) { retryTimer = retryDelay; return; }
        stepTarget = grid.CellToWorld(path[1]);
    }

    bool GoalStillValid()
    {
        if (!goalCell.HasValue) return false;
        var occ = Table.Occupant;
        return occ.anchor == goalTableAnchor && occ.quarterTurns == goalTableTurns && GridPathfinder.IsWalkable(grid, goalCell.Value);
    }

    /// <summary>Reachable cell beside the table that is closest to the reserved seat.</summary>
    Vector2Int? PickApproachCell(Vector2Int from, Vector3 seatPosition)
    {
        var occ = Table.Occupant;
        goalTableAnchor = occ.anchor;
        goalTableTurns = occ.quarterTurns;

        var cells = Table.ApproachCells();
        cells.Sort((a, b) => FlatDistance(grid.CellToWorld(a), seatPosition).CompareTo(FlatDistance(grid.CellToWorld(b), seatPosition)));
        foreach (var c in cells)
            if (c == from || GridPathfinder.FindPath(grid, from, c) != null) return c;
        return null;
    }

    void Sit(Transform seat)
    {
        transform.position = seat.position;
        Face(seat.forward);
        Table.SitDown(this, SeatIndex);
        CurrentState = State.Seated;
        SatDown?.Invoke(this);
        AnySatDown?.Invoke(this);
    }

    /// <summary>Moves toward <paramref name="target"/>; returns true once there.</summary>
    bool MoveTowards(Vector3 target)
    {
        var pos = transform.position;
        var delta = target - pos;
        delta.y = 0f;
        if (delta.sqrMagnitude > 1e-6f)
        {
            var look = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
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
