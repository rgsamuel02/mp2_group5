using UnityEngine;

public class Scr_Plot : MonoBehaviour
{
    [Header("Soil Stats")]
    [Range(0, 100)] public float soilQuality = 70f;
    [Range(0, 100)] public float fertility = 50f;
    [Range(0, 100)] public float moisture = 50f;

    [Header("Soil Settings")]
    public float moistureLossPerSecond = 0.25f;
    public float fertilizerBonus = 10f;

    void Update()
    {
        // Soil slowly dries out over time.
        moisture = Mathf.Clamp(
            moisture - moistureLossPerSecond * Time.deltaTime,
            0f,
            100f
        );
    }

    public void AddFertilizer()
    {
        fertility = Mathf.Clamp(
            fertility + fertilizerBonus,
            0f,
            100f
        );

        Debug.Log("Soil fertility is now " + fertility);
    }

    public void AddMoisture(float amount)
    {
        moisture = Mathf.Clamp(moisture + amount, 0f, 100f);
    }
}