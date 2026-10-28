using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeleportationManager : MonoBehaviour
{
    public InputActionReference StickPressed;
    public InputActionReference StickPosition;
    public Image StickRepresentation;
    public TextMeshProUGUI north, east, south, west;
    public InputActionReference confirm;
    string SelectedScene = "";
    static string CurrentScene = "Orchestra";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StickPressed.action.performed += OpenMenu;
        StickPressed.action.Enable();
        StickPosition.action.Enable();
        confirm.action.Enable();
    }

    private void OpenMenu(InputAction.CallbackContext context)
    {
        StickPressed.action.performed -= OpenMenu;
        StickPosition.action.performed += StickMoved;
        StickPressed.action.performed += CloseMenu;
        gameObject.SetActive(true);
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        StickPosition.action.performed -= StickMoved;
        StickPressed.action.performed -= CloseMenu;
        StickPressed.action.performed += OpenMenu;
        gameObject.SetActive(false);
    }

    private void StickMoved(InputAction.CallbackContext context)
    {
        south.color = new Color(0, 0, 0);
        north.color = new Color(0, 0, 0);
        east.color = new Color(0, 0, 0);
        west.color = new Color(0, 0, 0);
        Vector2 stickPos = context.ReadValue<Vector2>() * 200;
        StickRepresentation.transform.localPosition = stickPos;
        if (stickPos.magnitude < 0.5) return; // deadzone prevents accidental inputs
        if (Math.Abs(stickPos.x) < Math.Abs(stickPos.y) && stickPos.y > 0) {
            north.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Cafe";
        }
        if (Math.Abs(stickPos.x) < Math.Abs(stickPos.y) && stickPos.y < 0) {
            south.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Orchestra";
        }
        if (Math.Abs(stickPos.x) >= Math.Abs(stickPos.y) && stickPos.x > 0) {
            east.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Garden";
        }
        if (Math.Abs(stickPos.x) >= Math.Abs(stickPos.y) && stickPos.x < 0) {
            west.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Kitchen";
        }
    }

    private void LoadLevel(InputAction.CallbackContext context)
    {
        CloseMenu(context);
        if (SelectedScene.Equals(CurrentScene)) return;
        StickPressed.action.performed -= OpenMenu;

        if (CurrentScene.Equals("Cafe")) UnloadCafe();
        else if (CurrentScene.Equals("Orchestra")) UnloadOrchestra();
        else if (CurrentScene.Equals("Garden")) UnloadGarden();
        else if (CurrentScene.Equals("Kitchen")) UnloadKitchen();

        if (SelectedScene.Equals("Cafe")) LoadCafe();
        else if (SelectedScene.Equals("Orchestra")) LoadOrchestra();
        else if (SelectedScene.Equals("Garden")) LoadGarden();
        else if (SelectedScene.Equals("Kitchen")) LoadKitchen();
    }

    void UnloadCafe()
    {
        
    }

    void LoadCafe() {
        CurrentScene = "Cafe";
        Debug.Log("Loading Cafe");
        SceneManager.LoadScene("Rishabh Scene");
    }

    void UnloadOrchestra()
    {
        Debug.Log("Leaving Orchestra");
    }

    void LoadOrchestra() {
        int level = GameState.Instance == null ? 1 : GameState.Instance.level;
        CurrentScene = "Orchestra";
        SceneManager.LoadScene($"BryceSceneLvl{level}");
    }

    void UnloadGarden()
    {
        
    }

    void LoadGarden() {
        CurrentScene = "Garden";
        SceneManager.LoadScene("SampleScene");
    }

    void UnloadKitchen()
    {
        
    }

    void LoadKitchen() {
        CurrentScene = "Kitchen";
        SceneManager.LoadScene("StevenScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
