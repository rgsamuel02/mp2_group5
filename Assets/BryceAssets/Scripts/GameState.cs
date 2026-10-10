using TMPro;
using UnityEngine;


public class GameState : MonoBehaviour
{
    public const int GRID_WIDTH = 30;
    public const int GRID_HEIGHT = 20;
    // The static reference that other scripts call
    public static GameState Instance { get; private set; }

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetEmptyGrid();
        menu.LoadMenu("Welcome to your new job as an orchestra manager! You must hire instruments to improve the sound, but be careful; you don't want to have too much of one type of sound.\n\nYou can purchase instruments over at stage left. How about you start with a violin to be the concertmaster?\n\nPress B to close this menu.");
        DontDestroyOnLoad(gameObject); // Optional: Keeps it alive across scenes
    }

    public MenuManager menu;

    public enum InstrumentType
    {
        EMPTY, 
        TROMBONE,
        VIOLIN,
        TUBA,
        CLARINET,
        CELLO
    };

    int[] basePrices =
    {
        0,
        10,
        30,
        30,
        10,
        10
    };
    
    int instrumentCount = 0;

    static float enjoyment = 1f;
    float money = 0.0f;

    InstrumentType[,] grid = new InstrumentType[GRID_HEIGHT, GRID_WIDTH];

    public TextMeshProUGUI moneyText;

    public float GetEnjoyment()
    {
        return enjoyment;
    }

    public void AddPay(float income)
    {
        money += income;
    }

    public float GetMoney()
    {
        return money;
    }

    public float GetInstrumentCost(InstrumentType i)
    {
        if (i == InstrumentType.VIOLIN && instrumentCount == 0) return 0;
        return basePrices[(int) i] * (instrumentCount + 1);
    }

    void SetEmptyGrid()
    {
        for (int i = 0; i < GRID_HEIGHT; i++)
        {
            for (int j = 0; j < GRID_WIDTH; j++)
            {
                grid[i, j] = InstrumentType.EMPTY;
            }
        }
    }

    public bool Placeable(Instrument inst, int x, int y)
    {
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (x + j < 0 || x + j >= GRID_WIDTH || y + i < 0 || y + j >= GRID_HEIGHT) {
                    if(!inst.gridMask[(i + 1) * 3 + j + 1])
                        continue;
                    return false;
                }
                if (inst.gridMask[(i + 1) * 3 + j + 1] && grid[y + i, x + j] != InstrumentType.EMPTY) return false;
            }
        }
        return true;
    }

    public bool Place(Instrument inst, int x, int y)
    {
        if (!Placeable(inst, x, y)) return false;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (inst.gridMask[i * 3 + j]) grid[y + i - 1, x + j - 1] = inst.type;
            }
        }
        instrumentCount += 1;
        return true;
    }

    void Update()
    {
        moneyText.SetText($"Money: {money}");
    }
}
