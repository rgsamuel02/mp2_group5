using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A brewed cup of coffee. Flavoring it with syrup tints it red. Let go near a <see cref="SeatingTable"/>'s cup
/// anchor and it snaps there; the customer in the matching seat drinks it (now, or when they sit down).
/// </summary>
public class CoffeeCup : MonoBehaviour
{
    public Color flavoredTint = new Color(1f, 0.35f, 0.35f, 1f);
    [Tooltip("Seconds the cup takes to slide into the customer when they drink it.")]
    [Min(0.01f)] public float drinkSeconds = 0.75f;

    static readonly List<CoffeeCup> all = new List<CoffeeCup>();
    public static IReadOnlyList<CoffeeCup> All => all;

    public bool IsFlavored { get; private set; }
    public bool IsBeingDrunk { get; private set; }
    /// <summary>The table this cup is sitting on, if it's been snapped to one.</summary>
    public SeatingTable Table { get; private set; }

    XRGrabInteractable grab;
    int anchorIndex = -1;

    public bool IsHeld => grab && grab.isSelected;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (grab)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    void OnEnable() => all.Add(this);

    void OnDisable()
    {
        all.Remove(this);
        if (Table) Table.RemoveCup(this);
    }

    void OnDestroy()
    {
        if (grab)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Picking a cup off a table frees its spot.
        if (Table) Table.RemoveCup(this);
        Table = null;
        anchorIndex = -1;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        foreach (var table in SeatingTable.All)
            if (table.TrySnapCup(this)) return;
    }

    void LateUpdate()
    {
        // Ride along with the table if it gets moved while the cup is waiting on it.
        if (Table && !IsBeingDrunk && !IsHeld)
        {
            var a = Table.cupAnchors[anchorIndex];
            transform.SetPositionAndRotation(a.position, a.rotation);
        }
    }

    /// <summary>Called by <see cref="SeatingTable"/> when the cup snaps onto one of its anchors.</summary>
    public void AttachTo(SeatingTable table, int index)
    {
        Table = table;
        anchorIndex = index;
        var a = table.cupAnchors[index];
        transform.SetPositionAndRotation(a.position, a.rotation);
    }

    /// <summary>Slides the cup into <paramref name="customer"/>, then despawns it and sends them on their way.</summary>
    public void BeginDrinking(Customer customer)
    {
        if (IsBeingDrunk) return;
        StartCoroutine(Drink(customer));
    }

    IEnumerator Drink(Customer customer)
    {
        IsBeingDrunk = true;
        // Can't be picked up while it's being drunk.
        if (grab) grab.enabled = false;

        var start = transform.position;
        var startScale = transform.localScale;
        float t = 0f;
        while (t < 1f && customer)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / drinkSeconds);
            transform.position = Vector3.Lerp(start, customer.DrinkPoint, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = startScale * Mathf.Lerp(1f, 0.6f, t);
            yield return null;
        }

        if (Table) Table.RemoveCup(this);
        Table = null;
        if (customer) customer.Serve(this);
        Destroy(gameObject);
    }

    public void SetFlavored()
    {
        if (IsFlavored) return;
        IsFlavored = true;

        var block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", flavoredTint);
            block.SetColor("_Color", flavoredTint);
            r.SetPropertyBlock(block);
        }
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }
}
