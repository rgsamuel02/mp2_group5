using UnityEngine;

public class Scr_SprinklerSpawner : MonoBehaviour
{
    [Header("Sprinkler Prefab")]
    public GameObject sprinklerPrefab;

    [Header("Garden Grid")]
    public Scr_Builder builder;

    [Header("Spawn Height")]
    public float spawnY = 0.1f;

    private void Start()
    {
        // Find the builder automatically if one was not assigned.
        if (builder == null)
        {
            builder = FindFirstObjectByType<Scr_Builder>();
        }

        if (builder.gridSize < 2 || builder.cellSize <= 0f)
        {
            Debug.LogError(
                "SprinklerSpawner: Invalid builder grid settings."
            );
            return;
        }

        // Choose an interior grid corner, excluding the outer edges.
        int xIndex = Random.Range(1, builder.gridSize);
        int zIndex = Random.Range(1, builder.gridSize);

        // Match Scr_Builder's world-coordinate grid system.
        Vector3 spawnPosition = new Vector3(
            xIndex * builder.cellSize,
            spawnY,
            zIndex * builder.cellSize
        );

        // Spawn exactly one sprinkler.
        Instantiate(
            sprinklerPrefab,
            spawnPosition,
            sprinklerPrefab.transform.rotation
        );

        Debug.Log(
            $"Sprinkler spawned at grid corner ({xIndex}, {zIndex}), " +
            $"world position {spawnPosition}"
        );
    }
}