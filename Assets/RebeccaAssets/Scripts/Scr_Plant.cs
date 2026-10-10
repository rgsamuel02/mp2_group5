
using UnityEngine;

public class Scr_Plant : MonoBehaviour
{
    [Header("Plant Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float moisture = 50f;
    [Range(0, 100)] public float growth = 0f;

    [Header("Growth Settings")]
    public float growthPerSecond = 2f;
    public float moistureLossPerSecond = 1f;
    public float healthLossWhenDry = 5f;
    public float waterAddedPerAction = 30f;
    public float matureAt = 100f;
    public int tomatoYield = 3;

    private Vector3 startingScale;
    private Scr_Inventory inventory;
    private Renderer[] plantRenderers;
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
        plantRenderers = GetComponentsInChildren<Renderer>();

        UpdateAppearance();
    }

    void Update()
    {
        if (isDead)
            return;

        // Plants grow passively over time.
        if (moisture > 0f)
        {
            growth += growthPerSecond * Time.deltaTime;
            growth = Mathf.Clamp(growth, 0f, matureAt);
        }

        // Soil moisture slowly decreases.
        moisture -= moistureLossPerSecond * Time.deltaTime;
        moisture = Mathf.Clamp(moisture, 0f, 100f);

        // Dry plants lose health.
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

        UpdateAppearance();
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

        // Watering also restores a little health.
        health = Mathf.Clamp(health + 5f, 0f, 100f);

        Debug.Log(
            "Plant watered. Moisture: " +
            Mathf.RoundToInt(moisture)
        );

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
            Debug.Log("This plant is not mature yet!");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("GardenInventory was not found.");
            return;
        }

        inventory.AddTomatoes(tomatoYield);

        // Remove the harvested plant for now.
        Destroy(gameObject);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("A plant has died from neglect!");

        // Turn the plant brown to signal death.
        foreach (Renderer plantRenderer in plantRenderers)
        {
            if (plantRenderer == null)
                continue;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            plantRenderer.GetPropertyBlock(block);

            block.SetColor("_BaseColor", new Color(0.35f, 0.2f, 0.1f));
            block.SetColor("_Color", new Color(0.35f, 0.2f, 0.1f));

            plantRenderer.SetPropertyBlock(block);
        }
    }

    void UpdateAppearance()
    {
        // Scale the plant as it grows, up to twice its original size.
        float growthScale = Mathf.Lerp(0.5f, 2f, growth / matureAt);
        transform.localScale = startingScale * growthScale;
    }
}