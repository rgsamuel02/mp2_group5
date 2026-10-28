using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A coffee plant from the garden that can be grabbed and fed into a PlantToBeanPack machine.
public class RebeccaCoffeePlant : MonoBehaviour
{
    Rigidbody body;
    XRGrabInteractable grab;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        if (!grab) return;
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
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
