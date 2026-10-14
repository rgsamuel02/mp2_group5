using UnityEngine;

public class Scr_CornPlant : MonoBehaviour
{
    [Header("Core Plant Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float moisture = 50f;
    [Range(0, 100)] public float growth = 0f;
    public float age = 0f;

    [Header("Herb Stats")]
    [Min(0)] public int herbYield = 0;

    [Header("Growth Settings")]
    public float growthPerSecond = 2f;
    public float moistureLossPerSecond = 1f;
    public float healthLossWhenDry = 5f;
    public float waterAddedPerAction = 30f;
    public float matureAt = 100f;
    public float matureVisualAt = 100f;
    public int maximumYield = 5;

    [Header("Visual Stages")]
    public GameObject seedlingVisual;
    public GameObject matureHerbPrefab;

    private GameObject matureVisualInstance;
    private bool showingMatureVisual;
    private bool isDead;

    public bool IsMature => growth >= matureAt && !isDead;
    public bool IsDead => isDead;

    private Scr_Inventory inventory;

    void Start()
    {
        inventory = FindFirstObjectByType<Scr_Inventory>();

        if (seedlingVisual == null && transform.childCount > 0)
            seedlingVisual = transform.GetChild(0).gameObject;

        if (seedlingVisual == null)
        {
            Debug.LogError("HerbPlant needs a seedling visual child.");
            enabled = false;
            return;
        }

        UpdateVisualStage();
    }

    void Update()
    {
        if (isDead)
            return;

        age += Time.deltaTime;

        if (moisture > 0f)
            growth = Mathf.Clamp(
                growth + growthPerSecond * Time.deltaTime,
                0f, matureAt
            );

        moisture = Mathf.Clamp(
            moisture - moistureLossPerSecond * Time.deltaTime,
            0f, 100f
        );

        if (moisture <= 0f)
            health = Mathf.Clamp(
                health - healthLossWhenDry * Time.deltaTime,
                0f, 100f
            );

        herbYield = Mathf.RoundToInt(
            Mathf.Lerp(0, maximumYield, growth / matureAt)
        );

        if (health <= 0f)
        {
            Die();
            return;
        }

        UpdateVisualStage();
    }

    public void Water()
    {
        if (isDead)
            return;

        moisture = Mathf.Clamp(
            moisture + waterAddedPerAction, 0f, 100f
        );

        health = Mathf.Clamp(health + 5f, 0f, 100f);

        Debug.Log("Herb watered. Moisture: " + moisture);
    }

    public void Fertilize()
    {
        if (isDead)
        {
            Debug.Log("This corn plant is dead. It cannot be fertilized.");
            return;
        }

        // Fertilizer encourages growth and improves plant health.
        growth = Mathf.Clamp(growth + 20f, 0f, matureAt);
        health = Mathf.Clamp(health + 10f, 0f, 100f);

        // Update the number of herbs/corn produced as growth increases.
        herbYield = Mathf.RoundToInt(
            Mathf.Lerp(0, maximumYield, growth / matureAt)
        );

        UpdateVisualStage();

        Debug.Log(
            "Corn fertilized! Growth: " +
            Mathf.RoundToInt(growth) +
            ", Health: " +
            Mathf.RoundToInt(health)
        );
    }

    void UpdateVisualStage()
    {
        bool shouldBeMature = growth >= matureVisualAt;

        if (shouldBeMature == showingMatureVisual)
            return;

        if (shouldBeMature && matureHerbPrefab == null)
        {
            Debug.LogWarning("Assign the Mature Herb Prefab.");
            return;
        }

        showingMatureVisual = shouldBeMature;
        seedlingVisual.SetActive(!shouldBeMature);

        if (shouldBeMature)
        {
            matureVisualInstance = Instantiate(
                matureHerbPrefab,
                seedlingVisual.transform.parent
            );

            matureVisualInstance.transform.localPosition =
                seedlingVisual.transform.localPosition;

            matureVisualInstance.transform.localRotation =
                seedlingVisual.transform.localRotation;

            matureVisualInstance.transform.localScale =
                seedlingVisual.transform.localScale;
        }
        else if (matureVisualInstance != null)
        {
            Destroy(matureVisualInstance);
            matureVisualInstance = null;
        }
    }


    public void Harvest()
    {
        // Dead plants can be removed, but never give inventory.
        if (isDead)
        {
            Debug.Log("Dead corn plant removed. No resources collected.");
            Destroy(gameObject);
            return;
        }

        // Living plants must be mature to produce resources.
        if (!IsMature)
        {
            Debug.Log("Corn is not ready to harvest yet.");
            return;
        }

        if (inventory == null)
            inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("Scr_Inventory was not found.");
            return;
        }

        int amount = herbYield;
        inventory.AddResource(GardenResource.Corn, amount);

        Debug.Log("Harvested " + amount + " corn.");

        growth = 0f;
        herbYield = 0;
        UpdateVisualStage();
    }


    void Die()
    {
        isDead = true;
        Debug.Log("The herb plant died from neglect.");

        if (matureVisualInstance != null)
            matureVisualInstance.SetActive(false);

        if (seedlingVisual != null)
            seedlingVisual.SetActive(true);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer plantRenderer in renderers)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            plantRenderer.GetPropertyBlock(block);
            Color deadColor = new Color(0.35f, 0.2f, 0.1f);
            block.SetColor("_BaseColor", deadColor);
            block.SetColor("_Color", deadColor);
            plantRenderer.SetPropertyBlock(block);
        }
    }
}