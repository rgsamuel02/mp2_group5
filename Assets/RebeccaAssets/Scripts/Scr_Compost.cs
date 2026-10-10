using UnityEngine;

public class Scr_Compost : MonoBehaviour
{
    [Header("Compost Stats")]
    [Range(0, 100)] public float compostProgress = 0f;

    [Header("Compost Settings")]
    public float progressPerMaterial = 25f;
    public float fertilizerPerBatch = 1f;

    public void AddCompostMaterials(int amount)
    {
        if (amount <= 0)
            return;

        Scr_Inventory inventory =
            FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        if (!inventory.UseResource(
            GardenResource.CompostMaterials, amount))
        {
            return;
        }

        compostProgress = Mathf.Clamp(
            compostProgress + progressPerMaterial * amount,
            0f,
            100f
        );

        Debug.Log("Compost progress: " + compostProgress);

        if (compostProgress >= 100f)
        {
            int fertilizerAmount =
                Mathf.RoundToInt(fertilizerPerBatch);

            inventory.AddResource(
                GardenResource.Fertilizer,
                fertilizerAmount
            );

            compostProgress = 0f;

            Debug.Log("Compost finished! Fertilizer produced.");
        }
    }
}