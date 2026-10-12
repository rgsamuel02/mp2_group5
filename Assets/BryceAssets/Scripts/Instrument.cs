using UnityEngine;

public class Instrument : MonoBehaviour
{
    public int x, y;
    float t = 0.0f;
    public bool[] gridMask = new bool[9];
    float popularity = 1;
    float loudness = 1;
    float happiness = 300;
    float depression = 1; // I could not think of a better name for the variable that describes how fast happiness is lost
    float synergy = 1;
    public enum InstrumentType
    { 
        VIOLIN,
        VIOLA,
        CELLO,
        BASS,
        FLUTE,
        CLARINET,
        OBOE,
        BASSOON,
        TRUMPET,
        HORN,
        TROMBONE,
        TUBA,
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
            GameState.Instance.AddPay(CalculatePay());
        }
        happiness -= depression * Time.deltaTime;
        if (happiness <= 0)
        {
            ChangeLoudness(-loudness); // remove buffs from everything
            GameState.Instance.DestroyInstrument(this);
            Destroy(gameObject);
        }
    }

    public float CalculatePay()
    {
        return GameState.Instance.GetEnjoyment() * GameState.basePrices[(int) type] * loudness * synergy;
    }

    public void AddPerks()
    {
        float divisor = (high ? 1 : 0) + (low ? 1 : 0) + (mid ? 1 : 0);
        GameState.Instance.AdjustPitch(high ? loudness / divisor : 0, mid ? loudness / divisor : 0, low ? loudness / divisor : 0);
        GameState.Instance.AdjustTimbre(brass ? loudness : 0, woodwind ? loudness : 0, strings ? loudness : 0);
    }

    public void ResetStats()
    {
        loudness = 1;
        popularity = 1;
        depression = 1;
        synergy = 1;
    }

    public void Neighbor(float popularityMod, float loudnessMod, float depressionMod)
    {
        popularity *= popularityMod;
        ChangeLoudness((loudnessMod - 1) * loudness);
        depression /= depressionMod;
    }

    public void ChangeLoudness(float delta)
    {
        float divisor = (high ? 1 : 0) + (low ? 1 : 0) + (mid ? 1 : 0);
        GameState.Instance.AdjustPitch(high ? -loudness / divisor : 0, mid ? -loudness / divisor : 0, low ? -loudness / divisor : 0);
        GameState.Instance.AdjustTimbre(brass ? -loudness : 0, woodwind ? -loudness : 0, strings ? -loudness : 0);
        loudness += delta;
        AddPerks();
    }

    public float GetHappiness()
    {
        return happiness;
    }
    public float GetLoudness()
    {
        return loudness;
    }
    public float GetPopularity()
    {
        return popularity;
    }
    public float GetDepression()
    {
        return depression;
    }

    public void AddSynergy()
    {
        synergy += 1;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if(other.gameObject.name.StartsWith("coin"))
        {
            happiness = 300;
            Destroy(other.gameObject);
        }
    }

    public static bool Synergize(InstrumentType i, InstrumentType j)
    {
        switch (i)
        {
            case InstrumentType.VIOLIN:
                if (j == InstrumentType.VIOLA) return true;
                return false;
            case InstrumentType.VIOLA:
                if (j == InstrumentType.VIOLIN || j == InstrumentType.CELLO) return true;
                return false;
            case InstrumentType.CELLO:
                if (j == InstrumentType.VIOLA || j == InstrumentType.BASS) return true;
                return false;
            case InstrumentType.BASS:
                if (j == InstrumentType.CELLO) return true;
                return false;
            case InstrumentType.FLUTE:
                if (j == InstrumentType.CLARINET) return true;
                return false;
            case InstrumentType.CLARINET:
                if (j == InstrumentType.FLUTE) return true;
                return false;
            case InstrumentType.OBOE:
                if (j == InstrumentType.BASSOON) return true;
                return false;
            case InstrumentType.BASSOON:
                if (j == InstrumentType.OBOE) return true;
                return false;
            case InstrumentType.TRUMPET:
                if (j == InstrumentType.TROMBONE) return true;
                return false;
            case InstrumentType.HORN:
                if (j == InstrumentType.HORN) return true;
                return false;
            case InstrumentType.TROMBONE:
                if (j == InstrumentType.TRUMPET || j == InstrumentType.TUBA) return true;
                return false;
            case InstrumentType.TUBA:
                if (j == InstrumentType.TROMBONE) return true;
                return false;
            default:
                return false;
        }
    }
}
