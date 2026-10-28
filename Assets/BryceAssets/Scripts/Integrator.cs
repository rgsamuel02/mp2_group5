using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Integrator : MonoBehaviour
{
    public InputActionReference c, r, g, ca;
    public static InputActionReference cafe, rest, gard, cancel;
    static GameObject ToSend;

    void OnEnable()
    {
        cafe = c;
        rest = r;
        gard = g;
        cancel = ca;
    }

    public static void SendInstrument(GameObject prefab)
    {
        GameState.Instance.menu.LoadMenu("Press A to send to Cafe\n\nPress X to send to Garden\n\nPress Y to send to Restaurant\n\nPress B to cancel");
        ToSend = prefab;
        cancel.action.performed += Cancel;
        cafe.action.performed += SendToCafe;
        rest.action.performed += SendToRestaurant;
        gard.action.performed += SendToGarden;
    }

    private static void Cancel(InputAction.CallbackContext context)
    {
        cancel.action.performed -= Cancel;
        cafe.action.performed -= SendToCafe;
        rest.action.performed -= SendToRestaurant;
        gard.action.performed -= SendToGarden;

        GameState.Instance.menu.CloseMenu();
    }

    private static void SendToGarden(InputAction.CallbackContext context)
    {
        GameState.Instance.menu.CloseMenu();
        GameState.Instance.AddPay(-1 * GameState.Instance.GetInstrumentCost(ToSend.GetComponent<Instrument>()));
        cafe.action.performed -= SendToCafe;
        rest.action.performed -= SendToRestaurant;
        gard.action.performed -= SendToGarden;
        GameObject newInst = Instantiate(ToSend);
        Destroy(newInst.GetComponent<Instrument>());
        
        // TODO whatever you want your system to do.
        throw new NotImplementedException();
    }

    private static void SendToRestaurant(InputAction.CallbackContext context)
    {
        GameState.Instance.menu.CloseMenu();
        GameState.Instance.AddPay(-1 * GameState.Instance.GetInstrumentCost(ToSend.GetComponent<Instrument>()));
        cafe.action.performed -= SendToCafe;
        rest.action.performed -= SendToRestaurant;
        gard.action.performed -= SendToGarden;
        GameObject newInst = Instantiate(ToSend);
        Destroy(newInst.GetComponent<Instrument>());
        
        // TODO whatever you want your system to do.
        throw new NotImplementedException();
    }

    private static void SendToCafe(InputAction.CallbackContext context)
    {
        GameState.Instance.menu.CloseMenu();
        GameState.Instance.AddPay(-1 * GameState.Instance.GetInstrumentCost(ToSend.GetComponent<Instrument>()));
        cafe.action.performed -= SendToCafe;
        rest.action.performed -= SendToRestaurant;
        gard.action.performed -= SendToGarden;
        GameObject newInst = Instantiate(ToSend);
        Destroy(newInst.GetComponent<Instrument>());
        
        // TODO whatever you want your system to do.
        throw new NotImplementedException();
    }
}
