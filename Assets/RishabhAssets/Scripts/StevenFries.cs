using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Fries from the restaurant. Released near a table's fries spot it snaps there
// and is eaten by that seat's customer; released anywhere else it falls.
// Cannot be picked up or moved until Goal 1 (Serve two customers) is unlocked.
public class StevenFries : MonoBehaviour
{
    [Min(0.01f)] public float eatSeconds = 0.75f;

    static readonly List<StevenFries> all = new List<StevenFries>();
    public static IReadOnlyList<StevenFries> All => all;

    public static bool IsUnlocked { get; private set; }

    public bool IsBeingEaten { get; private set; }
    public bool IsHeld => grab && grab.isSelected;

    XRGrabInteractable grab;
    Rigidbody body;
    SeatingTable table;
    int anchorIndex = -1;

    public static void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        foreach (var s in Object.FindObjectsByType<StevenFries>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s) s.ApplyLockState();
        }
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        if (grab)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
        ApplyLockState();
    }

    void OnEnable()
    {
        all.Add(this);
        ApplyLockState();
    }

    void OnDisable()
    {
        all.Remove(this);
        if (IsBeingEaten && table)
        {
            table.RemoveFries(this);
            table = null;
        }
    }

    void OnDestroy()
    {
        if (table) table.RemoveFries(this);
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    public void ApplyLockState()
    {
        if (!body) body = GetComponent<Rigidbody>();
        if (!grab) grab = GetComponent<XRGrabInteractable>();
        if (IsBeingEaten) return;

        if (!IsUnlocked)
        {
            if (grab) grab.enabled = false;
            if (body)
            {
                // Allow vertical settling into the delivery crate, but prevent being pushed or knocked around.
                body.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            }
        }
        else
        {
            if (grab) grab.enabled = true;
            if (body)
                body.constraints = RigidbodyConstraints.None;
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (table) table.RemoveFries(this);
        table = null;
        anchorIndex = -1;
        SetDocked(true);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        foreach (var t in SeatingTable.All)
            if (t.TrySnapFries(this)) return;
        SetDocked(false);
    }

    void SetDocked(bool docked)
    {
        if (!body && !TryGetComponent(out body)) return;
        body.isKinematic = docked;
        body.useGravity = !docked;
    }

    void LateUpdate()
    {
        // Ride along if the table is moved while the fries wait on it, and stay kinematic.
        if (table && !IsBeingEaten && !IsHeld)
        {
            SetDocked(true);
            var a = table.friesAnchors[anchorIndex];
            transform.SetPositionAndRotation(a.position, a.rotation);
        }
    }

    public void AttachTo(SeatingTable table, int index)
    {
        this.table = table;
        anchorIndex = index;
        SetDocked(true);
        var a = table.friesAnchors[index];
        transform.SetPositionAndRotation(a.position, a.rotation);
    }

    public void BeginEating(Customer customer)
    {
        if (!IsBeingEaten) StartCoroutine(Eat(customer));
    }

    IEnumerator Eat(Customer customer)
    {
        IsBeingEaten = true;
        if (customer) customer.ConsumeFries();
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

        if (table) table.RemoveFries(this);
        table = null;
        Destroy(gameObject);
    }
}
