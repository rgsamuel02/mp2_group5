using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Periodically drops a <see cref="Splat"/> on a random floor-level cell that has nothing on it.
/// </summary>
public class SplatSpawner : MonoBehaviour
{
    public CafeGrid grid;
    public Splat splatPrefab;

    [Tooltip("Average seconds between spills.")]
    [Min(0.1f)] public float interval = 10f;
    [Tooltip("Each wait is interval ± this many seconds.")]
    [Min(0f)] public float jitter = 2f;
    [Tooltip("Stop spawning while this many splats are on the floor.")]
    [Min(1)] public int maxSplats = 20;
    [Tooltip("Height above the cell's floor.")]
    public float heightAboveFloor = 0.01f;

    readonly Dictionary<Vector2Int, Splat> splats = new Dictionary<Vector2Int, Splat>();
    readonly List<Vector2Int> candidates = new List<Vector2Int>();
    float timer;

    public int ActiveCount => splats.Count;

    void Start()
    {
        timer = NextWait();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = NextWait();
        if (splats.Count < maxSplats) TrySpawn();
    }

    float NextWait() => Mathf.Max(0.1f, interval + Random.Range(-jitter, jitter));

    /// <summary>Spawns a splat on a random eligible cell. Returns null if none are free.</summary>
    public Splat TrySpawn()
    {
        candidates.Clear();
        for (int x = 0; x < grid.Columns; x++)
            for (int y = 0; y < grid.Rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!grid.IsRaised(cell) && !grid.IsOccupied(cell) && !splats.ContainsKey(cell))
                    candidates.Add(cell);
            }
        if (candidates.Count == 0) return null;

        var chosen = candidates[Random.Range(0, candidates.Count)];
        var position = grid.CellToWorld(chosen) + Vector3.up * heightAboveFloor;
        var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * splatPrefab.transform.rotation;

        var splat = Instantiate(splatPrefab, position, rotation, transform);
        splat.Cell = chosen;
        splat.Cleaned += OnCleaned;
        splats[chosen] = splat;
        return splat;
    }

    void OnCleaned(Splat splat)
    {
        if (splats.TryGetValue(splat.Cell, out var s) && s == splat) splats.Remove(splat.Cell);
    }
}
