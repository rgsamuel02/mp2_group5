using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A chocolate souffle from the restaurant. Released near a table's souffle spot it snaps there
// and is eaten by that seat's customer; released anywhere else it falls.
public class StevenChocolateSouffle : MonoBehaviour
{
    [Min(0.01f)] public float eatSeconds = 0.75f;

    static readonly List<StevenChocolateSouffle> all = new List<StevenChocolateSouffle>();
    public static IReadOnlyList<StevenChocolateSouffle> All => all;

    public bool IsBeingEaten { get; private set; }
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
        if (table) table.RemoveSouffle(this);
    }

    void OnDestroy()
    {
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (table) table.RemoveSouffle(this);
        table = null;
        anchorIndex = -1;
        SetDocked(true);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        foreach (var t in SeatingTable.All)
            if (t.TrySnapSouffle(this)) return;
        SetDocked(false);
    }

    void SetDocked(bool docked)
    {
        if (!TryGetComponent<Rigidbody>(out var body)) return;
        body.isKinematic = docked;
        body.useGravity = !docked;
    }

    void LateUpdate()
    {
        // Ride along if the table is moved while the souffle waits on it, and stay kinematic.
        if (table && !IsBeingEaten && !IsHeld)
        {
            SetDocked(true);
            var a = table.souffleAnchors[anchorIndex];
            transform.SetPositionAndRotation(a.position, a.rotation);
        }
    }

    public void AttachTo(SeatingTable table, int index)
    {
        this.table = table;
        anchorIndex = index;
        SetDocked(true);
        var a = table.souffleAnchors[index];
        transform.SetPositionAndRotation(a.position, a.rotation);
    }

    public void BeginEating(Customer customer)
    {
        if (!IsBeingEaten) StartCoroutine(Eat(customer));
    }

    IEnumerator Eat(Customer customer)
    {
        IsBeingEaten = true;
        if (customer) customer.ConsumeSouffle();
        if (grab) grab.enabled = false;
        SetDocked(true);

        var start = transform.position;
        var startScale = transform.localScale;
        float t = 0f;
        while (t < 1f && customer)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / eatSeconds);
            transform.position = Vector3.Lerp(start, customer.DrinkPoint, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = startScale * Mathf.Lerp(1f, 0.6f, t);
            yield return null;
        }

        if (table) table.RemoveSouffle(this);
        table = null;
        Destroy(gameObject);
    }
}
