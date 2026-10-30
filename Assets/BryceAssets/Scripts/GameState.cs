using System;
using System.Collections.Generic;
using Steven.MP2A;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class GameState : MonoBehaviour
{
    static string template = "Money: {0:F2}\nMaterial: {1}\n\nHigh: {2:F2}\nMedium: {3:F2}\nLow: {4:F2}\n\nBrass: {5}\nWoodwind: {6}\nStrings: {7}\n\n{8}\n{9}\n{10}";
    public const int GRID_WIDTH = 30;
    public const int GRID_HEIGHT = 20;
    public const float GRID_SIZE = 0.5f;
    // The static reference that other scripts call
    public static GameState Instance { get; private set; }

    public GameObject person;
    public GameObject audience;
    public TrashSpawner trashSpawner;
    public List<Material> breakStates;
    bool unlockedGarden = false;
    int numCustomers = 0;
    public int level = 1;
    int flowers = 100, coffee = 100, meals = 100;
    public int[] rowMultiplier = {1, 1, 1, 1, 1};
    bool boughtPiano = false;
    public GameObject pianoTrophy;


    private void Awake()
    {
        if (level == 1 && unlockedGarden) ShowVases.MakeVasesActive();
        // If an instance already exists and it's not this one, destroy it
        if (Instance != null && Instance != this)
        {
            Instance.stats = stats;
            Instance.audience = audience;
            Instance.trashSpawner = trashSpawner;
            Instance.menu = menu;
            Instance.threshold = threshold;
            if (boughtPiano) pianoTrophy.SetActive(true);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        trashSpawner.SpawnTrash(10);
        menu.LoadMenu("Welcome to your new job as an orchestra manager! You must hire instruments to improve the sound, but be careful; you don't want to have too much of one type of sound.\n\nYou can purchase instruments over at stage left. How about you start with a violin to be the concertmaster?\n\nPress B to close this menu.");
        DontDestroyOnLoad(gameObject); // Optional: Keeps it alive across scenes
        DontDestroyOnLoad(pianoTrophy); // Optional: Keeps it alive across scenes
    }

    public MenuManager menu;

    public static int[] basePrices =
    {
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        100
    };

    Person[] occupiedSeats = new Person[50];

    float high = 0, mid = 0, low = 0, brass = 0, woodwind = 0, strings = 0;
    float pitchEnjoyment = 1, timbreEnjoyment = 1;
    int trash = 10;
    float money = 100.0f;
    int material = 0;
    public InputActionReference nextLevel;

    List<Instrument> instruments = new();

    public TextMeshProUGUI stats;
    public TextMeshProUGUI threshold;

    public void AdjustPitch(float h, float m, float l)
    {
        high += h;
        mid += m;
        low += l;
        pitchEnjoyment = GameCalculations.CalculateCloseness(high, mid, low);
    }

    public void AdjustTimbre(float b, float w, float s)
    {
        brass += b;
        woodwind += w;
        strings += s;
        timbreEnjoyment = GameCalculations.CalculateCloseness(brass, woodwind, strings);
    }

    public float GetEnjoyment()
    {
        return pitchEnjoyment * timbreEnjoyment * (Math.Max(20f - trash, 10f) / 20);
    }

    public bool UnlockedGarden()
    {
        return unlockedGarden;
    }

    bool firstCustomer = true;
    public void AddPay(float income)
    {
        money += income;

        if (income <= 0) return; // if we bought or otherwise lost money for some reason don't spawn a customer

        numCustomers++;

        if (numCustomers >= 30 && !unlockedGarden)
        {
            menu.LoadMenu("Great work! You have had 30 customers visit your ensemble, but the place looks pretty drab. I bet you can put up some flowers to bring in a little more money from anyone who sits in the row with flowers.\n\nFor your next unlock, try to fill the hall and plant five flowers. Each flower costs $2. Good luck!\n\nPress B to close this menu.");
            ShowVases.MakeVasesActive();
            unlockedGarden = true;
        }

        if (firstCustomer && !menu.IsOpen())
        {
            menu.LoadMenu("Congratulations! You got your first customer. Each customer will pay an amount based on the instrument they came to see. You can see how much an instrument will sell tickets for by pointing at their grid space and pressing X\n\nPress B to close this menu.");
            firstCustomer = false;
        }

        // verify that there is an empty seat
        if (CalculateFillPercent() == 1)
        {
            if (level == 1) {
                int decorationFlowers = CountFlowers();
                if (decorationFlowers >= 5)
                {
                    menu.CloseMenu();
                    menu.LoadMenu($"You filled the house and planted five flowers to complete the first level! Great job.\n\nYour rank is {ComputeRank()}\n\nPress B to go to the next level.");
                    nextLevel.action.performed += LoadNextLevel;
                }
                return;
            }
            else if (level == 2)
            {
                if (AwakeAudienceMembers() == 50)
                {
                    menu.CloseMenu();
                    menu.LoadMenu($"Wow you have a whole audience who is all awake! Great job.\n\nYour rank is {ComputeRank()}\n\nPress B to go to the next level.");
                    nextLevel.action.performed += LoadNextLevel;
                }
            }
            return;
        }

        GameObject newPerson = Instantiate(person, audience.transform);
        newPerson.transform.localPosition = new Vector3(6.5f, 0, -4.5f);
        int seat = UnityEngine.Random.Range(0, 50);
        while (occupiedSeats[seat] != null)
        {
            seat = UnityEngine.Random.Range(0, 50);
        }
        occupiedSeats[seat] = newPerson.GetComponent<Person>();
        occupiedSeats[seat].SetTarget(seat);
        int row = seat / 10;
        float adjustedIncome = rowMultiplier[row] * income;
        money += (rowMultiplier[row] - 1) * income;
        newPerson.GetComponent<Person>().SetSpawnPrice(adjustedIncome);
    }

    private void LoadNextLevel(InputAction.CallbackContext context)
    {
        level += 1;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLoaded;
            trash = 5 + level * 5;
            trashSpawner.SpawnTrash(5 + level * 5);
        }
        List<Instrument> insts = new List<Instrument>(instruments);
        for (int j = 0; j < insts.Count; j++)
        {
            insts[j].DestroyInstrument();
        }
        occupiedSeats = new Person[50];
        nextLevel.action.performed -= LoadNextLevel;
        SceneManager.sceneLoaded += OnLoaded;
        SceneManager.LoadScene($"BryceSceneLvl{level}");
    }

    string ComputeRank()
    {
        float rank = GetEnjoyment() * money;
        if (rank >= 100 * level) return "S";
        if (rank >= 90 * level) return "A";
        if (rank >= 80 * level) return "B";
        if (rank >= 70 * level) return "C";
        if (rank >= 60 * level) return "D";
        return "F";
    }

    public float GetMoney()
    {
        return money;
    }

    public bool TrySpendMoney(float amount)
    {
        if (amount <= 0 || money < amount)
        {
            Debug.Log("Orchestra does not have enough money.");
            return false;
        }

        money -= amount;
        return true;
    }

    public float GetInstrumentCost(Instrument i)
    {
        if (i.type == Instrument.InstrumentType.VIOLIN && instruments.Count == 0) return 0;
        if (i.type == Instrument.InstrumentType.PIANO) return basePrices[(int) i.type];
        return basePrices[(int) i.type] * (instruments.Count + 1);
    }

    public bool Placeable(Instrument inst, int x, int y)
    {
        List<Vector2> points = new();
        // collect all points of instrument
        for (int i = 0; i < 3; i ++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (inst.gridMask[i * 3 + j]) points.Add(new(x + j - 1, y + i - 1));
            }
        }

        // verify that all points are in bounds
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].x >= GRID_WIDTH || points[i].x < 0 || points[i].y >= GRID_HEIGHT || points[i].y < 0) return false;
        }

        // check for overlap with other instruments
        for (int m = 0; m < instruments.Count; m++)
        {
            if (Math.Abs(instruments[m].x - x) >= 3 || Math.Abs(instruments[m].y - y) >= 3) continue; // instruments are too far apart don't need to worry about overlap
            List<Vector2> otherPoints = new();
            // collect all points of other instrument instrument
            for (int i = 0; i < 3; i ++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (instruments[m].gridMask[i * 3 + j]) otherPoints.Add(new(instruments[m].x + j - 1, instruments[m].y + i - 1));
                }
            }
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < otherPoints.Count; j++)
                {
                    if (points[i].Equals(otherPoints[j])) return false;
                }
            }
        }
        return true;
    }

    public bool Place(Instrument inst, int x, int y)
    {
        if (inst.type == Instrument.InstrumentType.PIANO) 
        {
            boughtPiano = true; 
            pianoTrophy.SetActive(true); 
            menu.LoadMenu("Great job buying a piano! Let this trophy commemorate that you can afford every instrument in the orchestra\n\nPress B to close this menu");
        }
        if (!Placeable(inst, x, y)) return false;
        inst.x = x;
        inst.y = y;
        instruments.Add(inst);
        inst.AddPerks();
        CalculateNeighbors();
        return true;
    }

    public Instrument GetInstrumentAt(int x, int y)
    {
        if (x < 0 || x >= GRID_WIDTH || y < 0 || y >= GRID_HEIGHT) return null;
        for (int i = 0; i < instruments.Count; i++)
        {
            if (Math.Abs(instruments[i].x - x) > 1 || Math.Abs(instruments[i].y - y) > 1) continue;
            for (int j = 0; j < 9; j++)
            {
                if (instruments[i].gridMask[j] && instruments[i].x + (j % 3) - 1 == x && instruments[i].y + (j / 3) - 1 == y) return instruments[i];
            }
        }
        return null;
    }

    public void FreeSeat(int t)
    {
        occupiedSeats[t] = null;
    }

    public void CreateTrash()
    {
        trash += 1;
    }

    public void DestroyTrash()
    {
        material += 1;
        trash -= 1;
    }

    public void DestroyInstrument(Instrument inst)
    {
        instruments.Remove(inst);
        CalculateNeighbors();
    }
    public int GetMaterial()
    {
        return material;
    }
    public void SetMaterial(int m)
    {
        material = m;
    }

    int CountFlowers()
    {
        int decorationFlowers = 0;
        for (int k = 0; k < rowMultiplier.Length; k++) decorationFlowers += rowMultiplier[k] - 1;
        return decorationFlowers;
    }

    float CalculateFillPercent()
    {
        float numerator = 0;
        float divisor = occupiedSeats.Length;
        for (int i = 0; i < occupiedSeats.Length; i++)
        {
            if (occupiedSeats[i]) numerator++;
        }
        return numerator / divisor;
    }

    int AwakeAudienceMembers()
    {
        int awake = 0;
        for (int i = 0; i < occupiedSeats.Length; i++)
        {
            if (occupiedSeats[i] != null && !occupiedSeats[i].IsAsleep()) awake++;
        }
        return awake;
    }

    public void AddFlower()
    {
        flowers += 1;
    }

    public void AddCoffee()
    {
        coffee += 1;
    }

    public void AddMeal()
    {
        meals += 1;
    }

    public bool WithdrawFlower() {
        if (flowers < 1) return false;
        flowers-= 1;
        return true;
    }
    public bool WithdrawCoffee() {
        if (coffee < 1) return false;
        coffee-= 1;
        return true;
    }
    public bool WithdrawMeal() {
        if (meals < 1) return false;
        meals-= 1;
        return true;
    }

    void Update()
    {
        string gardenIntegration = unlockedGarden ? $"Flowers: {flowers}" : "";
        string coffeeIntegration = level >= 2 ? $"Coffee: {coffee}" : "";
        string mealIntegration = level >= 3 ? $"Meals: {KitchenLedger.MealsReady}" : "";
        stats.SetText(string.Format(template, money, material, high, mid, low, brass, woodwind, strings, gardenIntegration, coffeeIntegration, mealIntegration));
        if (!unlockedGarden)
        {
            threshold.SetText($"Customers: {numCustomers}/30");
        }
        else if (level == 1)
        {
            threshold.SetText($"Audience Filled %: {CalculateFillPercent() * 100}\nFlowers Planted: {CountFlowers()}/5");
        }
        else if (level == 2)
        {
            threshold.SetText($"Audience Filled %: {CalculateFillPercent()}\nAwake Audience Members: {AwakeAudienceMembers()} / 50");
        }
    }

    void CalculateNeighbors()
    {
        for (int i = 0; i < instruments.Count; i++)
        {
            instruments[i].ResetStats();
        }
        for (int i = 0; i < instruments.Count; i++)
        {
            for (int j = i + 1; j < instruments.Count; j++)
            {
                // These instruments are classified as neighbors
                if (Math.Abs(instruments[i].x - instruments[j].x) + Math.Abs(instruments[i].y - instruments[j].y) <= 3)
                {
                    // increase popularity by putting same instruments next to each other.
                    if (instruments[i].type == instruments[j].type)
                    {
                        instruments[i].Neighbor(1.5f, 1, 1);
                        instruments[j].Neighbor(1.5f, 1, 1);
                    }
                    // increase volume by putting brass/woodwinds/strings next to each other
                    if (instruments[i].brass && instruments[j].brass || instruments[i].woodwind && instruments[j].woodwind || instruments[i].strings && instruments[j].strings)
                    {
                        instruments[i].Neighbor(1, 1.5f, 1);
                        instruments[j].Neighbor(1, 1.5f, 1);
                    }
                    // instruments are just happy to have neighbors of any kind.
                    instruments[i].Neighbor(1, 1, 1.1f);
                    instruments[j].Neighbor(1, 1, 1.1f);
                    if (Instrument.Synergize(instruments[i].type, instruments[j].type))
                    {
                        instruments[i].AddSynergy();
                        instruments[j].AddSynergy();
                    }
                }
            }
        }
    }

    public void SaveAudience()
    {
        foreach (Person p in occupiedSeats)
        {
            if (p == null) continue;
            p.transform.SetParent(null);
            p.gameObject.SetActive(false);
            DontDestroyOnLoad(p.gameObject);
        }
    }

    public void LoadAudience()
    {
        foreach (Person p in occupiedSeats)
        {
            if (p == null) continue;
            p.transform.SetParent(audience.transform);
            p.gameObject.SetActive(true);
        }
    }
}
