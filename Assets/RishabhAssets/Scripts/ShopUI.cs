using System.Collections.Generic;
using UnityEngine;

// Lists the catalog on the TV; buying moves an item into the inventory.
public class ShopUI : MonoBehaviour
{
    public List<ShopItem> catalog = new List<ShopItem>();
    public ItemRow rowPrefab;
    public Transform content;
    public PlayerWallet wallet;
    public PlayerInventory inventory;

    readonly List<(ItemRow row, ShopItem item)> rows = new List<(ItemRow, ShopItem)>();

    void Start()
    {
        foreach (var item in catalog)
        {
            if (!item) continue;
            var row = Instantiate(rowPrefab, content);
            row.Bind(item, $"{item.price}¥", "Buy", () => Buy(item));
            rows.Add((row, item));
        }
        wallet.BalanceChanged += RefreshAffordability;
        RefreshAffordability();
    }

    void Buy(ShopItem item)
    {
        if (wallet.TrySpend(item.price)) inventory.Add(item);
    }

    void RefreshAffordability()
    {
        foreach (var (row, item) in rows)
            row.SetInteractable(wallet.CanAfford(item.price));
    }
}
