using UnityEngine;

public class Scr_Plant : MonoBehaviour
{
    [Header("Plant Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float moisture = 50f;
    [Range(0, 100)] public float growth = 0f;
    public float age = 0f;

    [Header("Growth Settings")]
    public float growthPerSecond = 2f;
    public float moistureLossPerSecond = 1f;
    public float healthLossWhenDry = 5f;
    public float waterAddedPerAction = 30f;
    public float matureAt = 100f;
    public int tomatoYield = 3;

    [Header("Growth Stage Prefabs")]
    public GameObject youngTomatoPlantPrefab;
    public GameObject tomatoPlantPrefab;

    [Header("Growth Stage Thresholds")]
    [Range(1, 99)] public float youngStageAt = 34f;
    [Range(2, 100)] public float matureStageAt = 67f;

    private Vector3 startingScale;
    private Scr_Inventory inventory;
    private Renderer[] plantRenderers;

    private Renderer rootRenderer;
    private GameObject currentVisual;
    private int currentStage = 1;
    private bool isDead = false;

    public bool IsMature
    {
        get { return growth >= matureAt && !isDead; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    void Start()
    {
        startingScale = transform.localScale;
        inventory = FindFirstObjectByType<Scr_Inventory>();

        // Stage 1 uses the original Seedling model.
        rootRenderer = GetComponent<Renderer>();

        // If the seedling's visible model is a child instead,
        // use the first child as its initial visual.
        if (rootRenderer == null && transform.childCount > 0)
        {
            currentVisual = transform.GetChild(0).gameObject;
        }

        plantRenderers = GetComponentsInChildren<Renderer>();

        UpdateAppearance();
        UpdateGrowthStage();
    }

    void Update()
    {
        if (isDead)
            return;

        age += Time.deltaTime;
        
        if (moisture > 0f)
        {
            growth += growthPerSecond * Time.deltaTime;
            growth = Mathf.Clamp(growth, 0f, matureAt);
        }

        moisture -= moistureLossPerSecond * Time.deltaTime;
        moisture = Mathf.Clamp(moisture, 0f, 100f);

        if (moisture <= 0f)
        {
            health -= healthLossWhenDry * Time.deltaTime;
            health = Mathf.Clamp(health, 0f, 100f);
        }

        if (health <= 0f)
        {
            Die();
            return;
        }

        UpdateGrowthStage();
        UpdateAppearance();
    }

    void UpdateGrowthStage()
    {
        int desiredStage;

        if (growth < youngStageAt)
        {
            desiredStage = 1;
        }
        else if (growth < matureStageAt)
        {
            desiredStage = 2;
        }
        else
        {
            desiredStage = 3;
        }

        if (desiredStage != currentStage)
        {
            ChangeVisualStage(desiredStage);
        }
    }

    void ChangeVisualStage(int newStage)
    {
        GameObject nextPrefab = null;

        if (newStage == 2)
        {
            nextPrefab = youngTomatoPlantPrefab;
        }
        else if (newStage == 3)
        {
            nextPrefab = tomatoPlantPrefab;
        }

        // If a required prefab is missing, keep the current
        // visual and try again on a later update.
        if (newStage > 1 && nextPrefab == null)
        {
            Debug.LogWarning(
                "GardenPlant: Assign the prefab for growth stage "
                + newStage + " on " + gameObject.name
            );
            return;
        }

        // Remove the previous child visual, if there is one.
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }

        // The original seedling model may be on the root.
        if (rootRenderer != null)
        {
            rootRenderer.enabled = (newStage == 1);
        }

        if (newStage > 1)
        {
            currentVisual = Instantiate(nextPrefab, transform, false);

            // Align the new model to the plant's root.
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localRotation = Quaternion.identity;
        }

        currentStage = newStage;
        plantRenderers = GetComponentsInChildren<Renderer>();

        UpdateAppearance();

        Debug.Log(
            gameObject.name + " changed to growth stage " + currentStage
        );
    }

    public void Water()
    {
        if (isDead)
        {
            Debug.Log("This plant is dead. It cannot be watered.");
            return;
        }

        moisture = Mathf.Clamp(
            moisture + waterAddedPerAction, 0f, 100f
        );

        health = Mathf.Clamp(health + 5f, 0f, 100f);

        Debug.Log("Plant watered. Moisture: " + Mathf.RoundToInt(moisture));

        UpdateAppearance();
    }

    public void Harvest()
    {
        if (isDead)
        {
            Debug.Log("This plant is dead and cannot be harvested.");
            return;
        }

        if (!IsMature)
        {
            Debug.Log("This plant is not ready to harvest yet!");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        inventory.AddTomatoes(tomatoYield);
        Destroy(gameObject);
    }

    void Die()
    {
        isDead = true;

        Debug.Log("A plant has died from neglect!");

        foreach (Renderer plantRenderer in plantRenderers)
        {
            if (plantRenderer == null)
                continue;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            plantRenderer.GetPropertyBlock(block);

            Color deadColor = new Color(0.35f, 0.2f, 0.1f);
            block.SetColor("_BaseColor", deadColor);
            block.SetColor("_Color", deadColor);

            plantRenderer.SetPropertyBlock(block);
        }
    }

    void UpdateAppearance()
    {
        float safeMatureAt = Mathf.Max(matureAt, 1f);
        float growthPercent = Mathf.Clamp01(growth / safeMatureAt);

        float growthScale = Mathf.Lerp(0.5f, 2f, growthPercent);

        transform.localScale = startingScale * growthScale;
    }
}