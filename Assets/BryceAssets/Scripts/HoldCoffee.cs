using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HoldCoffee : MonoBehaviour
{

    public XRSocketInteractor held;
    void Start()
    {
        held.selectEntered.AddListener(StartSelect);
    }

    private void StartSelect(SelectEnterEventArgs arg0)
    {
        if (CoffeeTutorial.Instance != null)
        {
            CoffeeTutorial.Complete();
        }
        GameObject thing = held.GetOldestInteractableSelected().transform.gameObject;
        thing.GetComponent<XRGrabInteractable>().interactionLayers = InteractionLayerMask.GetMask("Coffee");
        // ilm.
    }
}
