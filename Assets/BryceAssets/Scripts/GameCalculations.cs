using System;
using UnityEngine;

public class GameCalculations : MonoBehaviour
{
    public static float CalculateCloseness(float a, float b, float c)
    {
        // Calculates the Coefficient of Variation and converts it to be between 0 and 1. Used for scaling by balance.
        float mean = (a + b + c) / 3;
        if (mean == 0) return 1;
        float std = (float) Math.Sqrt(Math.Pow(a - mean, 2) + Math.Pow(b - mean, 2) + Math.Pow(c - mean, 2) / 3);
        float cv = std / mean;
        return 1 / (1 + cv);
    }
}
