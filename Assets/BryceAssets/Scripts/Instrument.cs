using UnityEngine;

public class Instrument : MonoBehaviour
{
    static bool hasBroken = false;
    public int x, y;
    float t = 0.0f;
    public bool[] gridMask = new bool[9];
    float popularity = 1;
    float loudness = 1;
    float happiness = 300;
    float depression = 1; // I could not think of a better name for the variable that describes how fast happiness is lost
    float synergy = 1;
    int durability = 3;
    int breakChance = 4096; // 1 in this chance of durability decreasing.
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
        PIANO
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
            DestroyInstrument();
        }
        if (Random.Range(0, breakChance) == 0) {
            if (!hasBroken)
            {
                GameState.Instance.menu.LoadMenu("Uh oh! One of your instruments started to break! You can fix it using your material. To do this, point your controller at a grid space and press X\n\nPress B to close this message.");
                hasBroken = true;
            }
            durability -= 1;
            if (durability < 0) DestroyInstrument();
            var rend = GetComponent<MeshRenderer>();
            var mats = rend.materials;
            mats[1] = GameState.Instance.breakStates[2 - durability];
            rend.materials = mats;
        }
    }

    public void DestroyInstrument()
    {
        ChangeLoudness(-loudness); // remove buffs from everything
        GameState.Instance.DestroyInstrument(this);
        Destroy(gameObject);
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

    public void Repair()
    {
        if (GameState.Instance.GetMaterial() < 1) return;
        GameState.Instance.SetMaterial(GameState.Instance.GetMaterial() - 1);
        var rend = GetComponent<MeshRenderer>();
        var mats = rend.materials;
        mats[1] = mats[0];
        rend.materials = mats;
        durability = 3;
    }

    public void ResetStats()
    {
        ChangeLoudness(1 - loudness);
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
    public float GetSynergy()
    {
        return synergy;
    }
    public float GetDurability()
    {
        return durability;
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
        if (i == InstrumentType.PIANO || j == InstrumentType.PIANO) return true;
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
