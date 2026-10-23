using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MoveInstrument : MonoBehaviour
{
    public InputActionReference confirm;
    public InputActionReference cancel;
    private Action<InputAction.CallbackContext> confirmHandler;
    private static bool firstSelect = true, firstPlace = true, secondPurchase = true, thirdPurchase = true;
    private GameObject selected = null;

    public InputActionReference placeButton;
    public XRRayInteractor raycast;
    public Material unplaceableMat;
    private Material selectedMat;
    private Vector3 offset;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placeButton.action.started += (ctx) =>
        {
            Place();
        };
        placeButton.action.Disable();
        cancel.action.performed += (ctx) => {
            confirm.action.performed -= confirmHandler;
            confirm.action.Disable();
        };
        cancel.action.Enable();
    }

    public void Select(GameObject prefab)
    {
        float cost = GameState.Instance.GetInstrumentCost(prefab.GetComponent<Instrument>());
        bool affordable = GameState.Instance.GetMoney() >= cost;
        if (affordable)
        {
            GameState.Instance.menu.LoadMenu($"The cost of buying this instrument is {cost}.\n\nPress A to purchase the instrument. Press B to cancel.");
            confirmHandler = (ctx) =>
            {
                GameState.Instance.AddPay(-cost);
                confirm.action.performed -= confirmHandler;
                confirm.action.Disable();
                GameState.Instance.menu.CloseMenu();
                Purchase(prefab);
            };
            confirm.action.performed += confirmHandler;
            confirm.action.Enable();
        }
        else
        {
            GameState.Instance.menu.LoadMenu($"The cost of buying this instrument is {cost}.\n\nPress B to close this menu.");
        }
    }

    public void Purchase(GameObject prefab)
    {
        if (selected != null) return;
        if (firstSelect)
        {
            GameState.Instance.menu.LoadMenu("You just bought your first violin! At the beginning, the location doesn't matter very much, but location will affect instrument performance later in the game. Press X while pointing at a grid point to see that instruments stats or move it.\n\nPress Right Trigger to place.\n\nPress B to close this menu.");
            firstSelect = false;
        }
        else if (secondPurchase)
        {
            GameState.Instance.menu.LoadMenu("Congratulations on purchasing a second instrument! This will really help balance your ensemble. Putting it closer to another instrument will make sure it gets sad less quickly.\n\nThere are also perks for placing instruments of the same type next to each other. Neighboring brass, woodwind, or strings will increase loudness. Neighboring instruments that are the same will increase popularity.\n\nPress B to close this menu.");
            secondPurchase = false;
        }
        else if (thirdPurchase)
        {
            GameState.Instance.menu.LoadMenu("As you keep buying instruments, you may notice some synergize with each other. This is based on if they neighbor an instrument they would neighbor in a real orchestra. You'll have to experiment to see what instruments synergize.\n\nTry getting 30 customers, you may unlock a way of making money a little faster!\n\nPress B to close this menu.");
            thirdPurchase = false;
        }
        selected = Instantiate(prefab);
        selectedMat = selected.GetComponent<MeshRenderer>().material;
        placeButton.action.Enable();
        offset = prefab.GetComponent<Transform>().Find("offset").localPosition;
    }

    public void Move(GameObject instrument)
    {
        if (selected != null) return;
        selected = instrument;
        selectedMat = selected.GetComponent<MeshRenderer>().material;
        placeButton.action.Enable();
        offset = instrument.GetComponent<Transform>().Find("offset").localPosition;
    }

    void Update()
    {
        if (selected == null) return;
        raycast.TryGetCurrent3DRaycastHit(out RaycastHit hit);
        Vector2 target = GetNearestGridPoint(hit.point);
        if (GameState.Instance.Placeable(selected.GetComponent<Instrument>(), (int) target.x, (int) target.y))
        {
            placeButton.action.Enable();
            selected.GetComponent<MeshRenderer>().material = selectedMat;
        }
        else
        {
            placeButton.action.Disable();
            selected.GetComponent<MeshRenderer>().material = unplaceableMat;
        }
        selected.transform.SetLocalPositionAndRotation(new Vector3((target.x - 14) * GameState.GRID_SIZE - GameState.GRID_SIZE / 2, selected.GetComponent<Transform>().localPosition.y, (-target.y - 1) * GameState.GRID_SIZE + GameState.GRID_SIZE / 2) - offset, selected.GetComponent<Transform>().rotation * Quaternion.identity);
    }
    
    public static Vector2 GetNearestGridPoint(Vector3 p)
    {
        Vector2 ret;
        ret.x = (float) Math.Round((p.x + GameState.GRID_SIZE / 2) / GameState.GRID_SIZE) + 14; 
        ret.y = -(float) Math.Round((p.z - GameState.GRID_SIZE / 2) / GameState.GRID_SIZE + 1);
        return ret;
    }

    bool Place()
    {
        if (firstPlace)
        {
            GameState.Instance.menu.LoadMenu("You now have your first instrument. Instruments have stats like happiness, volume, and popularity.\n\nIf an instrument becomes too unhappy or breaks, it will leave your orchestra.\n\nLouder instruments contribute more to their respective pitch groups, but they also make more money.\n\nPopular instruments will get paid at a faster rate.\n\nPress B to close menu.");
            firstPlace = false;
        }
        Vector3 pos = selected.transform.localPosition + offset;
        int x = (int) Math.Round((GameState.GRID_SIZE / 2 + pos.x) / GameState.GRID_SIZE + 14);
        int y = (int) -Math.Round((pos.z - GameState.GRID_SIZE / 2) / GameState.GRID_SIZE + 1);
        Debug.Log(x);
        Debug.Log(y);
        if (GameState.Instance.Place(selected.GetComponent<Instrument>(), x, y)) {
            selected = null;
            placeButton.action.Disable();
            return true;
        }
        return false;
    }
}
