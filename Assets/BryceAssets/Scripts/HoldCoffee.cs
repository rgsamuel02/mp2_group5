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
            GameState.Instance.menu.LoadMenu("Good job! Now try it in the real hall. Customers may fall asleep during their stay, and you must give them coffee before they leave if they do. Coffee will cost 3 dollars though, so don't overspend on it!\n\nPress B to exit menu.");
            CoffeeTutorial.Complete();
        }
        GameObject thing = held.GetOldestInteractableSelected().transform.gameObject;
        thing.GetComponent<XRGrabInteractable>().interactionLayers = InteractionLayerMask.GetMask("Coffee");
        // ilm.
    }
}
