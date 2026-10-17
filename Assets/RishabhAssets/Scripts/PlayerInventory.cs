using System;
using System.Collections.Generic;
using UnityEngine;

// Items bought but not yet pulled out of the wall. Duplicates stack.
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

    public void Add(ShopItem item)
    {
        if (!item) return;
        var entry = entries.Find(e => e.item == item);
        if (entry == null) entries.Add(new Entry { item = item, count = 1 });
        else entry.count++;
        Changed?.Invoke();
    }

    public bool Remove(ShopItem item)
    {
        var entry = entries.Find(e => e.item == item);
        if (entry == null) return false;
        if (--entry.count <= 0) entries.Remove(entry);
        Changed?.Invoke();
        return true;
    }
}
