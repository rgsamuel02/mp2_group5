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
    public InputActionReference selectHerbAction;
    public InputActionReference selectFlowerAction;
    public InputActionReference waterAction;
    public InputActionReference harvestAction;

    [Header("Prefabs")]
    public GameObject gardenPlotPrefab;
    public GameObject seedlingPrefab;
    public GameObject herbSeedlingPrefab;
    public GameObject flowerSeedlingPrefab;

    [Header("Grid Settings")]
    public int gridSize = 10;
    public float cellSize = 1f;
    public float plotHeight = 0.05f;
    public float seedlingHeight = 0.3f;
    public LayerMask groundLayer;

    [Header("Current Selection")]
    public GameObject selectedPrefab;

    private Scr_Inventory inventory;
    private readonly HashSet<InputAction> actionsEnabledHere =
        new HashSet<InputAction>();

    void OnEnable()
    {
        EnableAction(placeAction);
        EnableAction(selectPlotAction);
        EnableAction(selectSeedlingAction);
        EnableAction(selectHerbAction);
        EnableAction(selectFlowerAction);
        EnableAction(waterAction);
        EnableAction(harvestAction);
    }

    void OnDisable()
    {
        foreach (InputAction action in actionsEnabledHere)
        {
            if (action != null && action.enabled)
                action.Disable();
        }

        actionsEnabledHere.Clear();
    }

    void EnableAction(InputActionReference reference)
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

    void Start()
    {
        inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError(
                "GardenBuilder could not find GardenInventory."
            );
        }
    }

    void Update()
    {
        // Keyboard shortcuts for testing in the Editor.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                SelectPrefab(gardenPlotPrefab, "Garden plot");

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                SelectPrefab(seedlingPrefab, "Tomato seedling");

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                SelectPrefab(herbSeedlingPrefab, "Herb seedling");

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                SelectPrefab(flowerSeedlingPrefab, "Flower seedling");
        }

        if (WasPressed(selectPlotAction))
            SelectPrefab(gardenPlotPrefab, "Garden plot");

        if (WasPressed(selectSeedlingAction))
            SelectPrefab(seedlingPrefab, "Tomato seedling");

        if (WasPressed(selectHerbAction))
            SelectPrefab(herbSeedlingPrefab, "Herb seedling");

        if (WasPressed(selectFlowerAction))
            SelectPrefab(flowerSeedlingPrefab, "Flower seedling");

        if (WasPressed(placeAction))
            TryPlaceObject();

        if (WasPressed(waterAction))
            TryWaterPlant();

        if (WasPressed(harvestAction))
            TryHarvestPlant();
    }

    bool WasPressed(InputActionReference reference)
    {
        return reference != null &&
               reference.action != null &&
               reference.action.WasPressedThisFrame();
    }

    void SelectPrefab(GameObject prefab, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning(label + " prefab is not assigned.");
            return;
        }

        selectedPrefab = prefab;
        Debug.Log("Selected: " + label);
    }

    bool TryGetHit(out RaycastHit hit)
    {
        hit = default;

        if (rayInteractor == null)
            return false;

        return rayInteractor.TryGetCurrent3DRaycastHit(out hit);
    }

    void TryPlaceObject()
    {
        if (selectedPrefab == null)
        {
            Debug.Log("Select a plot or plant first.");
            return;
        }

        if (!TryGetHit(out RaycastHit hit))
            return;

        // Placement must target the garden ground.
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

        bool placingPlot = selectedPrefab == gardenPlotPrefab;

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

        // All three plant types require a plot.
        if (!HasPlotAt(cellCenter))
        {
            Debug.Log("Place a garden plot before planting.");
            return;
        }

        if (HasPlantAt(cellCenter))
        {
            Debug.Log("This plot already has a plant.");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("GardenInventory is missing.");
            return;
        }

        if (!inventory.UseSeeds(1))
            return;

        Vector3 plantPosition = cellCenter;
        plantPosition.y = seedlingHeight;

        Instantiate(
            selectedPrefab,
            plantPosition,
            Quaternion.identity
        );

        Debug.Log("Plant placed in the garden.");
    }

    bool HasPlotAt(Vector3 cellCenter)
    {
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        foreach (Scr_Plot plot in plots)
        {
            if (plot == null)
                continue;

            if (SameCell(plot.transform.position, cellCenter))
                return true;
        }

        return false;
    }

    bool HasPlantAt(Vector3 cellCenter)
    {
        // Tomato plants.
        Scr_Plant[] tomatoes =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        foreach (Scr_Plant plant in tomatoes)
        {
            if (plant == null || plant.IsDead)
                continue;

            if (SameCell(plant.transform.position, cellCenter))
                return true;
        }

        // Herb plants.
        Scr_CornPlant[] herbs =
            FindObjectsByType<Scr_CornPlant>(FindObjectsSortMode.None);

        foreach (Scr_CornPlant plant in herbs)
        {
            if (plant != null &&
                SameCell(plant.transform.position, cellCenter))
                return true;
        }

        // Flower plants.
        Scr_Flower[] flowers =
            FindObjectsByType<Scr_Flower>(FindObjectsSortMode.None);

        foreach (Scr_Flower plant in flowers)
        {
            if (plant != null &&
                SameCell(plant.transform.position, cellCenter))
                return true;
        }

        return false;
    }

    bool SameCell(Vector3 position, Vector3 cellCenter)
    {
        int x1 = Mathf.FloorToInt(position.x / cellSize);
        int z1 = Mathf.FloorToInt(position.z / cellSize);

        int x2 = Mathf.FloorToInt(cellCenter.x / cellSize);
        int z2 = Mathf.FloorToInt(cellCenter.z / cellSize);

        return x1 == x2 && z1 == z2;
    }

    
    void TryWaterPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        Scr_CornPlant herb =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        if (tomato == null && herb == null && flower == null)
        {
            Debug.Log("Point at a plant to water it.");
            return;
        }

        if (tomato != null && tomato.IsDead)
        {
            Debug.Log("This tomato plant is dead.");
            return;
        }

        if (herb != null && herb.IsDead)
        {
            Debug.Log("This herb plant is dead.");
            return;
        }

        if (flower != null && flower.IsDead)
        {
            Debug.Log("This flower plant is dead.");
            return;
        }

        if (inventory == null || !inventory.UseWater(1))
            return;

        if (tomato != null)
            tomato.Water();
        else if (herb != null)
            herb.Water();
        else
            flower.Water();
    }

    void TryHarvestPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        // Tomato plant.
        Scr_Plant tomato =
            hit.collider.GetComponentInParent<Scr_Plant>();

        if (tomato != null)
        {
            tomato.Harvest();
            return;
        }

        // Herb plant.
        Scr_CornPlant herb =
            hit.collider.GetComponentInParent<Scr_CornPlant>();

        if (herb != null)
        {
            herb.Harvest();
            return;
        }

        // Flower plant.
        Scr_Flower flower =
            hit.collider.GetComponentInParent<Scr_Flower>();

        if (flower != null)
        {
            flower.Harvest();
            return;
        }

        Debug.Log("Point at a plant to harvest it.");
    }
}