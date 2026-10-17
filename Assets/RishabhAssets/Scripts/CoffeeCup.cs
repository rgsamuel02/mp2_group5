using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A brewed cup. Released near a table's cup spot it snaps there and is drunk by that seat's customer;
// released anywhere else it falls. Syrup tints it red.
public class CoffeeCup : MonoBehaviour
{
    public Color flavoredTint = new Color(1f, 0.35f, 0.35f, 1f);
    [Min(0.01f)] public float drinkSeconds = 0.75f;

    static readonly List<CoffeeCup> all = new List<CoffeeCup>();
    public static IReadOnlyList<CoffeeCup> All => all;

    public bool IsFlavored { get; private set; }
    public bool IsBeingDrunk { get; private set; }
    public bool IsHeld => grab && grab.isSelected;

    XRGrabInteractable grab;
    SeatingTable table;
    int anchorIndex = -1;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (!grab) return;
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnEnable() => all.Add(this);

    void OnDisable()
    {
        all.Remove(this);
        if (table) table.RemoveCup(this);
    }

    void OnDestroy()
    {
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (table) table.RemoveCup(this);
        table = null;
        anchorIndex = -1;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        foreach (var t in SeatingTable.All)
            if (t.TrySnapCup(this)) return;
        SetDocked(false);
    }

    // Docked cups (machine slot or table spot) hold still; loose cups fall.
    void SetDocked(bool docked)
    {
        if (!TryGetComponent<Rigidbody>(out var body)) return;
        body.isKinematic = docked;
        body.useGravity = !docked;
    }

    void LateUpdate()
    {
        // Ride along if the table is moved while the cup waits on it.
        if (table && !IsBeingDrunk && !IsHeld)
        {
            var a = table.cupAnchors[anchorIndex];
            transform.SetPositionAndRotation(a.position, a.rotation);
        }
    }

    public void AttachTo(SeatingTable table, int index)
    {
        this.table = table;
        anchorIndex = index;
        SetDocked(true);
        var a = table.cupAnchors[index];
        transform.SetPositionAndRotation(a.position, a.rotation);
    }

    public void BeginDrinking(Customer customer)
    {
        if (!IsBeingDrunk) StartCoroutine(Drink(customer));
    }

    IEnumerator Drink(Customer customer)
    {
        IsBeingDrunk = true;
        if (customer) customer.BeginBeingServed();
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

        if (table) table.RemoveCup(this);
        table = null;
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

    public bool TryGetBounds(out Bounds bounds) => RendererBounds.TryGet(gameObject, out bounds);
}
