using UnityEngine;

public class MoneyAccumulator : MonoBehaviour
{
    float t = 0.0f;
    public float profit;
    void Update()
    {
        t += Time.deltaTime;
        if (t > GameState.payRate)
        {
            t = 0;
            GameState.Instance.AddPay(GameState.Instance.GetEnjoyment() * profit);
        }
    }
}
