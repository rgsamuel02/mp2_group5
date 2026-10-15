using UnityEngine;

/// <summary>
/// Catalog entry for something the player can buy from the wall shop.
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Cafe/Shop Item")]
public class ShopItem : ScriptableObject
{
    public string displayName = "Item";
    public int price = 100;

    [Tooltip("Prefab spawned when the item is pulled out of the wall.")]
    public GameObject prefab;

    [Tooltip("Preview shown in the shop and inventory lists. Leave empty for a blank placeholder.")]
    public Sprite preview;

    [Tooltip("Whether this item snaps onto the grid when placed (false for tools like the Broom).")]
    public bool gridPlaceable = true;
}
