using UnityEngine;

public class Scr_TomPlant : MonoBehaviour
{
    [Header("Tomato Stats")]
    [Min(0)] public int fruitCount = 0;
    [Range(0, 100)] public float ripeness = 0f;

    [Header("Ripeness Settings")]
    public float ripenessGrowthStart = 67f;
    public float fullyRipeAt = 100f;

    private Scr_Plant plant;

    void Start()
    {
        plant = GetComponent<Scr_Plant>();

        if (plant == null)
        {
            Debug.LogError(
                "TomatoPlantStats needs GardenPlant on the same object."
            );
        }
    }

    void Update()
    {
        if (plant == null || plant.IsDead)
            return;

        // Fruit begins developing in the final growth stage.
        float growth = plant.growth;

        float progress = Mathf.InverseLerp(
            ripenessGrowthStart,
            fullyRipeAt,
            growth
        );

        ripeness = progress * 100f;

        // Example yield: fruit appears as the plant grows.
        fruitCount = Mathf.RoundToInt(
            Mathf.Lerp(0f, 3f, progress)
        );
    }
}