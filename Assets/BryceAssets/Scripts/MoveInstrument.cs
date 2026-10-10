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
    private bool firstSelect = true, firstPlace = true;
    private GameObject selected = null;

    public InputActionReference placeButton;
    public XRRayInteractor raycast;
    public Material unplaceableMat;
    private Material selectedMat;
    private Vector3 offset;

    private float gridSize = 0.5f;

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
        float cost = GameState.Instance.GetInstrumentCost(prefab.GetComponent<Instrument>().type);
        bool affordable = GameState.Instance.GetMoney() >= cost;
        if (affordable)
        {
            GameState.Instance.menu.LoadMenu($"The cost of buying this instrument is {cost}.\n\nPress A to purchase the instrument. Press B to cancel.");
            confirmHandler = (ctx) =>
            {
                GameState.Instance.AddPay(-cost);
                Purchase(prefab);
                confirm.action.performed -= confirmHandler;
                confirm.action.Disable();
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
        if (firstSelect)
        {
            GameState.Instance.menu.LoadMenu("You just bought your first violin! At the beginning, the location doesn't matter very much, but location will affect instrument performance later in the game. You can move objects after their initial placement.\n\nPress Right Trigger to place.\n\nPress B to close this menu.");
            firstSelect = false;
        }
        selected = Instantiate(prefab);
        selectedMat = selected.GetComponent<MeshRenderer>().material;
        placeButton.action.Enable();
        offset = prefab.GetComponent<Transform>().Find("offset").localPosition;
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
        selected.transform.SetPositionAndRotation(new Vector3((target.x - 14) * gridSize, selected.GetComponent<Transform>().position.y, (-target.y - 1) * gridSize) - offset, selected.GetComponent<Transform>().rotation * Quaternion.identity);
    }
    
    Vector2 GetNearestGridPoint(Vector3 p)
    {
        Vector2 ret;
        ret.x = (float) Math.Round(p.x / gridSize) + 14; 
        ret.y = -(float) Math.Round(p.z / gridSize + 1);
        return ret;
    }

    bool Place()
    {
        if (firstPlace)
        {
            GameState.Instance.menu.LoadMenu("You now have your first instrument. Instruments have stats like happiness, volume, and popularity.\n\nIf an instrument becomes too unhappy, it will leave your orchestra.\n\nLouder instruments contribute more to their respective pitch groups, but they also make more money.\n\nPopular instruments will get paid at a faster rate.\n\nPress B to close menu.");
            firstPlace = false;
        }
        Vector3 pos = selected.transform.position + offset;
        int x = (int) Math.Round(pos.x / gridSize + 14);
        int y = (int) -Math.Round(pos.z / gridSize + 1);
        if (GameState.Instance.Place(selected.GetComponent<Instrument>(), x, y)) {
            selected = null;
            placeButton.action.Disable();
            return true;
        }
        return false;
    }
}
