using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    public InputActionReference close;
    public TextMeshProUGUI text;
    
    void Awake()
    {
        close.action.performed += (ctx) =>
        {
            gameObject.SetActive(false);
            close.action.Disable();
        };
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
        close.action.Disable();
    }
}
