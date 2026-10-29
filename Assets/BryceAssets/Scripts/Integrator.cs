using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Integrator : MonoBehaviour
{
    public InputActionReference c, r, g, ca;
    public static InputActionReference cafe, rest, gard, cancel;
    public static bool cEnabled, rEnabled, gEnabled;
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
        string cThing = cEnabled ? "Press A to send to Cafe\n\n" : "";
        string gThing = gEnabled ? "Press X to send to Garden\n\n": "";
        string rThing = rEnabled ? "Press Y to send to Restaurant\n\n": "";
        if (cEnabled || gEnabled || rEnabled) {
            GameState.Instance.menu.LoadMenu($"{cThing}{gThing}{rThing}Press B to cancel");
            ToSend = prefab;
            cafe.action.performed += SendToCafe;
            rest.action.performed += SendToRestaurant;
            gard.action.performed += SendToGarden;
        }
        else
        {
            GameState.Instance.menu.LoadMenu($"No Integrations Unlocked.\n\nPress B to cancel");
        }
        cancel.action.performed += Cancel;
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

    public static void UnlockGarden()
    {
        gEnabled = true;
    }
    public static void UnlockRestaurant()
    {
        rEnabled = true;
    }
    public static void UnlockCafe()
    {
        cEnabled = true;
    }
}
