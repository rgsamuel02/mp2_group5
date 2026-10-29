using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Vase : MonoBehaviour
{
    XRSocketInteractor vase;
    IXRSelectInteractable flower;
    public int row;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vase = gameObject.GetComponent<XRSocketInteractor>();
        vase.selectEntered.AddListener(FlowerPlaced);
        vase.selectExited.AddListener(FlowerRemoved);
    }

    public void CleanUp()
    {
        gameObject.SetActive(false);
        if (flower != null)
        {
            flower.transform.gameObject.SetActive(false);
        }   
    }

    public void StartUp()
    {
        gameObject.SetActive(true);
        if (flower != null)
        {
            flower.transform.gameObject.SetActive(true);
            vase.interactionManager.SelectEnter(vase, flower);
        }   
    }

    private void FlowerRemoved(SelectExitEventArgs arg0)
    {
        GameState.Instance.rowMultiplier[row - 1] -= 1;
        if (!gameObject.activeInHierarchy || flower != null && !flower.transform.gameObject.activeInHierarchy) return;
        GameObject obj = arg0.interactableObject.transform.gameObject;
        SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
        flower = null;
    }

    private void FlowerPlaced(SelectEnterEventArgs arg0)
    {
        GameObject obj = arg0.interactableObject.transform.gameObject;
        GameState.Instance.rowMultiplier[row - 1] += 1;
        if (!obj.name.Contains("Rose"))
        {
            GameState.Instance.menu.LoadMenu("No food is allowed in the concert hall! Try some non-food prefabs.\n\nPress B to exit.");
            obj.transform.position -= new Vector3(0, 0.3f, 0);
            vase.interactionManager.SelectExit(vase, arg0.interactableObject);
        }
        flower = arg0.interactableObject;
        DontDestroyOnLoad(obj.transform.gameObject);
        Debug.Log(GameState.Instance.rowMultiplier[row - 1]);
    }
}
