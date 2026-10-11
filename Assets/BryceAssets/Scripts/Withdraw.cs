using UnityEngine;
using UnityEngine.UI;

public class Withdraw : MonoBehaviour
{
    public Button b;
    public GameObject coin;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        b.onClick.AddListener(ButtonCallback);
    }

    void ButtonCallback()
    {
        if (GameState.Instance.GetMoney() < 1) return;
        Instantiate(coin, new(-7,1,-1), Quaternion.identity);
        GameState.Instance.AddPay(-1);
    }
}
