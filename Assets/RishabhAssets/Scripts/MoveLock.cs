using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Blocks grabbing a placed object while something depends on it. Can also switch off its solid colliders
// so the controller ray reaches what's inside.
public class MoveLock : MonoBehaviour
{
    public bool disableCollidersWhileLocked;

    bool locked;
    readonly List<Collider> disabled = new List<Collider>();

    public static MoveLock For(GameObject go, bool disableColliders)
    {
        if (!go.TryGetComponent<MoveLock>(out var l))
        {
            l = go.AddComponent<MoveLock>();
            l.disableCollidersWhileLocked = disableColliders;
        }
        return l;
    }

    public void SetLocked(bool value)
    {
        if (value == locked) return;
        locked = value;

        var grab = GetComponent<XRGrabInteractable>();
        if (locked)
        {
            if (grab) grab.enabled = false;
            if (!disableCollidersWhileLocked) return;
            disabled.Clear();
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger || !c.enabled) continue;
                c.enabled = false;
                disabled.Add(c);
            }
        }
        else
        {
            // Only re-enable what we turned off.
            foreach (var c in disabled) if (c) c.enabled = true;
            disabled.Clear();
            if (grab) grab.enabled = true;
        }
    }
}
