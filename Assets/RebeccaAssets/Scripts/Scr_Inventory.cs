
using UnityEngine;

public enum GardenResource
{
    Water,
    Seeds,
    Tomatoes,
    Herbs,
    Flowers,
    Fertilizer,
    CompostMaterials,
    Soil,
    Wood,
    Stone,
    Gold,
    GardenTools,
    RestaurantSupplyPoints
}

public class Scr_Inventory : MonoBehaviour
{
    [Header("Player Resources")]
    public int water = 20;
    public int seeds = 10;
    public int tomatoes = 0;
    public int herbs = 0;
    public int flowers = 0;
    public int fertilizer = 0;
    public int compostMaterials = 0;
    public int soil = 10;
    public int wood = 0;
    public int stone = 0;
    public int gold = 0;
    public int gardenTools = 1;
    public int restaurantSupplyPoints = 0;

    public int GetAmount(GardenResource resource)
    {
        switch (resource)
        {
            case GardenResource.Water: return water;
            case GardenResource.Seeds: return seeds;
            case GardenResource.Tomatoes: return tomatoes;
            case GardenResource.Herbs: return herbs;
            case GardenResource.Flowers: return flowers;
            case GardenResource.Fertilizer: return fertilizer;
            case GardenResource.CompostMaterials: return compostMaterials;
            case GardenResource.Soil: return soil;
            case GardenResource.Wood: return wood;
            case GardenResource.Stone: return stone;
            case GardenResource.Gold: return gold;
            case GardenResource.GardenTools: return gardenTools;
            case GardenResource.RestaurantSupplyPoints:
                return restaurantSupplyPoints;
            default: return 0;
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

    void ChangeResource(GardenResource resource, int amount)
    {
        switch (resource)
        {
            case GardenResource.Water: water += amount; break;
            case GardenResource.Seeds: seeds += amount; break;
            case GardenResource.Tomatoes: tomatoes += amount; break;
            case GardenResource.Herbs: herbs += amount; break;
            case GardenResource.Flowers: flowers += amount; break;
            case GardenResource.Fertilizer: fertilizer += amount; break;
            case GardenResource.CompostMaterials:
                compostMaterials += amount;
                break;
            case GardenResource.Soil: soil += amount; break;
            case GardenResource.Wood: wood += amount; break;
            case GardenResource.Stone: stone += amount; break;
            case GardenResource.Gold: gold += amount; break;
            case GardenResource.GardenTools: gardenTools += amount; break;
            case GardenResource.RestaurantSupplyPoints:
                restaurantSupplyPoints += amount;
                break;
        }

        ShowResources();
    }

    // Keep these methods so the existing GardenBuilder and GardenPlant
    // scripts can continue to call them.
    public bool UseWater(int amount)
    {
        return UseResource(GardenResource.Water, amount);
    }

    public bool UseSeeds(int amount)
    {
        return UseResource(GardenResource.Seeds, amount);
    }

    public void AddTomatoes(int amount)
    {
        AddResource(GardenResource.Tomatoes, amount);
        AddResource(GardenResource.RestaurantSupplyPoints, amount);
    }

    public void AddWater(int amount)
    {
        AddResource(GardenResource.Water, amount);
    }

    void ShowResources()
    {
        Debug.Log(
            "Inventory | Water: " + water +
            " | Seeds: " + seeds +
            " | Tomatoes: " + tomatoes +
            " | Herbs: " + herbs +
            " | Flowers: " + flowers +
            " | Fertilizer: " + fertilizer +
            " | Compost: " + compostMaterials +
            " | Soil: " + soil +
            " | Wood: " + wood +
            " | Stone: " + stone +
            " | Gold: " + gold +
            " | Tools: " + gardenTools +
            " | Supply Points: " + restaurantSupplyPoints
        );
    }
}