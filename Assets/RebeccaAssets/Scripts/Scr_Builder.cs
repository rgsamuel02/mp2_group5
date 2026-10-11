using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Scr_Builder : MonoBehaviour
{
    [Header("VR Controls")]
    public XRRayInteractor rayInteractor;
    public InputActionReference placeAction;
    public InputActionReference selectPlotAction;
    public InputActionReference selectSeedlingAction;
    public InputActionReference selectCornAction;
    public InputActionReference selectFlowerAction;
    public InputActionReference waterAction;
    public InputActionReference fertilizerAction;
    public InputActionReference harvestAction;

    [Header("Prefabs")]
    public GameObject gardenPlotPrefab;
    public GameObject seedlingPrefab;
    public GameObject cornSeedlingPrefab;
    public GameObject flowerSeedlingPrefab;

    [Header("Grid Settings")]
    public int gridSize = 10;
    public float cellSize = 1f;
    public float plotHeight = 0.05f;
    public float seedlingHeight = 0.3f;
    public LayerMask groundLayer;

    [Header("Resource Costs")]
    [Min(1)] public int seedsPerPlant = 1;
    [Min(1)] public int waterPerAction = 1;
    [Min(1)] public int fertilizerPerAction = 1;

    [Header("Current Selection")]
    public GameObject selectedPrefab;

    private Scr_Inventory inventory;

    private readonly HashSet<InputAction> actionsEnabledHere =
        new HashSet<InputAction>();

    private void OnEnable()
    {
        EnableAction(placeAction);
        EnableAction(selectPlotAction);
        EnableAction(selectSeedlingAction);
        EnableAction(selectCornAction);
        EnableAction(selectFlowerAction);
        EnableAction(waterAction);
        EnableAction(fertilizerAction);
        EnableAction(harvestAction);
    }

    private void OnDisable()
    {
        foreach (InputAction action in actionsEnabledHere)
        {
            if (action != null && action.enabled)
                action.Disable();
        }

        actionsEnabledHere.Clear();
    }

    private void EnableAction(InputActionReference reference)
    {
        if (reference == null || reference.action == null)
            return;

        InputAction action = reference.action;

        if (!action.enabled)
        {
            action.Enable();
            actionsEnabledHere.Add(action);
        }
    }

    private void Start()
    {
        inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError(
                "Scr_Builder could not find Scr_Inventory."
            );
        }
    }

    private void Update()
    {
        // Keyboard shortcuts for testing in the Unity Editor.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                SelectPrefab(gardenPlotPrefab, "Garden plot");

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                SelectPrefab(seedlingPrefab, "Tomato seedling");

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                SelectPrefab(cornSeedlingPrefab, "Corn seedling");

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                SelectPrefab(flowerSeedlingPrefab, "Flower seedling");
        }

        if (WasPressed(selectPlotAction))
            SelectPrefab(gardenPlotPrefab, "Garden plot");

        if (WasPressed(selectSeedlingAction))
            SelectPrefab(seedlingPrefab, "Tomato seedling");

        if (WasPressed(selectCornAction))
            SelectPrefab(cornSeedlingPrefab, "Corn seedling");

        if (WasPressed(selectFlowerAction))
            SelectPrefab(flowerSeedlingPrefab, "Flower seedling");

        if (WasPressed(placeAction))
            TryPlaceObject();

        if (WasPressed(waterAction))
            TryWaterPlant();

        if (WasPressed(fertilizerAction))
            TryFertilizePlant();

        if (WasPressed(harvestAction))
            TryHarvestPlant();
    }

    private bool WasPressed(InputActionReference reference)
    {
        return reference != null &&
               reference.action != null &&
               reference.action.WasPressedThisFrame();
    }

    private void SelectPrefab(GameObject prefab, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning(label + " prefab is not assigned.");
            return;
        }

        selectedPrefab = prefab;
        Debug.Log("Selected: " + label);
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

        // Plot and crop placement must target the garden ground.
        if ((groundLayer.value &
             (1 << hit.collider.gameObject.layer)) == 0)
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

            Instantiate(
                gardenPlotPrefab,
                plotPosition,
                Quaternion.identity
            );

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

        Instantiate(
            selectedPrefab,
            plantPosition,
            Quaternion.identity
        );

        Debug.Log("Crop planted.");
    }

    private bool HasPlotAt(Vector3 cellCenter)
    {
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        foreach (Scr_Plot plot in plots)
        {
            if (plot != null &&
                SameCell(plot.transform.position, cellCenter))
                return true;
        }

        return false;
    }

    private bool HasPlantAt(Vector3 cellCenter)
    {
        // Tomato crops.
        Scr_Plant[] tomatoes =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        foreach (Scr_Plant plant in tomatoes)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
                return true;
        }

        // Corn crops.
        Scr_CornPlant[] cornPlants =
            FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None);

        foreach (Scr_CornPlant plant in cornPlants)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
                return true;
        }

        // Flower crops.
        Scr_Flower[] flowers =
            FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None);

        foreach (Scr_Flower plant in flowers)
        {
            if (plant != null && !plant.IsDead &&
                SameCell(plant.transform.position, cellCenter))
                return true;
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
        return;

        // Refill water if pointing at the water bin.
        Scr_WaterBin bin =
            hit.collider.GetComponentInParent<Scr_WaterBin>();

        if (bin != null)
        {
            if (inventory == null)
            {
                Debug.LogError("Garden inventory is missing.");
                return;
            }

            bin.Refill(inventory);
            return;
        }

        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        Scr_CornPlant corn =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        if (tomato == null && corn == null && flower == null)
        {
            Debug.Log("Point at a crop to water it.");
            return;
        }

        if ((tomato != null && tomato.IsDead) ||
            (corn != null && corn.IsDead) ||
            (flower != null && flower.IsDead))
        {
            Debug.Log("This crop is dead.");
            return;
        }

        if (inventory == null ||
            !inventory.UseWater(waterPerAction))
            return;

        if (tomato != null)
            tomato.Water();
        else if (corn != null)
            corn.Water();
        else
            flower.Water();

        Debug.Log("Crop watered.");
    }

    
    void TryFertilizePlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        // If the player is pointing at the fertilizer bin,
        // refill the inventory instead of fertilizing a plant.
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
            return;
        }

        // Otherwise, try to fertilize a plant.
        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        Scr_CornPlant corn =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        if (tomato == null && corn == null && flower == null)
        {
            Debug.Log("Point at a plant or fertilizer bin.");
            return;
        }

        if ((tomato != null && tomato.IsDead) ||
            (corn != null && corn.IsDead) ||
            (flower != null && flower.IsDead))
        {
            Debug.Log("Dead plants cannot be fertilized.");
            return;
        }

        if (inventory == null || !inventory.UseFertilizer(1))
        {
            Debug.Log("You don't have enough fertilizer.");
            return;
        }

        if (tomato != null)
            tomato.Fertilize();
        else if (corn != null)
            corn.Fertilize();
        else
            flower.Fertilize();
    }

    private void TryHarvestPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        if (tomato != null)
        {
            tomato.Harvest();
            return;
        }

        Scr_CornPlant corn =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        if (corn != null)
        {
            corn.Harvest();
            return;
        }

        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        if (flower != null)
        {
            flower.Harvest();
            return;
        }

        Debug.Log("Point at a crop to harvest it.");
    }
}