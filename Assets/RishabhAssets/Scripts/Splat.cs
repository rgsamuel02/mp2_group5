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

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Broom>()) Clean();
    }

    public void Clean()
    {
        Cleaned?.Invoke(this);
        Destroy(gameObject);
    }
}
