using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A coffee plant from the garden that can be grabbed and fed into a PlantToBeanPack machine.
// Cannot be picked up or moved until Goal 2 (Clean up 4 splats and earn 1000 yen) is unlocked.
public class RebeccaCoffeePlant : MonoBehaviour
{
    public static bool IsUnlocked { get; private set; }

    Rigidbody body;
    XRGrabInteractable grab;

    public static void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        foreach (var p in Object.FindObjectsByType<RebeccaCoffeePlant>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p) p.ApplyLockState();
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
        ApplyLockState();
    }

    void OnDestroy()
    {
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    public void ApplyLockState()
    {
        if (!body) body = GetComponent<Rigidbody>();
        if (!grab) grab = GetComponent<XRGrabInteractable>();

        if (!IsUnlocked)
        {
            if (grab) grab.enabled = false;
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }
        else
        {
            if (grab) grab.enabled = true;
            if (body && (!grab || !grab.isSelected))
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (!body) return;
        body.isKinematic = true;
        body.useGravity = false;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (!body) return;
        body.isKinematic = false;
        body.useGravity = true;
    }
}
