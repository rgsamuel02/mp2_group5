using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills the shop list with the catalog and moves purchases into the player's inventory.
/// </summary>
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
            var captured = item;
            row.Bind(item, $"{item.price}¥", "Buy", () => Buy(captured));
            rows.Add((row, item));
        }
        wallet.BalanceChanged += _ => RefreshAffordability();
        RefreshAffordability();
    }

    public void Buy(ShopItem item)
    {
        if (!wallet.TrySpend(item.price)) return;
        inventory.Add(item);
    }

    void RefreshAffordability()
    {
        foreach (var (row, item) in rows)
            row.SetInteractable(wallet.CanAfford(item.price));
    }
}
