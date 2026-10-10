using UnityEngine;

public class Instrument : MonoBehaviour
{
    float t = 0.0f;
    public float profit;
    public bool[] gridMask = new bool[9];
    public GameState.InstrumentType type;
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
