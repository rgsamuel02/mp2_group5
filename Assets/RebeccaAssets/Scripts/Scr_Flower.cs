using UnityEngine;

public class Scr_Flower : MonoBehaviour
{
    [Header("Core Plant Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float moisture = 50f;
    [Range(0, 100)] public float growth = 0f;
    public float age = 0f;

    [Header("Flower Stats")]
    [Range(0, 100)] public float bloom = 0f;
    [Min(0)] public int flowerCount = 0;

    [Header("Growth Settings")]
    public float growthPerSecond = 2f;
    public float moistureLossPerSecond = 1f;
    public float healthLossWhenDry = 5f;
    public float waterAddedPerAction = 30f;
    public float matureAt = 100f;
    public float matureVisualAt = 50f;
    public int maximumFlowers = 6;

    [Header("Visual Stages")]
    public GameObject seedlingVisual;
    public GameObject matureFlowerPrefab;

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
            Debug.LogError("FlowerBush needs a seedling visual child.");
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

        // Flowers bloom as the plant grows.
        bloom = Mathf.Clamp(
            growth / matureAt * 100f,
            0f, 100f
        );

        flowerCount = Mathf.RoundToInt(
            Mathf.Lerp(0, maximumFlowers, bloom / 100f)
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

        Debug.Log("Flower watered. Moisture: " + moisture);
    }


    public void Fertilize()
    {
        if (isDead)
        {
            Debug.Log("This flower is dead. It cannot be fertilized.");
            return;
        }

        // Fertilizer encourages growth and improves plant health.
        growth = Mathf.Clamp(growth + 20f, 0f, matureAt);
        health = Mathf.Clamp(health + 10f, 0f, 100f);

        // Keep the flower-specific stats synchronized with growth.
        bloom = Mathf.Clamp(
            growth / Mathf.Max(matureAt, 1f) * 100f,
            0f,
            100f
        );

        flowerCount = Mathf.RoundToInt(
            Mathf.Lerp(0, maximumFlowers, bloom / 100f)
        );

        UpdateVisualStage();

        Debug.Log(
            "Flower fertilized! Growth: " +
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

        if (shouldBeMature && matureFlowerPrefab == null)
        {
            Debug.LogWarning("Assign the Mature Flower Prefab.");
            return;
        }

        showingMatureVisual = shouldBeMature;
        seedlingVisual.SetActive(!shouldBeMature);

        if (shouldBeMature)
        {
            matureVisualInstance = Instantiate(
                matureFlowerPrefab,
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
        if (isDead || !IsMature)
        {
            Debug.Log("The flowers are not ready to harvest.");
            return;
        }

        if (inventory == null)
            inventory = FindFirstObjectByType<Scr_Inventory>();

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        int amount = flowerCount;
        inventory.AddResource(GardenResource.Flowers, amount);

        Debug.Log("Harvested " + amount + " flowers.");

        Scr_Ach achievements =
            FindFirstObjectByType<Scr_Ach>();

        if (achievements != null)
        {
            achievements.RegisterHarvest(
                GardenResource.Flowers,
                flowerCount
            );
        }

        growth = 0f;
        bloom = 0f;
        flowerCount = 0;
        UpdateVisualStage();
    }

    void Die()
    {
        isDead = true;
        Debug.Log("The flower plant died from neglect.");

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