using UnityEngine;

public class Scr_Sprinkler : MonoBehaviour
{
    [Header("Automatic Watering")]
    [Min(0.5f)]
    public float wateringInterval = 5f;

    [Min(1f)]
    public float waterAreaWidth = 4f;

    [Min(1f)]
    public float waterAreaDepth = 4f;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private float wateringTimer;

    void Start()
    {
        wateringTimer = wateringInterval;

        if (showDebugMessages)
        {
            Debug.Log("Sprinkler started. It waters plants automatically.");
        }
    }

    void Update()
    {
        wateringTimer += Time.deltaTime;

        if (wateringTimer >= wateringInterval)
        {
            WaterNearbyPlants();
            wateringTimer = 0f;
        }
    }

    void WaterNearbyPlants()
    {
        int plantsWatered = 0;

        // Water tomatoes.
        Scr_Plant[] tomatoes =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        foreach (Scr_Plant plant in tomatoes)
        {
            if (plant == null || plant.IsDead)
                continue;

            if (IsInsideWaterArea(plant.transform.position))
            {
                plant.Water();
                plantsWatered++;
            }
        }

        // Water corn.
        Scr_CornPlant[] cornPlants =
            FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None);

        foreach (Scr_CornPlant plant in cornPlants)
        {
            if (plant == null || plant.IsDead)
                continue;

            if (IsInsideWaterArea(plant.transform.position))
            {
                plant.Water();
                plantsWatered++;
            }
        }

        // Water flowers.
        Scr_Flower[] flowers =
            FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None);

        foreach (Scr_Flower plant in flowers)
        {
            if (plant == null || plant.IsDead)
                continue;

            if (IsInsideWaterArea(plant.transform.position))
            {
                plant.Water();
                plantsWatered++;
            }
        }

        if (showDebugMessages && plantsWatered > 0)
        {
            Debug.Log("Sprinkler watered " + plantsWatered + " plant(s).");
        }
    }

    bool IsInsideWaterArea(Vector3 plantPosition)
    {
        // The sprinkler's position is the center of its watering area.
        float halfWidth = waterAreaWidth / 2f;
        float halfDepth = waterAreaDepth / 2f;

        float distanceX =
            Mathf.Abs(plantPosition.x - transform.position.x);

        float distanceZ =
            Mathf.Abs(plantPosition.z - transform.position.z);

        return distanceX <= halfWidth &&
               distanceZ <= halfDepth;
    }

    void OnDrawGizmosSelected()
    {
        // Show the watering area in the Scene view.
        Gizmos.color = Color.cyan;

        Vector3 areaCenter = new Vector3(
            transform.position.x,
            transform.position.y + 0.05f,
            transform.position.z
        );

        Gizmos.DrawWireCube(
            areaCenter,
            new Vector3(waterAreaWidth, 0.1f, waterAreaDepth)
        );
    }
}