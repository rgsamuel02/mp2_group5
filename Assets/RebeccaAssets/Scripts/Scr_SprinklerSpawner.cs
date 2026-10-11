using UnityEngine;

public class Scr_SprinklerSpawner : MonoBehaviour
{
    [Header("Sprinkler Prefab")]
    public GameObject sprinklerPrefab;

    [Header("Random Spawn Area")]
    public float minX = 0f;
    public float maxX = 10f;
    public float minZ = 0f;
    public float maxZ = 10f;
    public float spawnY = 0.1f;

    [Header("Spawn Settings")]
    public bool spawnOnlyOne = true;

    void Start()
    {
        if (sprinklerPrefab == null)
        {
            Debug.LogError("Sprinkler prefab is not assigned!");
            return;
        }

        Vector3 randomPosition = new Vector3(
            Random.Range(minX, maxX),
            spawnY,
            Random.Range(minZ, maxZ)
        );

        Instantiate(
            sprinklerPrefab,
            randomPosition,
            sprinklerPrefab.transform.rotation
        );

        Debug.Log("Sprinkler spawned at: " + randomPosition);
    }
}