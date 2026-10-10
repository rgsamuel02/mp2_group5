
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Scr_Builder : MonoBehaviour
{
    [Header("VR Controller")]
    public XRRayInteractor rayInteractor;

    public InputActionReference placeAction;
    public InputActionReference selectPlotAction;
    public InputActionReference selectSeedlingAction;
    public InputActionReference waterAction;
    public InputActionReference harvestAction;

    [Header("Garden Prefabs")]
    public GameObject gardenPlotPrefab;
    public GameObject seedlingPrefab;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public int gridSize = 10;

    [Header("Placement Height")]
    public float plotHeight = 0.05f;
    public float seedlingHeight = 0.3f;

    [Header("Ground")]
    public LayerMask groundLayer;

    private GameObject selectedPrefab;
    private Scr_Inventory inventory;

    private bool enabledPlace;
    private bool enabledPlot;
    private bool enabledSeedling;
    private bool enabledWater;
    private bool enabledHarvest;

    void OnEnable()
    {
        EnableAction(placeAction, ref enabledPlace);
        EnableAction(selectPlotAction, ref enabledPlot);
        EnableAction(selectSeedlingAction, ref enabledSeedling);
        EnableAction(waterAction, ref enabledWater);
        EnableAction(harvestAction, ref enabledHarvest);
    }

    void OnDisable()
    {
        DisableAction(placeAction, ref enabledPlace);
        DisableAction(selectPlotAction, ref enabledPlot);
        DisableAction(selectSeedlingAction, ref enabledSeedling);
        DisableAction(waterAction, ref enabledWater);
        DisableAction(harvestAction, ref enabledHarvest);
    }

    void Start()
    {
        selectedPrefab = gardenPlotPrefab;
        inventory = FindFirstObjectByType<Scr_Inventory>();

        if (rayInteractor == null)
        {
            Debug.LogWarning(
                "GardenBuilder: Assign the controller's XR Ray Interactor."
            );
        }

        if (inventory == null)
        {
            Debug.LogError("GardenBuilder: GardenInventory not found.");
        }
    }

    void Update()
    {
        // Desktop testing shortcuts.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                selectedPrefab = gardenPlotPrefab;

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                selectedPrefab = seedlingPrefab;
        }

        // Controller actions for choosing the object to place.
        if (WasPressed(selectPlotAction))
            selectedPrefab = gardenPlotPrefab;

        if (WasPressed(selectSeedlingAction))
            selectedPrefab = seedlingPrefab;

        if (WasPressed(placeAction))
            TryPlaceObject();

        if (WasPressed(waterAction))
            TryWaterPlant();

        if (WasPressed(harvestAction))
            TryHarvestPlant();
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
        if (selectedPrefab == null || inventory == null)
            return;

        if (!TryGetHit(out RaycastHit hit))
        {
            Debug.Log("Aim at the garden first.");
            return;
        }

        // Don't place objects on plants or other props.
        int hitLayer = hit.collider.gameObject.layer;

        if ((groundLayer.value & (1 << hitLayer)) == 0)
        {
            Debug.Log("Aim at the garden ground to place objects.");
            return;
        }

        Vector3 position = hit.point;

        int cellX = Mathf.FloorToInt(position.x / cellSize);
        int cellZ = Mathf.FloorToInt(position.z / cellSize);

        if (cellX < 0 || cellX >= gridSize ||
            cellZ < 0 || cellZ >= gridSize)
        {
            Debug.Log("Aim inside the garden grid.");
            return;
        }

        position.x = (cellX + 0.5f) * cellSize;
        position.z = (cellZ + 0.5f) * cellSize;

        bool placingPlot = selectedPrefab == gardenPlotPrefab;
        bool placingSeedling = selectedPrefab == seedlingPrefab;

        position.y = placingPlot ? plotHeight : seedlingHeight;

        if (placingPlot && HasPlotAt(cellX, cellZ))
        {
            Debug.Log("A garden plot already exists here!");
            return;
        }

        if (placingSeedling)
        {
            if (!HasPlotAt(cellX, cellZ))
            {
                Debug.Log("Planting requires a garden plot!");
                return;
            }

            if (HasPlantAt(cellX, cellZ))
            {
                Debug.Log("There is already a plant in this plot!");
                return;
            }

            if (!inventory.UseSeeds(1))
                return;
        }

        Instantiate(selectedPrefab, position, Quaternion.identity);
        Debug.Log("Garden object placed.");
    }

    void TryWaterPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        Scr_Plant plant = hit.collider.GetComponentInParent<Scr_Plant>();

        if (plant == null)
        {
            Debug.Log("Aim at a plant to water it.");
            return;
        }

        if (inventory == null || !inventory.UseWater(1))
            return;

        plant.Water();
    }

    void TryHarvestPlant()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        Scr_Plant plant = hit.collider.GetComponentInParent<Scr_Plant>();

        if (plant == null)
        {
            Debug.Log("Aim at a plant to harvest it.");
            return;
        }

        plant.Harvest();
    }

    bool HasPlotAt(int targetX, int targetZ)
    {
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        foreach (Scr_Plot plot in plots)
        {
            int x = Mathf.FloorToInt(plot.transform.position.x / cellSize);
            int z = Mathf.FloorToInt(plot.transform.position.z / cellSize);

            if (x == targetX && z == targetZ)
                return true;
        }

        return false;
    }

    bool HasPlantAt(int targetX, int targetZ)
    {
        Scr_Plant[] plants =
            FindObjectsByType<Scr_Plant>(FindObjectsSortMode.None);

        foreach (Scr_Plant plant in plants)
        {
            if (plant.IsDead)
                continue;

            int x = Mathf.FloorToInt(plant.transform.position.x / cellSize);
            int z = Mathf.FloorToInt(plant.transform.position.z / cellSize);

            if (x == targetX && z == targetZ)
                return true;
        }

        return false;
    }

    bool WasPressed(InputActionReference reference)
    {
        return reference != null &&
               reference.action != null &&
               reference.action.enabled &&
               reference.action.WasPressedThisFrame();
    }

    void EnableAction(
        InputActionReference reference,
        ref bool enabledByThisScript)
    {
        enabledByThisScript = false;

        if (reference == null || reference.action == null)
            return;

        if (!reference.action.enabled)
        {
            reference.action.Enable();
            enabledByThisScript = true;
        }
    }

    void DisableAction(
        InputActionReference reference,
        ref bool enabledByThisScript)
    {
        if (enabledByThisScript &&
            reference != null &&
            reference.action != null)
        {
            reference.action.Disable();
        }

        enabledByThisScript = false;
    }
}