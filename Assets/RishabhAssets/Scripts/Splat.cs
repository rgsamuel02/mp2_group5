using System;
using UnityEngine;

// A spill; swept away when a Broom touches its trigger.
public class Splat : MonoBehaviour
{
    public Vector2Int Cell { get; set; }

    public event Action<Splat> Cleaned;
    public static event Action<Splat> AnySwept;

    bool cleaned;

    void OnTriggerEnter(Collider other)
    {
        // Several broom colliders can touch at once; count the sweep once.
        if (cleaned || !other.GetComponentInParent<Broom>()) return;
        cleaned = true;
        Cleaned?.Invoke(this);
        AnySwept?.Invoke(this);
        Destroy(gameObject);
    }
}
