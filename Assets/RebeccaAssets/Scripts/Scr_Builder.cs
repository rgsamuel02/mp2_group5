using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class Scr_Builder : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public InputActionReference placeAction;
    public InputActionReference selectPlotAction;
    public InputActionReference selectSeedlingAction;
    public InputActionReference selectCornAction;
    public InputActionReference selectFlowerAction;
    public InputActionReference waterAction;
    public InputActionReference fertilizerAction;
    public InputActionReference harvestAction;

    public GameObject gardenPlotPrefab;
    public GameObject seedlingPrefab;
    public GameObject cornSeedlingPrefab;
    public GameObject flowerSeedlingPrefab;

    public int gridSize = 10;
    public float cellSize = 1f;
    public float plotHeight = 0.05f;
    public float seedlingHeight = 0.3f;
    public LayerMask groundLayer;

    public float interactionRadius = 1.5f;

    [Min(1)] public int seedsPerPlant = 1;
    [Min(1)] public int waterPerAction = 1;
    [Min(1)] public int fertilizerPerAction = 1;

    public GameObject selectedPrefab;

    private Scr_Inventory inventory;

    public ParticleSystem waterSpray;
    public ParticleSystem fertilizerSpray;

    private void Start()
    {
        inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("Scr_Builder could not find Scr_Inventory.");
        }
        
        Enable(placeAction, OnPlace);
        Enable(selectPlotAction, OnSelectPlot);
        Enable(selectSeedlingAction, OnSelectSeedling);
        Enable(selectCornAction, OnSelectCorn);
        Enable(selectFlowerAction, OnSelectFlower);
        Enable(waterAction, OnWater);
        Enable(fertilizerAction, OnFertilize);
        Enable(harvestAction, OnHarvest);
    }


    private void Enable(
        InputActionReference reference,
        System.Action<InputAction.CallbackContext> callback)
    {
        reference.action.Enable();
        reference.action.performed += callback;
    }


    // VR input callbacks

    private void OnPlace(InputAction.CallbackContext context)
    {
        TryPlaceObject();
    }

    private void OnSelectPlot(InputAction.CallbackContext context)
    {
        SelectPrefab(gardenPlotPrefab, "Garden plot");
        TryPlaceObject();
    }

    private void OnSelectSeedling(InputAction.CallbackContext context)
    {
        SelectPrefab(seedlingPrefab, "Tomato seedling");
        TryPlantOnNearestPlot();
    }

    private void OnSelectCorn(InputAction.CallbackContext context)
    {
        SelectPrefab(cornSeedlingPrefab, "Corn seedling");
        TryPlantOnNearestPlot();
    }

    private void OnSelectFlower(InputAction.CallbackContext context)
    {
        SelectPrefab(flowerSeedlingPrefab, "Flower seedling");
        TryPlantOnNearestPlot();
    }

    private void OnWater(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        TryWaterPlant();
    }

    private void OnFertilize(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        TryFertilizePlant();
    }

    private void OnHarvest(InputAction.CallbackContext context)
    {
        TryHarvestPlant();
    }

    private void SelectPrefab(GameObject prefab, string label)
    {
        selectedPrefab = prefab;
        Debug.Log("Selected: " + label);
    }

    
    private T FindNearestTarget<T>(Vector3 targetPosition)
        where T : Component
    {
        T[] targets = FindObjectsByType<T>(FindObjectsSortMode.None);

        T nearestTarget = null;
        float nearestDistance = interactionRadius;

        foreach (T target in targets)
        {
            if (target == null)
                continue;

            float distance = Vector3.Distance(
                targetPosition,
                target.transform.position
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }


    private bool TryGetHit(out RaycastHit hit)
    {
        hit = default;

        if (rayInteractor == null)
            return false;

        return rayInteractor.TryGetCurrent3DRaycastHit(out hit);
    }

    private void TryPlaceObject()
    {
        if (selectedPrefab == null)
        {
            Debug.Log("Select a plot or plant first.");
            return;
        }

        if (!TryGetHit(out RaycastHit hit))
            return;

        bool placingPlot = selectedPrefab == gardenPlotPrefab;

        if ((groundLayer.value & (1 << hit.collider.gameObject.layer)) == 0)
        {
            Debug.Log("Point at the garden ground to place an object.");
            return;
        }

        int cellX = Mathf.FloorToInt(hit.point.x / cellSize);
        int cellZ = Mathf.FloorToInt(hit.point.z / cellSize);

        if (cellX < 0 || cellX >= gridSize ||
            cellZ < 0 || cellZ >= gridSize)
        {
            Debug.Log("That position is outside the garden grid.");
            return;
        }

        Vector3 cellCenter = new Vector3(
            (cellX + 0.5f) * cellSize,
            0f,
            (cellZ + 0.5f) * cellSize
        );

        if (placingPlot)
        {
            if (HasPlotAt(cellCenter))
            {
                Debug.Log("There is already a plot in this cell.");
                return;
            }

            Vector3 plotPosition = cellCenter;
            plotPosition.y = plotHeight;

            Instantiate(gardenPlotPrefab, plotPosition, Quaternion.identity);
            Debug.Log("Garden plot placed.");
            return;
        }

        if (!HasPlotAt(cellCenter))
        {
            Debug.Log("Place a garden plot before planting.");
            return;
        }

        if (HasPlantAt(cellCenter))
        {
            Debug.Log("This plot already has a living crop.");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("Scr_Inventory is missing.");
            return;
        }

        if (!inventory.UseSeeds(seedsPerPlant))
            return;

        Vector3 plantPosition = cellCenter;
        plantPosition.y = seedlingHeight;

        Instantiate(selectedPrefab, plantPosition, Quaternion.identity);
        Debug.Log("Crop planted.");
    }

    private bool HasPlotAt(Vector3 cellCenter)
    {
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        int targetX = Mathf.FloorToInt(cellCenter.x / cellSize);
        int targetZ = Mathf.FloorToInt(cellCenter.z / cellSize);

        Debug.Log($"Looking for plot in cell ({targetX}, {targetZ}). Found {plots.Length} plot components.");

        foreach (Scr_Plot plot in plots)
        {
            if (plot == null)
                continue;

            int plotX = Mathf.FloorToInt(plot.transform.position.x / cellSize);
            int plotZ = Mathf.FloorToInt(plot.transform.position.z / cellSize);

            Debug.Log(
                $"Plot: {plot.name}, Position: {plot.transform.position}, " +
                $"Cell: ({plotX}, {plotZ})"
            );

            if (plotX == targetX && plotZ == targetZ)
            {
                Debug.Log("Found a plot in the target cell.");
                return true;
            }
        }

        Debug.Log("No plot found in the target cell.");
        return false;
    }

    private bool HasPlantAt(Vector3 cellCenter)
    {
        Scr_Plant[] tomatoes =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        foreach (Scr_Plant plant in tomatoes)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
            {
                return true;
            }
        }

        Scr_CornPlant[] cornPlants =
            FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None);

        foreach (Scr_CornPlant plant in cornPlants)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
            {
                return true;
            }
        }

        Scr_Flower[] flowers =
            FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None);

        foreach (Scr_Flower plant in flowers)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
            {
                return true;
            }
        }

        return false;
    }

    private bool SameCell(Vector3 position, Vector3 cellCenter)
    {
        int x1 = Mathf.FloorToInt(position.x / cellSize);
        int z1 = Mathf.FloorToInt(position.z / cellSize);

        int x2 = Mathf.FloorToInt(cellCenter.x / cellSize);
        int z2 = Mathf.FloorToInt(cellCenter.z / cellSize);

        return x1 == x2 && z1 == z2;
    }

   
    private void TryWaterPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
        {
            Debug.Log("Point toward a plant or water bin.");
            return;
        }

        Vector3 targetPosition = hit.point;

        Scr_WaterBin bin = FindNearestTarget<Scr_WaterBin>(targetPosition);

        Scr_Plant tomato = FindNearestTarget<Scr_Plant>(targetPosition);
        Scr_CornPlant corn = FindNearestTarget<Scr_CornPlant>(targetPosition);
        Scr_Flower flower = FindNearestTarget<Scr_Flower>(targetPosition);

        // Prefer a nearby water bin when it is closer than the plants.
        float binDistance = bin != null
            ? Vector3.Distance(targetPosition, bin.transform.position)
            : float.MaxValue;

        float tomatoDistance = tomato != null
            ? Vector3.Distance(targetPosition, tomato.transform.position)
            : float.MaxValue;

        float cornDistance = corn != null
            ? Vector3.Distance(targetPosition, corn.transform.position)
            : float.MaxValue;

        float flowerDistance = flower != null
            ? Vector3.Distance(targetPosition, flower.transform.position)
            : float.MaxValue;

        float nearestPlantDistance = Mathf.Min(
            tomatoDistance, cornDistance, flowerDistance);

        if (bin != null && binDistance <= nearestPlantDistance)
        {
            if (inventory == null)
            {
                Debug.LogError("Garden inventory is missing.");
                return;
            }

            bin.Refill(inventory);
            return;
        }

        if (nearestPlantDistance == float.MaxValue)
        {
            Debug.Log("No plant or water bin nearby.");
            return;
        }

        // Select the closest plant.
        if (tomatoDistance <= cornDistance &&
            tomatoDistance <= flowerDistance)
        {
            if (tomato.IsDead)
            {
                Debug.Log("Dead plants cannot be watered.");
                return;
            }
        }
        else if (cornDistance <= flowerDistance)
        {
            if (corn.IsDead)
            {
                Debug.Log("Dead plants cannot be watered.");
                return;
            }
        }
        else if (flower.IsDead)
        {
            Debug.Log("Dead plants cannot be watered.");
            return;
        }

        if (inventory == null || !inventory.UseWater(waterPerAction))
            return;

        if (tomatoDistance <= cornDistance &&
            tomatoDistance <= flowerDistance)
            tomato.Water();
        else if (cornDistance <= flowerDistance)
            corn.Water();
        else
            flower.Water();

        Debug.Log("Crop watered.");
    }


    
    private void TryPlantOnNearestPlot()
    {
        if (selectedPrefab == null)
        {
            Debug.Log("Select a seedling first.");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("Scr_Inventory is missing.");
            return;
        }

        // Find where the controller ray is pointing.
        if (!TryGetHit(out RaycastHit hit))
        {
            Debug.Log("Point toward a garden plot.");
            return;
        }

        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        Scr_Plot nearestPlot = null;
        float nearestDistance = float.MaxValue;

        foreach (Scr_Plot plot in plots)
        {
            if (plot == null)
                continue;

            Vector3 plotPosition = plot.transform.position;

            // Only consider plots within the garden grid.
            int x = Mathf.FloorToInt(plotPosition.x / cellSize);
            int z = Mathf.FloorToInt(plotPosition.z / cellSize);

            if (x < 0 || x >= gridSize || z < 0 || z >= gridSize)
                continue;

            // Measure distance from the ray's hit point to this plot.
            Vector2 hitXZ = new Vector2(hit.point.x, hit.point.z);
            Vector2 plotXZ = new Vector2(plotPosition.x, plotPosition.z);

            float distance = Vector2.Distance(hitXZ, plotXZ);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlot = plot;
            }
        }

        if (nearestPlot == null)
        {
            Debug.Log("No garden plots found.");
            return;
        }

        Vector3 cellCenter = new Vector3(
            (Mathf.Floor(nearestPlot.transform.position.x / cellSize) + 0.5f) * cellSize,
            0f,
            (Mathf.Floor(nearestPlot.transform.position.z / cellSize) + 0.5f) * cellSize
        );

        if (HasPlantAt(cellCenter))
        {
            Debug.Log("The nearest plot is occupied. Point toward another plot.");
            return;
        }

        if (!inventory.UseSeeds(seedsPerPlant))
            return;

        Vector3 plantPosition = cellCenter;
        plantPosition.y = seedlingHeight;

        Quaternion plantRotation = Quaternion.identity;

        Instantiate(selectedPrefab, plantPosition, plantRotation);

        Debug.Log("Seedling planted on the nearest plot.");
    }


    private void TryFertilizePlant()
    {
        if (!TryGetHit(out RaycastHit hit))
        {
            Debug.LogWarning("Fertilizer: Ray is not hitting anything.");
            return;
        }

        // First, check whether the ray directly hits the fertilizer bin.
        Scr_FertilizerBin bin =
            hit.collider.GetComponentInParent<Scr_FertilizerBin>();

        if (bin != null)
        {
            if (inventory == null)
            {
                Debug.LogError("Garden inventory is missing.");
                return;
            }

            bin.Refill(inventory);
            Debug.Log("Fertilizer bin used.");
            return;
        }

        // Check for a plant directly hit by the ray.
        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        Scr_CornPlant corn =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        // If the ray missed a plant, look for a nearby one.
        if (tomato == null && corn == null && flower == null)
        {
            Vector3 targetPosition = hit.point;

            tomato = FindNearestTarget<Scr_Plant>(targetPosition);
            corn = FindNearestTarget<Scr_CornPlant>(targetPosition);
            flower = FindNearestTarget<Scr_Flower>(targetPosition);
        }

        // Pick the nearest plant among the candidates.
        Component nearestPlant = null;
        float nearestDistance = float.MaxValue;

        if (tomato != null)
        {
            nearestPlant = tomato;
            nearestDistance = Vector3.Distance(hit.point, tomato.transform.position);
        }

        if (corn != null)
        {
            float distance = Vector3.Distance(hit.point, corn.transform.position);
            if (distance < nearestDistance)
            {
                nearestPlant = corn;
                nearestDistance = distance;
            }
        }

        if (flower != null)
        {
            float distance = Vector3.Distance(hit.point, flower.transform.position);
            if (distance < nearestDistance)
            {
                nearestPlant = flower;
                nearestDistance = distance;
            }
        }

        if (nearestPlant == null)
        {
            Debug.LogWarning("Fertilizer: No plant or fertilizer bin found.");
            return;
        }

        if (nearestPlant is Scr_Plant t && t.IsDead ||
            nearestPlant is Scr_CornPlant c && c.IsDead ||
            nearestPlant is Scr_Flower f && f.IsDead)
        {
            Debug.Log("Dead plants cannot be fertilized.");
            return;
        }

        if (inventory == null ||
            !inventory.UseFertilizer(fertilizerPerAction))
        {
            Debug.Log("You don't have enough fertilizer.");
            return;
        }

        if (nearestPlant is Scr_Plant tomatoPlant)
            tomatoPlant.Fertilize();
        else if (nearestPlant is Scr_CornPlant cornPlant)
            cornPlant.Fertilize();
        else if (nearestPlant is Scr_Flower flowerPlant)
            flowerPlant.Fertilize();

        Debug.Log("Plant fertilized.");
    }

    private void StartWaterEffect()
    {
        if (waterSpray != null && !waterSpray.isPlaying)
            waterSpray.Play();
    }

    private void StopWaterEffect()
    {
        if (waterSpray != null)
            waterSpray.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
    }

    private void StartFertilizerEffect()
    {
        if (fertilizerSpray != null && !fertilizerSpray.isPlaying)
            fertilizerSpray.Play();
    }

    private void StopFertilizerEffect()
    {
        if (fertilizerSpray != null)
            fertilizerSpray.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
    }
    private void TryHarvestPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
        {
            Debug.LogWarning("Harvest: Ray is not hitting anything.");
            return;
        }

        // First, check whether the ray hit the plant root
        // or one of its visible child models.
        Scr_Plant tomato = hit.collider.GetComponentInParent<Scr_Plant>();
        Scr_CornPlant corn = hit.collider.GetComponentInParent<Scr_CornPlant>();
        Scr_Flower flower = hit.collider.GetComponentInParent<Scr_Flower>();

        if (tomato != null)
        {
            HarvestTomato(tomato);
            return;
        }

        if (corn != null)
        {
            HarvestCorn(corn);
            return;
        }

        if (flower != null)
        {
            HarvestFlower(flower);
            return;
        }

        // If the ray hit the ground near a plant, find the
        // closest plant using horizontal distance.
        Vector3 targetPosition = hit.point;

        Component nearestPlant = null;
        float nearestDistance = float.MaxValue;

        foreach (Scr_Plant plant in FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None))
        {
            if (plant == null) continue;

            float distance = Vector2.Distance(
                new Vector2(targetPosition.x, targetPosition.z),
                new Vector2(plant.transform.position.x, plant.transform.position.z));

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlant = plant;
            }
        }

        foreach (Scr_CornPlant plant in FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None))
        {
            if (plant == null) continue;

            float distance = Vector2.Distance(
                new Vector2(targetPosition.x, targetPosition.z),
                new Vector2(plant.transform.position.x, plant.transform.position.z));

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlant = plant;
            }
        }

        foreach (Scr_Flower plant in FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None))
        {
            if (plant == null) continue;

            float distance = Vector2.Distance(
                new Vector2(targetPosition.x, targetPosition.z),
                new Vector2(plant.transform.position.x, plant.transform.position.z));

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlant = plant;
            }
        }

        const float maxHarvestDistance = 3f;

        if (nearestPlant == null || nearestDistance > maxHarvestDistance)
        {
            Debug.LogWarning(
                $"Harvest: No plant nearby. Closest distance: {nearestDistance:F2}. " +
                "Check that the plant scripts are attached to active root objects."
            );
            return;
        }

        if (nearestPlant is Scr_Plant nearestTomato)
            HarvestTomato(nearestTomato);
        else if (nearestPlant is Scr_CornPlant nearestCorn)
            HarvestCorn(nearestCorn);
        else if (nearestPlant is Scr_Flower nearestFlower)
            HarvestFlower(nearestFlower);
    }

    private void HarvestTomato(Scr_Plant plant)
    {
        if (plant.IsDead)
        {
            Destroy(plant.gameObject);
            Debug.Log("Dead tomato removed. No resources collected.");
            return;
        }

        plant.Harvest();
    }

    private void HarvestCorn(Scr_CornPlant plant)
    {
        if (plant.IsDead)
        {
            Destroy(plant.gameObject);
            Debug.Log("Dead corn removed. No resources collected.");
            return;
        }

        plant.Harvest();
    }

    private void HarvestFlower(Scr_Flower plant)
    {
        if (plant.IsDead)
        {
            Destroy(plant.gameObject);
            Debug.Log("Dead flower removed. No resources collected.");
            return;
        }

        plant.Harvest();
    }



}
