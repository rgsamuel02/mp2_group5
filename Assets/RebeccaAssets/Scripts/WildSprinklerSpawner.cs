using UnityEngine;

public class WildSprinklerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject wildSprinklerPrefab;
    [SerializeField] private Vector3 spawnPosition =
        new Vector3(2.5f, 0.1f, 2.5f);

    private GameObject spawnedSprinkler;

    private void Start()
    {
        if (spawnedSprinkler != null)
            return;

        if (wildSprinklerPrefab == null)
        {
            Debug.LogError("Assign the wild sprinkler prefab.");
            return;
        }

        spawnedSprinkler = Instantiate(
            wildSprinklerPrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );
    }
}