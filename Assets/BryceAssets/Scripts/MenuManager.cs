using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    public InputActionReference close;
    public TextMeshProUGUI text;
    
    void OnEnable()
    {
        close.action.performed += CloseMenu;
    }

    void OnDisable()
    {
        close.action.performed -= CloseMenu;
    }

    public void LoadMenu(string t)
    {
        close.action.Enable();
        gameObject.SetActive(true);
        text.text = t;
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
        // close.action.Disable();
    }

    public void CloseMenu(InputAction.CallbackContext ctx)
    {
        gameObject.SetActive(false);
        // close.action.Disable();
    }

    public bool IsOpen()
    {
        return gameObject.activeInHierarchy;
    }
}
