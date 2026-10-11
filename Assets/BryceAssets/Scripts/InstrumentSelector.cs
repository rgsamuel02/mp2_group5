using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InstrumentSelector : MonoBehaviour
{
    public XRRayInteractor ray;
    public InputActionReference menuButton;

    void Start()
    {
        menuButton.action.performed += (ctx) =>
        {
            ray.TryGetCurrent3DRaycastHit(out RaycastHit hit);
            UnityEngine.Vector2 grid = MoveInstrument.GetNearestGridPoint(hit.point);
            Debug.Log(grid);
            Instrument i = GameState.Instance.GetInstrumentAt((int) grid.x, (int) grid.y);
            if (i == null) return;
            GameState.Instance.menu.LoadMenu($"Happiness: {i.GetHappiness()}\nDepression: {i.GetDepression()}\nPopularity: {i.GetPopularity()}\nLoudness: {i.GetLoudness()}\n\nPress B to close this menu.");
        };
        menuButton.action.Enable();
    }
}
