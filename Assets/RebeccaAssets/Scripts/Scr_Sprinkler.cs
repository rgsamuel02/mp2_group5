
using UnityEngine;

public class Scr_Sprinkler : MonoBehaviour
{

    [Header("Watering Settings")]
    [Min(0.1f)]
    public float wateringRadius = 2f;

    [Min(0.1f)]
    public float wateringInterval = 2f;

    [Header("Visual Effect")]
    public ParticleSystem waterSpray;

    private float wateringTimer;

    private void Start()
    {
        if (waterSpray != null)
        {
            waterSpray.Play();
        }
    }

    private void Update()
    {
        StartSpraying();

        wateringTimer += Time.deltaTime;

        if (wateringTimer >= wateringInterval)
        {
            wateringTimer = 0f;
            WaterNearbyCrops();
        }
    }

    private void StartSpraying()
    {
        if (waterSpray != null && !waterSpray.isPlaying)
        {
            waterSpray.Play();
        }
    }

    private void StopSpraying()
    {
        if (waterSpray != null && waterSpray.isPlaying)
        {
            waterSpray.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void WaterNearbyCrops()
    {
        // Find living tomato plants.
        Scr_Plant[] tomatoes =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        // Find living corn plants.
        Scr_CornPlant[] cornPlants =
            FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None);

        // Find living flowers.
        Scr_Flower[] flowers =
            FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None);

        bool wateredAnyCrop = false;

        foreach (Scr_Plant plant in tomatoes)
        {
            if (plant != null && !plant.IsDead &&
                IsInsideWateringRadius(plant.transform.position))
            {
                plant.Water();
                wateredAnyCrop = true;
            }
        }

        foreach (Scr_CornPlant plant in cornPlants)
        {
            if (plant != null && !plant.IsDead &&
                IsInsideWateringRadius(plant.transform.position))
            {
                plant.Water();
                wateredAnyCrop = true;
            }
        }

        foreach (Scr_Flower flower in flowers)
        {
            if (flower != null && !flower.IsDead &&
                IsInsideWateringRadius(flower.transform.position))
            {
                flower.Water();
                wateredAnyCrop = true;
            }
        }
    }

    private bool IsInsideWateringRadius(Vector3 position)
    {
        // Measure distance across the ground, ignoring height.
        Vector2 sprinklerPosition =
            new Vector2(transform.position.x, transform.position.z);

        Vector2 cropPosition =
            new Vector2(position.x, position.z);

        return Vector2.Distance(sprinklerPosition, cropPosition)
               <= wateringRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = transform.position;
        center.y = transform.position.y + 0.05f;

        Gizmos.DrawWireSphere(center, wateringRadius);
    }
}
