using UnityEngine;

public class Scr_CarPlant : MonoBehaviour
{
    [Header("Carrot Stats")]
    [Range(0, 100)] public float herbGrowth = 0f;
    [Min(0)] public int herbYield = 0;

    [Header("Growth Settings")]
    public float growthPerSecond = 1f;
    public float maximumYield = 5f;

    void Update()
    {
        herbGrowth = Mathf.Clamp(
            herbGrowth + growthPerSecond * Time.deltaTime,
            0f,
            100f
        );

        herbYield = Mathf.RoundToInt(
            Mathf.Lerp(0f, maximumYield, herbGrowth / 100f)
        );
    }

    public void Harvest()
    {
        if (herbGrowth < 100f)
        {
            Debug.Log("The herbs are not ready to harvest.");
            return;
        }

        Scr_Inventory inventory =
            FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        inventory.AddResource(GardenResource.Herbs, herbYield);

        Debug.Log("Harvested " + herbYield + " herbs.");

        herbGrowth = 0f;
        herbYield = 0;
    }
}