
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Scr_Builder : MonoBehaviour
{
    [Header("VR Controller")]
    public XRRayInteractor rayInteractor;

    [Tooltip("Controller action used to place the selected object.")]
    public InputActionReference placeAction;

    [Tooltip("Optional action for selecting GardenPlot.")]
    public InputActionReference selectPlotAction;

    [Tooltip("Optional action for selecting Seedling.")]
    public InputActionReference selectSeedlingAction;

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

    private bool enabledPlaceAction;
    private bool enabledPlotAction;
    private bool enabledSeedlingAction;

    void OnEnable()
    {
        EnableIfNeeded(placeAction, ref enabledPlaceAction);
        EnableIfNeeded(selectPlotAction, ref enabledPlotAction);
        EnableIfNeeded(selectSeedlingAction, ref enabledSeedlingAction);
    }

    void OnDisable()
    {
        DisableIfEnabled(placeAction, ref enabledPlaceAction);
        DisableIfEnabled(selectPlotAction, ref enabledPlotAction);
        DisableIfEnabled(selectSeedlingAction, ref enabledSeedlingAction);
    }

    void Start()
    {
        selectedPrefab = gardenPlotPrefab;

        if (rayInteractor == null)
        {
            Debug.LogWarning(
                "GardenBuilder: Assign your VR controller's XR Ray Interactor."
            );
        }
    }

    void Update()
    {
        // Keyboard controls are kept for quick desktop testing.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                selectedPrefab = gardenPlotPrefab;

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                selectedPrefab = seedlingPrefab;
        }

        // Optional VR actions for switching the selected object.
        if (WasPressed(selectPlotAction))
            selectedPrefab = gardenPlotPrefab;

        if (WasPressed(selectSeedlingAction))
            selectedPrefab = seedlingPrefab;

        // Place an object when the assigned controller action is pressed.
        if (WasPressed(placeAction))
            TryPlaceObject();
    }

    void TryPlaceObject()
    {
        if (rayInteractor == null || selectedPrefab == null)
            return;

        if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Debug.Log("GardenBuilder: Aim at the ground first.");
            return;
        }

        // Only allow placement on objects in the selected ground layer.
        int hitLayer = hit.collider.gameObject.layer;

        if ((groundLayer.value & (1 << hitLayer)) == 0)
        {
            Debug.Log("GardenBuilder: Aim at the garden ground.");
            return;
        }

        Vector3 position = hit.point;

        // Snap to the center of a grid cell.
        int cellX = Mathf.FloorToInt(position.x / cellSize);
        int cellZ = Mathf.FloorToInt(position.z / cellSize);

        // Keep placement inside the grid.
        if (cellX < 0 || cellX >= gridSize ||
            cellZ < 0 || cellZ >= gridSize)
        {
            Debug.Log("GardenBuilder: Aim inside the garden grid.");
            return;
        }

        position.x = (cellX + 0.5f) * cellSize;
        position.z = (cellZ + 0.5f) * cellSize;

        bool placingPlot = selectedPrefab == gardenPlotPrefab;
        bool placingSeedling = selectedPrefab == seedlingPrefab;

        // Set height explicitly so prefab root transforms don't cause
        // objects to spawn below or above the ground unexpectedly.
        position.y = placingPlot ? plotHeight : seedlingHeight;

        if (placingSeedling && !HasPlotAt(cellX, cellZ))
        {
            Debug.Log("Planting requires a garden plot!");
            return;
        }

        if (placingPlot && HasPlotAt(cellX, cellZ))
        {
            Debug.Log("A garden plot already exists here!");
            return;
        }

        Instantiate(selectedPrefab, position, Quaternion.identity);
        Debug.Log("Garden object placed.");
    }

    bool HasPlotAt(int targetX, int targetZ)
    {
        Scr_Plot[] plots =
            FindObjectsByType<Scr_Plot>(FindObjectsSortMode.None);

        foreach (Scr_Plot plot in plots)
        {
            int plotX = Mathf.FloorToInt(
                plot.transform.position.x / cellSize
            );

            int plotZ = Mathf.FloorToInt(
                plot.transform.position.z / cellSize
            );

            if (plotX == targetX && plotZ == targetZ)
                return true;
        }

        return false;
    }

    bool WasPressed(InputActionReference actionReference)
    {
        return actionReference != null &&
               actionReference.action != null &&
               actionReference.action.enabled &&
               actionReference.action.WasPressedThisFrame();
    }

    void EnableIfNeeded(
        InputActionReference actionReference,
        ref bool enabledByThisScript)
    {
        enabledByThisScript = false;

        if (actionReference == null || actionReference.action == null)
            return;

        if (!actionReference.action.enabled)
        {
            actionReference.action.Enable();
            enabledByThisScript = true;
        }
    }

    void DisableIfEnabled(
        InputActionReference actionReference,
        ref bool enabledByThisScript)
    {
        if (enabledByThisScript &&
            actionReference != null &&
            actionReference.action != null)
        {
            actionReference.action.Disable();
        }

        enabledByThisScript = false;
    }
}