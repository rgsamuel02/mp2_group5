using System.Collections.Generic;
using UnityEngine;

// Mirrors PlayerInventory into the inventory list on the TV.
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

    void OnDisable() => inventory.Changed -= Rebuild;

    public bool Owns(ItemRow row) => row && row.gameObject.activeSelf && rows.Contains(row);

    void Rebuild()
    {
        // Rows are reused: Destroy is deferred, so destroyed rows would linger for a frame.
        var entries = inventory.Entries;
        while (rows.Count < entries.Count) rows.Add(Instantiate(rowPrefab, content));

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < entries.Count;
            rows[i].gameObject.SetActive(used);
            if (used) rows[i].Bind(entries[i].item, $"x{entries[i].count}  ·  grip to pull out");
        }
    }
}
