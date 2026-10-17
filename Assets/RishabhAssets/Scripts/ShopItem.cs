using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Cafe/Shop Item")]
public class ShopItem : ScriptableObject
{
    public string displayName = "Item";
    public int price = 100;
    public GameObject prefab;
    [Tooltip("Leave empty for a blank placeholder.")]
    public Sprite preview;
    [Tooltip("False for tools like the Broom, which just drop.")]
    public bool gridPlaceable = true;
    [Tooltip("Cells covered: X along the prefab's local X, Y along its local Z.")]
    public Vector2Int footprint = Vector2Int.one;
}
