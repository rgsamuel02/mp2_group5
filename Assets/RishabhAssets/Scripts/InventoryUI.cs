using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mirrors <see cref="PlayerInventory"/> into the inventory list on the wall screen.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public PlayerInventory inventory;
    public ItemRow rowPrefab;
    public Transform content;

    readonly List<ItemRow> rows = new List<ItemRow>();

    void OnEnable()
    {
        inventory.Changed += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        inventory.Changed -= Rebuild;
    }

    /// <summary>True if <paramref name="row"/> is one of this list's visible rows.</summary>
    public bool Owns(ItemRow row) => row && row.gameObject.activeSelf && rows.Contains(row);

    void Rebuild()
    {
        // Reuse rows rather than destroying them; Destroy is deferred, so stale rows would linger this frame.
        var entries = inventory.Entries;
        while (rows.Count < entries.Count) rows.Add(Instantiate(rowPrefab, content));

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < entries.Count;
            rows[i].gameObject.SetActive(used);
            // No button: InventoryPuller grabs the row itself when it's pointed at and gripped.
            if (used) rows[i].Bind(entries[i].item, $"x{entries[i].count}  ·  grip to pull out", null, null);
        }
    }
}
