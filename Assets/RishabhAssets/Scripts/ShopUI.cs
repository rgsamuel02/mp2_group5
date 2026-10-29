using System.Collections;
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
    public GameObject errorPanel;
    public float errorDuration = 2f;

    readonly List<(ItemRow row, ShopItem item)> rows = new List<(ItemRow, ShopItem)>();
    Coroutine errorRoutine;

    void Awake()
    {
        if (errorPanel) errorPanel.SetActive(false);
    }

    void Start()
    {
        if (errorPanel) errorPanel.SetActive(false);
        foreach (var item in catalog)
        {
            if (!item) continue;
            var row = Instantiate(rowPrefab, content);
            row.Bind(item, $"{item.price}¥", "Buy", () => Buy(item));
            rows.Add((row, item));
        }
    }

    void Buy(ShopItem item)
    {
        if (wallet.TrySpend(item.price))
        {
            inventory.Add(item);
        }
        else
        {
            ShowError();
        }
    }

    void ShowError()
    {
        if (!errorPanel) return;
        errorPanel.SetActive(true);
        if (errorRoutine != null) StopCoroutine(errorRoutine);
        errorRoutine = StartCoroutine(HideErrorAfter(errorDuration));
    }

    IEnumerator HideErrorAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (errorPanel) errorPanel.SetActive(false);
        errorRoutine = null;
    }
}
