using UnityEngine;

public class Instrument : MonoBehaviour
{
    public int x, y;
    float t = 0.0f;
    public float profit;
    public bool[] gridMask = new bool[9];
    float popularity = 1;
    float loudness = 1;
    float happiness = 300;
    public enum InstrumentType
    { 
        TROMBONE,
        VIOLIN,
        TUBA,
        CLARINET,
        CELLO
    };

    public InstrumentType type;

    public bool high, mid, low;
    public bool brass, woodwind, strings;
    void Update()
    {
        t += Time.deltaTime;
        if (t > 10 / popularity)
        {
            t = 0;
            GameState.Instance.AddPay(GameState.Instance.GetEnjoyment() * GameState.basePrices[(int) type] * loudness / 2);
        }
        happiness -= Time.deltaTime;
    }

    public void AddPerks()
    {
        float divisor = (high ? 1 : 0) + (low ? 1 : 0) + (mid ? 1 : 0);
        GameState.Instance.AdjustPitch(high ? loudness / divisor : 0, mid ? loudness / divisor : 0, low ? loudness / divisor : 0);
        GameState.Instance.AdjustTimbre(brass ? loudness : 0, woodwind ? loudness : 0, strings ? loudness : 0);
    }

    public void ChangeLoudness(float delta)
    {
        float divisor = (high ? 1 : 0) + (low ? 1 : 0) + (mid ? 1 : 0);
        GameState.Instance.AdjustPitch(high ? -loudness / divisor : 0, mid ? -loudness / divisor : 0, low ? -loudness / divisor : 0);
        GameState.Instance.AdjustTimbre(brass ? -loudness : 0, woodwind ? -loudness : 0, strings ? -loudness : 0);
        loudness += delta;
        AddPerks();
    }
}
