
using UnityEngine;

public class Scr_Indicator : MonoBehaviour
{
    [Header("Indicator Object")]
    public GameObject indicatorObject;
    public Renderer indicatorRenderer;

    [Header("Indicator Materials")]
    public Material healthyMaterial;
    public Material thirstyMaterial;
    public Material ripeMaterial;
    public Material deadMaterial;

    [Header("Health Settings")]
    public float lowHealthThreshold = 0f;

    [Range(0f, 100f)]
    public float thirstyThreshold = 25f;

    private Scr_Plant tomato;
    private Scr_CornPlant herb;
    private Scr_Flower flower;

    private void Awake()
    {
        tomato = GetComponentInParent<Scr_Plant>();
        herb = GetComponentInParent<Scr_CornPlant>();
        flower = GetComponentInParent<Scr_Flower>();

        if (indicatorObject == null)
            indicatorObject = gameObject;

        if (indicatorRenderer == null)
            indicatorRenderer = indicatorObject.GetComponent<Renderer>();
    }

    private void Update()
    {
        bool isDead = false;
        bool isMature = false;
        float health = 100f;
        float moisture = 100f;

        if (tomato != null)
        {
            isDead = tomato.IsDead;
            isMature = tomato.IsMature;
            health = tomato.health;
            moisture = tomato.moisture;
        }
        else if (herb != null)
        {
            isDead = herb.IsDead;
            isMature = herb.IsMature;
            health = herb.health;
            moisture = herb.moisture;
        }
        else if (flower != null)
        {
            isDead = flower.IsDead;
            isMature = flower.IsMature;
            health = flower.health;
            moisture = flower.moisture;
        }
        else
        {
            return;
        }

        Material desiredMaterial;

        // Dead takes priority over every other condition.
        if (isDead || health <= 0f)
        {
            desiredMaterial = deadMaterial;
        }
        // Ripe plants should remain identifiable as harvestable.
        else if (isMature)
        {
            desiredMaterial = ripeMaterial;
        }
        // A living plant with low moisture needs watering.
        else if (moisture <= thirstyThreshold)
        {
            desiredMaterial = thirstyMaterial;
        }
        else if (health <= lowHealthThreshold)
        {
            desiredMaterial = deadMaterial;
        }
        else
        {
            desiredMaterial = healthyMaterial;
        }

        SetIndicator(desiredMaterial);
    }

    private void SetIndicator(Material material)
    {
        if (indicatorObject == null || indicatorRenderer == null)
            return;

        if (material == null)
        {
            indicatorObject.SetActive(false);
            return;
        }

        if (!indicatorObject.activeSelf)
            indicatorObject.SetActive(true);

        if (indicatorRenderer.sharedMaterial != material)
            indicatorRenderer.sharedMaterial = material;
    }
}