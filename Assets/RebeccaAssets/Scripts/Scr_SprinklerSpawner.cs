using UnityEngine;

public class Scr_SprinklerSpawner : MonoBehaviour
{
    [Header("Sprinkler Prefab")]
    public GameObject sprinklerPrefab;

    [Header("Grid Settings")]
    public Scr_GridVis grid;

    [Header("Spawn Height")]
    public float spawnY = 0.1f;

    void Start()
    {
        if (grid == null)
            grid = FindFirstObjectByType<Scr_GridVis>();

        // Choose a random INTERIOR grid corner.
        // Exclude index 0 and gridSize to avoid the edges.
        int xIndex = Random.Range(1, grid.gridSize);
        int zIndex = Random.Range(1, grid.gridSize);

        Vector3 spawnPosition = grid.transform.TransformPoint(
            new Vector3(
                xIndex * grid.cellSize,
                spawnY,
                zIndex * grid.cellSize
            )
        );

        // Spawn exactly one sprinkler.
        Instantiate(
            sprinklerPrefab,
            spawnPosition,
            sprinklerPrefab.transform.rotation
        );

        Debug.Log(
            $"Sprinkler spawned at interior grid corner: {spawnPosition}"
        );
    }
}