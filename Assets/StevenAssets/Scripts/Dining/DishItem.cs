using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// A finished, plated dish that can be picked up and carried (e.g. into another teammate's scene).
    /// Read <see cref="dish"/> or <see cref="displayName"/> to know which meal it is.
    /// Has no dependency on the kitchen managers, so it works in any scene.
    /// </summary>
    public class DishItem : MonoBehaviour
    {
        public ResourceId dish;
        public string displayName;
        [Tooltip("Menu price in Gold, for teammates who want to buy or sell it.")]
        public int price;
    }
}
