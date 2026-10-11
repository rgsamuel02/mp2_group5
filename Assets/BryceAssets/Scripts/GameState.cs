using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


public class GameState : MonoBehaviour
{
    static string template = "Money: {0:F2}\nMaterial: {1}\n\nHigh: {2:F2}\nMedium: {3:F2}\nLow: {4:F2}\n\nBrass: {5}\nWoodwind: {6}\nStrings: {7}";
    public const int GRID_WIDTH = 30;
    public const int GRID_HEIGHT = 20;
    public const float GRID_SIZE = 0.5f;
    // The static reference that other scripts call
    public static GameState Instance { get; private set; }

    public GameObject person;
    public GameObject audience;
    public TrashSpawner trashSpawner;

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        menu.LoadMenu("Welcome to your new job as an orchestra manager! You must hire instruments to improve the sound, but be careful; you don't want to have too much of one type of sound.\n\nYou can purchase instruments over at stage left. How about you start with a violin to be the concertmaster?\n\nPress B to close this menu.");
        DontDestroyOnLoad(gameObject); // Optional: Keeps it alive across scenes
    }

    public MenuManager menu;

    public static int[] basePrices =
    {
        1,
        3,
        3,
        1,
        1
    };

    bool[] occupiedSeats = new bool[50];

    float high = 0, mid = 0, low = 0, brass = 0, woodwind = 0, strings = 0;
    float pitchEnjoyment = 1, timbreEnjoyment = 1;
    int trash = 10;
    float money = 0.0f;
    int material = 0;

    List<Instrument> instruments = new();

    public TextMeshProUGUI stats;

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

    public void AddPay(float income)
    {
        money += income;

        if (income <= 0) return; // if we bought or otherwise lost money for some reason don't spawn a customer

        // verify that there is an empty seat
        for (int i = 0; i < occupiedSeats.Length; i++)
        {
            if (!occupiedSeats[i]) break;
            if (i == occupiedSeats.Length - 1) return;
        }

        GameObject newPerson = Instantiate(person, audience.transform);
        newPerson.transform.localPosition = new Vector3(6.5f, 0, -4.5f);
        int seat = UnityEngine.Random.Range(0, 50);
        while (occupiedSeats[seat])
        {
            seat = UnityEngine.Random.Range(0, 50);
        }
        newPerson.GetComponent<Person>().SetTarget(seat);
        occupiedSeats[seat] = true;
    }

    public float GetMoney()
    {
        return money;
    }

    public float GetInstrumentCost(Instrument i)
    {
        if (i.type == Instrument.InstrumentType.VIOLIN && instruments.Count == 0) return 0;
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
                Debug.Log(i);
                Debug.Log(j);
                if (instruments[i].gridMask[j] && instruments[i].x + (j % 3) - 1 == x && instruments[i].y + (j / 3) - 1 == y) return instruments[i];
            }
        }
        return null;
    }

    public void FreeSeat(int t)
    {
        occupiedSeats[t] = false;
    }

    public void DestroyTrash()
    {
        trash -= 1;
    }

    public void DestroyInstrument(Instrument inst)
    {
        instruments.Remove(inst);
        CalculateNeighbors();
    }

    void Update()
    {
        stats.SetText(string.Format(template, money, material, high, mid, low, brass, woodwind, strings));
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
                }
            }
        }
    }
}
