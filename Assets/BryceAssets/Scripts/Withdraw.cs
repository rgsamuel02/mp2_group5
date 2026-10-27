using UnityEngine;
using UnityEngine.UI;

public class Withdraw : MonoBehaviour
{
    public Button b;
    public GameObject coin;
    static bool firstWithdraw = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        b.onClick.AddListener(ButtonCallback);
    }

    void ButtonCallback()
    {
        if (firstWithdraw && !GameState.Instance.menu.IsOpen())
        {
            GameState.Instance.menu.LoadMenu("You can withdraw currency to pay your players. This will reset their happiness. Just drop a coin on the instrument and it'll be right as rain.\n\nPress B to close this menu.");
            firstWithdraw = false;
        }
        if (GameState.Instance.GetMoney() < 1) return;
        Instantiate(coin, new (-7, 1, -1), Quaternion.identity);
        GameState.Instance.AddPay(-1);
    }
}
