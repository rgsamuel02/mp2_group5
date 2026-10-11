
using UnityEngine;

public class Scr_Sprinkler : MonoBehaviour
{
    [Header("Sprinkler Stats")]
    [Min(0f)] public float maxWaterReserve = 20f;
    [Min(0f)] public float waterReserve = 20f;

    [Header("Watering Settings")]
    [Min(0f)] public float waterUsedPerAction = 1f;
    [Min(0f)] public float moistureAddedPerAction = 15f;

    [Header("Refill Settings")]
    [Min(0f)] public float waterAddedPerRefill = 5f;

    public bool IsEmpty => waterReserve < waterUsedPerAction;
    public bool IsFull => waterReserve >= maxWaterReserve;

    private void Awake()
    {
        waterReserve = Mathf.Clamp(
            waterReserve, 0f, maxWaterReserve
        );
    }

    public bool WaterPlant(Scr_Plant plant)
    {
        if (plant == null || plant.IsDead)
            return false;

        if (IsEmpty)
        {
            Debug.Log("The sprinkler has run out of water.");
            return false;
        }

        waterReserve -= waterUsedPerAction;
        waterReserve = Mathf.Max(0f, waterReserve);

        plant.Water();

        Debug.Log("Sprinkler water reserve: " + waterReserve);
        return true;
    }

    public bool Refill(float amount)
    {
        if (amount <= 0f || IsFull)
        {
            Debug.Log("The sprinkler is already full or cannot be refilled.");
            return false;
        }

        float previousReserve = waterReserve;
        waterReserve = Mathf.Min(
            maxWaterReserve, waterReserve + amount
        );

        Debug.Log(
            "Sprinkler refilled: +" +
            (waterReserve - previousReserve) +
            ". Water reserve: " + waterReserve + "/" +
            maxWaterReserve
        );

        return waterReserve > previousReserve;
    }

    public bool RefillOnce()
    {
        return Refill(waterAddedPerRefill);
    }
}