
using UnityEngine;

public class Scr_Inventory : MonoBehaviour
{
    [Header("Player Resources")]
    public int water = 20;
    public int seeds = 10;
    public int tomatoes = 0;

    public bool UseWater(int amount)
    {
        if (water < amount)
        {
            Debug.Log("Not enough water!");
            return false;
        }

        water -= amount;
        ShowResources();
        return true;
    }

    public bool UseSeeds(int amount)
    {
        if (seeds < amount)
        {
            Debug.Log("Not enough seeds!");
            return false;
        }

        seeds -= amount;
        ShowResources();
        return true;
    }

    public void AddTomatoes(int amount)
    {
        tomatoes += amount;
        Debug.Log("Harvested " + amount + " tomatoes!");
        ShowResources();
    }

    public void AddWater(int amount)
    {
        water += amount;
        ShowResources();
    }

    void ShowResources()
    {
        Debug.Log(
            "Inventory | Water: " + water +
            " | Seeds: " + seeds +
            " | Tomatoes: " + tomatoes
        );
    }
}