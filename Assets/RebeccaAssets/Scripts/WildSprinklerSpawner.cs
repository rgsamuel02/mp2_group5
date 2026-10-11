
using UnityEngine;

public class WildSprinklerSpawner : MonoBehaviour
{
    [Header("Wild Sprinkler")]
    public GameObject wildSprinklerPrefab;

    [Header("Spawn Position")]
    public Vector3 spawnPosition = new Vector3(2.5f, 0.1f, 2.5f);
    public Vector3 spawnRotation = Vector3.zero;

    private void Start()
    {
        if (wildSprinklerPrefab == null)
        {
            Debug.LogError("Assign the Wild Sprinkler Prefab in the Inspector.");
            return;
        }

        Instantiate(
            wildSprinklerPrefab,
            spawnPosition,
            Quaternion.Euler(spawnRotation)
        );

        Debug.Log("Wild sprinkler spawned in the garden!");
    }
}