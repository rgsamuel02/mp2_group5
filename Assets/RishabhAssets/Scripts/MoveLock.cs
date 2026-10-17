using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Stops a placed object being picked up while something depends on it (a cup in it, a customer on it).
/// Optionally also switches off its solid colliders so the controller ray can reach what's inside it.
/// </summary>
public class MoveLock : MonoBehaviour
{
    [Tooltip("Also disable solid (non-trigger) colliders while locked, so things inside the object can be grabbed.")]
    public bool disableCollidersWhileLocked;

    public bool IsLocked { get; private set; }

    readonly List<Collider> disabled = new List<Collider>();

    public void SetLocked(bool locked)
    {
        if (locked == IsLocked) return;
        IsLocked = locked;

        var grab = GetComponent<XRGrabInteractable>();
        if (locked)
        {
            if (grab) grab.enabled = false;
            if (disableCollidersWhileLocked)
            {
                disabled.Clear();
                foreach (var c in GetComponentsInChildren<Collider>())
                {
                    if (c.isTrigger || !c.enabled) continue;
                    c.enabled = false;
                    disabled.Add(c);
                }
            }
        }
        else
        {
            // Only re-enable what we turned off, so colliders disabled for other reasons stay off.
            foreach (var c in disabled) if (c) c.enabled = true;
            disabled.Clear();
            if (grab) grab.enabled = true;
        }
    }

    /// <summary>Gets (or adds) the lock on <paramref name="go"/>.</summary>
    public static MoveLock For(GameObject go, bool disableColliders)
    {
        if (!go.TryGetComponent<MoveLock>(out var l))
        {
            l = go.AddComponent<MoveLock>();
            l.disableCollidersWhileLocked = disableColliders;
        }
        return l;
    }
}
