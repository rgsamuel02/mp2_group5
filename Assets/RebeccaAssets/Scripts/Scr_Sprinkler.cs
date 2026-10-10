using UnityEngine;

public class Scr_Sprinkler : MonoBehaviour
{
    [Header("Sprinkler Stats")]
    [Min(0)] public float waterReserve = 20f;

    [Header("Watering Settings")]
    public float waterUsedPerAction = 1f;
    public float moistureAddedPerAction = 15f;

    public bool WaterPlant(Scr_Plant plant)
    {
        if (plant == null)
            return false;

        if (plant.IsDead)
            return false;

        if (waterReserve < waterUsedPerAction)
        {
            Debug.Log("The sprinkler has run out of water.");
            return false;
        }

        waterReserve -= waterUsedPerAction;
        plant.Water();

        Debug.Log(
            "Sprinkler water reserve: " + waterReserve
        );

        return true;
    }

    public void Refill(float amount)
    {
        if (amount <= 0f)
            return;

        waterReserve += amount;
        Debug.Log("Sprinkler refilled. Water: " + waterReserve);
    }
}