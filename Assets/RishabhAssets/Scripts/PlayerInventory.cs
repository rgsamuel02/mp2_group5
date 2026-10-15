using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Items the player owns but has not yet pulled out of the wall. Duplicate purchases stack.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public ShopItem item;
        public int count;
    }

    [SerializeField] List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;
    public event Action Changed;

    public void Add(ShopItem item, int amount = 1)
    {
        if (!item || amount <= 0) return;
        var entry = entries.Find(e => e.item == item);
        if (entry == null) entries.Add(new Entry { item = item, count = amount });
        else entry.count += amount;
        Changed?.Invoke();
    }

    /// <summary>Removes one of <paramref name="item"/>; returns false if none are owned.</summary>
    public bool Remove(ShopItem item)
    {
        var entry = entries.Find(e => e.item == item);
        if (entry == null) return false;
        if (--entry.count <= 0) entries.Remove(entry);
        Changed?.Invoke();
        return true;
    }

    public int Count(ShopItem item)
    {
        var entry = entries.Find(e => e.item == item);
        return entry == null ? 0 : entry.count;
    }
}
