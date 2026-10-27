using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InstrumentSelector : MonoBehaviour
{
    public XRRayInteractor ray;
    public InputActionReference menuButton;
    public InputActionReference moveAction;
    public InputActionReference sellAction;
    public InputActionReference closeAction;
    public MoveInstrument mover;
    Instrument targetInstrument;

    void OnEnable()
    {
        sellAction.action.performed += SellInstrument;
        closeAction.action.performed += CloseMenu;
        moveAction.action.performed += Move;
        menuButton.action.performed += MenuAction;
        menuButton.action.Enable();
    }

    void OnDisable()
    {
        sellAction.action.performed -= SellInstrument;
        closeAction.action.performed -= CloseMenu;
        moveAction.action.performed -= Move;
        menuButton.action.performed -= MenuAction;
    }

    void Update()
    {
        if (targetInstrument == null) return;
            GameState.Instance.menu.LoadMenu($"Happiness: {targetInstrument.GetHappiness():F2}\nDepression: {targetInstrument.GetDepression():F2}\nPopularity: {targetInstrument.GetPopularity():F2}\nLoudness: {targetInstrument.GetLoudness():F2}\nSynergy: {targetInstrument.GetSynergy()}\nDurability: {targetInstrument.GetDurability()}\nProfit: {targetInstrument.CalculatePay()}\n\nPress B to close this menu.\nPress X to repair this instrument (cost 1 material)\nPress A to move this instrument.\nPress Y to sell this instrument for half its purchase price.");
    }

    private void CloseMenu(InputAction.CallbackContext ctx)
    {
        GameState.Instance.menu.CloseMenu();

        moveAction.action.Disable();
        sellAction.action.Disable();
        targetInstrument = null;
    }

    private void SellInstrument(InputAction.CallbackContext ctx)
    {
        targetInstrument.DestroyInstrument();
        GameState.Instance.AddPay(GameState.Instance.GetInstrumentCost(targetInstrument) / 2);
        CloseMenu(ctx);
    }

    private void Move(InputAction.CallbackContext ctx)
    {
        if (targetInstrument == null) return;
            mover.Move(targetInstrument.gameObject);
            GameState.Instance.DestroyInstrument(targetInstrument);
            CloseMenu(ctx);
    }

    private void MenuAction(InputAction.CallbackContext ctx)
    {
        if (targetInstrument != null) targetInstrument.Repair();
            else {
                ray.TryGetCurrent3DRaycastHit(out RaycastHit hit);
                UnityEngine.Vector2 grid = MoveInstrument.GetNearestGridPoint(hit.point);
                targetInstrument = GameState.Instance.GetInstrumentAt((int) grid.x, (int) grid.y);
                if (targetInstrument != null)
                {
                    sellAction.action.Enable();
                    moveAction.action.Enable();
                }
            }
    }

}
