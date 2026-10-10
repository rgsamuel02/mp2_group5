using UnityEngine;

public class Scr_Flower : MonoBehaviour
{
    [Header("Flower Stats")]
    [Range(0, 100)] public float bloom = 0f;
    [Min(0)] public int flowerCount = 0;

    [Header("Bloom Settings")]
    public float bloomPerSecond = 1f;
    public int maximumFlowers = 6;

    void Update()
    {
        bloom = Mathf.Clamp(
            bloom + bloomPerSecond * Time.deltaTime,
            0f,
            100f
        );

        flowerCount = Mathf.RoundToInt(
            Mathf.Lerp(0f, maximumFlowers, bloom / 100f)
        );
    }

    public void Harvest()
    {
        if (bloom < 100f)
        {
            Debug.Log("The flowers are not ready to harvest.");
            return;
        }

        Scr_Inventory inventory =
            FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        inventory.AddResource(GardenResource.Flowers, flowerCount);

        Debug.Log("Harvested " + flowerCount + " flowers.");

        bloom = 0f;
        flowerCount = 0;
    }
}