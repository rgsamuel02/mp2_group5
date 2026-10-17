using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drops a Splat on a random free floor-level cell every so often, plus a few at startup.
public class SplatSpawner : MonoBehaviour
{
    public CafeGrid grid;
    public Splat splatPrefab;
    [Min(0.1f)] public float interval = 10f;
    [Min(0f)] public float jitter = 2f;
    [Tooltip("No new splats while this many are on the floor.")]
    [Min(1)] public int maxSplats = 20;
    public float heightAboveFloor = 0.01f;
    [Min(0)] public int initialSplats = 4;

    readonly Dictionary<Vector2Int, Splat> splats = new Dictionary<Vector2Int, Splat>();
    readonly List<Vector2Int> candidates = new List<Vector2Int>();
    float timer;

    IEnumerator Start()
    {
        timer = NextWait();
        // Wait a frame so starting furniture has claimed its cells.
        yield return null;
        for (int i = 0; i < initialSplats; i++) TrySpawn();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = NextWait();
        if (splats.Count < maxSplats) TrySpawn();
    }

    float NextWait() => Mathf.Max(0.1f, interval + Random.Range(-jitter, jitter));

    void TrySpawn()
    {
        candidates.Clear();
        for (int x = 0; x < grid.Columns; x++)
            for (int y = 0; y < grid.Rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!grid.IsRaised(cell) && !grid.IsOccupied(cell) && !splats.ContainsKey(cell))
                    candidates.Add(cell);
            }
        if (candidates.Count == 0) return;

        var chosen = candidates[Random.Range(0, candidates.Count)];
        var position = grid.CellToWorld(chosen) + Vector3.up * heightAboveFloor;
        var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * splatPrefab.transform.rotation;

        var splat = Instantiate(splatPrefab, position, rotation, transform);
        splat.Cell = chosen;
        splat.Cleaned += OnCleaned;
        splats[chosen] = splat;
    }

    void OnCleaned(Splat splat)
    {
        if (splats.TryGetValue(splat.Cell, out var s) && s == splat) splats.Remove(splat.Cell);
    }
}
