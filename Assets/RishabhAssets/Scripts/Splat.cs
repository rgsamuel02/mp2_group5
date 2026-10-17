using System;
using UnityEngine;

/// <summary>
/// A spill on the floor. Disappears when any part of a <see cref="Broom"/> touches its trigger.
/// </summary>
public class Splat : MonoBehaviour
{
    /// <summary>Cell this splat sits on; set by <see cref="SplatSpawner"/>.</summary>
    public Vector2Int Cell { get; set; }

    public event Action<Splat> Cleaned;
    /// <summary>Raised whenever a broom sweeps up any splat.</summary>
    public static event Action<Splat> AnySwept;

    bool cleaned;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Broom>()) Clean();
    }

    public void Clean()
    {
        // Several broom colliders can touch in one step; only count the sweep once.
        if (cleaned) return;
        cleaned = true;
        Cleaned?.Invoke(this);
        AnySwept?.Invoke(this);
        Destroy(gameObject);
    }
}
