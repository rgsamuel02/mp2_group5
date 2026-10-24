using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Vase : MonoBehaviour
{
    XRSocketInteractor vase;
    public int row;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vase = gameObject.GetComponent<XRSocketInteractor>();
        vase.selectEntered.AddListener(FlowerPlaced);
        vase.selectExited.AddListener(FlowerRemoved);
    }

    private void FlowerRemoved(SelectExitEventArgs arg0)
    {
        GameState.Instance.rowMultiplier[row - 1] -= 1;
    }

    private void FlowerPlaced(SelectEnterEventArgs arg0)
    {
        if (!vase.GetOldestInteractableSelected().transform.gameObject.name.Contains("Rose"))
        {
            GameState.Instance.menu.LoadMenu("No food is allowed in the concert hall! Try some non-food prefabs.\n\nPress B to exit.");
            vase.GetOldestInteractableSelected().transform.position = vase.GetOldestInteractableSelected().transform.position - new Vector3(0, 0.3f, 0);
            vase.interactionManager.SelectExit(vase, vase.GetOldestInteractableSelected());
        }
        GameState.Instance.rowMultiplier[row - 1] += 1;
    }
}
