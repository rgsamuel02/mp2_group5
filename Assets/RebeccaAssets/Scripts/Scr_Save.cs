using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GardenAchievementSaveData
{
    public int tomatoesHarvested;
    public int cornHarvested;
    public int flowersHarvested;

    public bool tomatoAchievementUnlocked;
    public bool cornAchievementUnlocked;
    public bool flowerAchievementUnlocked;
}

[System.Serializable]
public class GardenEconomySaveData
{
    public int walletBalanceCents;
    public int totalEarnedFromSalesCents;

    public int tomatoesHarvested;
    public int coffeeBeansHarvested;
    public int flowersHarvested;

    public bool restaurantUnlocked;
    public bool cafeUnlocked;
    public bool orchestraUnlocked;
    public bool challengeCompleted;
}

[System.Serializable]
public class TomatoPlantSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public float health;
    public float moisture;
    public float growth;
    public float age;
    public bool isDead;
}

[System.Serializable]
public class CornPlantSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public float health;
    public float moisture;
    public float growth;
    public float age;
    public int herbYield;
    public bool isDead;
}

[System.Serializable]
public class FlowerPlantSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public float health;
    public float moisture;
    public float growth;
    public float age;
    public float bloom;
    public int flowerCount;
    public bool isDead;
}

[System.Serializable]
public class GardenPlacedObjectSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[System.Serializable]
public class GardenSaveData
{
    public int[] inventory;
    public GardenEconomySaveData economyData;
    public int walletBalanceCents;
    public int totalEarnedFromSalesCents;
    public GardenAchievementSaveData achievementData;

    public List<TomatoPlantSaveData> tomatoPlants =
        new List<TomatoPlantSaveData>();

    public List<CornPlantSaveData> cornPlants =
        new List<CornPlantSaveData>();

    public List<FlowerPlantSaveData> flowerPlants =
        new List<FlowerPlantSaveData>();

    public List<GardenPlacedObjectSaveData> placedPlots =
        new List<GardenPlacedObjectSaveData>();
}


public class Scr_Save : MonoBehaviour
{
    public static Scr_Save Instance { get; private set; }

    public Scr_Inventory inventory;
    public Scr_GardenWallet wallet;
    public Scr_GardenEconomy economy;
    public Scr_Ach achievements;

