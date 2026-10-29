using UnityEngine;

public enum GardenResource
{
    Water,
    Fertilizer,
    Tomatoes,
    Corn,
    Flowers
}

public class Scr_Inventory : MonoBehaviour
{
    [Header("Player Resources")]
    public int water = 20;
    public int fertilizer = 10;

    public int tomatoes = 0;
    public int corn = 0;
    public int flowers = 0;

    public int GetAmount(GardenResource resource)
    {
        switch (resource)
        {
            case GardenResource.Water:
                return water;

            case GardenResource.Fertilizer:
                return fertilizer;

            case GardenResource.Tomatoes:
                return tomatoes;

            case GardenResource.Corn:
                return corn;

            case GardenResource.Flowers:
                return flowers;

            default:
                return 0;
        }
    }

    public bool UseResource(GardenResource resource, int amount)
    {
        if (amount <= 0)
            return false;

        if (GetAmount(resource) < amount)
        {
            Debug.Log("Not enough " + resource + "!");
            return false;
        }

        ChangeResource(resource, -amount);
        return true;
    }

    public void AddResource(GardenResource resource, int amount)
    {
        if (amount <= 0)
            return;

        ChangeResource(resource, amount);
    }

    private void ChangeResource(GardenResource resource, int amount)
    {
        switch (resource)
        {
            case GardenResource.Water:
                water += amount;
                break;

            case GardenResource.Fertilizer:
                fertilizer += amount;
                break;

            case GardenResource.Tomatoes:
                tomatoes += amount;
                break;

            case GardenResource.Corn:
                corn += amount;
                break;

            case GardenResource.Flowers:
                flowers += amount;
                break;
        }

        ShowResources();
    }

    // Compatibility methods for existing scripts.

    public bool UseWater(int amount)
    {
        return UseResource(GardenResource.Water, amount);
    }

    public void AddTomatoes(int amount)
    {
        AddResource(GardenResource.Tomatoes, amount);
    }

    public void AddCorn(int amount)
    {
        AddResource(GardenResource.Corn, amount);
    }

    public void AddFlowers(int amount)
    {
        AddResource(GardenResource.Flowers, amount);
    }

    public void AddWater(int amount)
    {
        AddResource(GardenResource.Water, amount);
    }

    public void AddFertilizer(int amount)
    {
        AddResource(GardenResource.Fertilizer, amount);
    }

    // Unlimited fertilizer supply: applying fertilizer does not
    // decrease the inventory count.

    public bool UseFertilizer(int amount)
    {
        return UseResource(GardenResource.Fertilizer, amount);
    }

    public int[] SaveInventory()
    {
        return new int[]
        {
            water,
            fertilizer,
            tomatoes,
            corn,
            flowers
        };
    }

    public void LoadInventory(int[] savedResources)
    {
        if (savedResources == null || savedResources.Length < 5)
            return;

        water = savedResources[0];
        fertilizer = savedResources[1];
        tomatoes = savedResources[2];
        corn = savedResources[3];
        flowers = savedResources[4];

        ShowResources();
    }

    private void ShowResources()
    {
        Debug.Log(
            "Inventory | Water: " + water +
            " | Fertilizer: " + fertilizer +
            " | Tomatoes: " + tomatoes +
            " | Corn: " + corn +
            " | Flowers: " + flowers
        );
    }
}