    public GameObject tomatoSeedlingPrefab;
    public GameObject cornSeedlingPrefab;
    public GameObject flowerSeedlingPrefab;
    public GameObject plotPrefab;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "gardenSave.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SaveGarden()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("Cannot save garden: inventory not found.");
            return;
        }

        GardenSaveData data = new GardenSaveData();
        data.inventory = inventory.SaveInventory();

        // Save wallet values
        data.walletBalanceCents = wallet.BalanceCents;
        data.totalEarnedFromSalesCents = wallet.TotalEarnedFromSalesCents;

        // Save economy progress
        data.economyData = economy.CaptureSaveData();

        data.achievementData = achievements.CaptureSaveData();

        Scr_Plant[] plants = FindObjectsByType<Scr_Plant>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Scr_Plant plant in plants)
        {
            if (plant == null)
                continue;

            data.tomatoPlants.Add(new TomatoPlantSaveData
            {
                position = plant.transform.position,
                rotation = plant.transform.rotation,
                scale = plant.transform.localScale,
                health = plant.health,
                moisture = plant.moisture,
                growth = plant.growth,
                age = plant.age,
                isDead = plant.IsDead
            });
        }
                
        Scr_CornPlant[] cornPlants = FindObjectsByType<Scr_CornPlant>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Scr_CornPlant plant in cornPlants)
        {
            if (plant == null)
                continue;

            data.cornPlants.Add(new CornPlantSaveData
            {
                position = plant.transform.position,
                rotation = plant.transform.rotation,
                scale = plant.transform.localScale,
                health = plant.health,
                moisture = plant.moisture,
                growth = plant.growth,
                age = plant.age,
                herbYield = plant.herbYield,
                isDead = plant.IsDead
            });
        }

        Scr_Flower[] flowerPlants = FindObjectsByType<Scr_Flower>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Scr_Flower plant in flowerPlants)
        {
            if (plant == null)
                continue;

            data.flowerPlants.Add(new FlowerPlantSaveData
            {
                position = plant.transform.position,
                rotation = plant.transform.rotation,
                scale = plant.transform.localScale,
                health = plant.health,
                moisture = plant.moisture,
                growth = plant.growth,
                age = plant.age,
                bloom = plant.bloom,
                flowerCount = plant.flowerCount,
                isDead = plant.IsDead
            });
        }

        // Save placed plots
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Scr_Plot plot in plots)
        {
            data.placedPlots.Add(new GardenPlacedObjectSaveData
            {
                position = plot.transform.position,
                rotation = plot.transform.rotation,
                scale = plot.transform.localScale
            });
        }
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Garden saved successfully to: " + SavePath);
    }

    public void LoadGarden()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No garden save file found. Starting a new garden.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        GardenSaveData data = JsonUtility.FromJson<GardenSaveData>(json);

        if (data == null)
        {
            Debug.LogError("Garden save file could not be loaded.");
            return;
        }

        // Restore inventory
        inventory.LoadInventory(data.inventory);

        // Restore wallet
        wallet.RestoreWallet(
            data.walletBalanceCents,
            data.totalEarnedFromSalesCents
        );

        // Restore economy progress
        economy.RestoreSaveData(data.economyData);

        achievements.RestoreSaveData(data.achievementData);

        // Remove existing plants so they are not duplicated
        Scr_Plant[] existingTomatoes =
            FindObjectsByType<Scr_Plant>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Scr_Plant plant in existingTomatoes)
        {
            Destroy(plant.gameObject);
        }

        Scr_CornPlant[] existingCorn =
            FindObjectsByType<Scr_CornPlant>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Scr_CornPlant plant in existingCorn)
        {
            Destroy(plant.gameObject);
        }

        Scr_Flower[] existingFlowers =
            FindObjectsByType<Scr_Flower>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Scr_Flower plant in existingFlowers)
        {
            Destroy(plant.gameObject);
        }

        // Remove existing plots so they are not duplicated
        Scr_Plot[] existingPlots =
            FindObjectsByType<Scr_Plot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Scr_Plot plot in existingPlots)
        {
            Destroy(plot.gameObject);
        }

        // Restore saved plots
        if (data.placedPlots != null)
        {
            foreach (GardenPlacedObjectSaveData saved in data.placedPlots)
            {
                GameObject plotObject = Instantiate(
                    plotPrefab,
                    saved.position,
                    saved.rotation
                );

                plotObject.transform.localScale = saved.scale;
            }
        }

        // Restore tomato plants
        if (data.tomatoPlants != null)
        {
            foreach (TomatoPlantSaveData saved in data.tomatoPlants)
            {
                GameObject plantObject = Instantiate(
                    tomatoSeedlingPrefab,
                    saved.position,
                    saved.rotation
                );

                plantObject.transform.localScale = saved.scale;

                Scr_Plant plant = plantObject.GetComponent<Scr_Plant>();

                plant.RestorePlant(
                    saved.health,
                    saved.moisture,
                    saved.growth,
                    saved.age,
                    saved.isDead
                );
            }

        }

        // Restore corn plants
        if (data.cornPlants != null)
        {
            foreach (CornPlantSaveData saved in data.cornPlants)
            {
                GameObject plantObject = Instantiate(
                    cornSeedlingPrefab,
                    saved.position,
                    saved.rotation
                );

                plantObject.transform.localScale = saved.scale;

                Scr_CornPlant plant =
                    plantObject.GetComponent<Scr_CornPlant>();

                plant.RestorePlant(
                    saved.health,
                    saved.moisture,
                    saved.growth,
                    saved.age,
                    saved.herbYield,
                    saved.isDead
                );
            }
        }

        // Restore flower plants
        if (data.flowerPlants != null)
        {
            foreach (FlowerPlantSaveData saved in data.flowerPlants)
            {
                GameObject plantObject = Instantiate(
                    flowerSeedlingPrefab,
                    saved.position,
                    saved.rotation
                );

                plantObject.transform.localScale = saved.scale;

                Scr_Flower plant =
                    plantObject.GetComponent<Scr_Flower>();

                plant.RestorePlant(
                    saved.health,
                    saved.moisture,
                    saved.growth,
                    saved.age,
                    saved.bloom,
                    saved.flowerCount,
                    saved.isDead
                );
            }
        }

        Debug.Log("Garden inventory and saved plants restored.");
    }
}
