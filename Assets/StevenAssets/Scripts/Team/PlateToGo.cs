using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// On a kitchen sending crate (next to its TransportTunnel): a plate dropped in is boxed "to go". The plate is
    /// swapped for a clean Dish_ prefab of the same meal (no kitchen logic, so it is safe in any scene), the kitchen is
    /// paid, and the order ticket it came from can be plated again for the guest.
    /// </summary>
    public class PlateToGo : MonoBehaviour, ITransportPrepare
    {
        [Tooltip("Dish_ prefabs (Assets/StevenAssets/Prefabs/Dishes); the one matching the plate's meal is sent.")]
        public List<GameObject> dishPrefabs = new List<GameObject>();
        [Tooltip("Gold the kitchen earns per meal sent.")]
        public float goldPerMeal = 10f;
        public string partner = "Orchestra";

        public GameObject Prepare(GameObject item)
        {
            var plate = item != null ? item.GetComponent<Plate>() : null;
            if (plate == null)
                return item;

            GameObject prefab = null;
            foreach (var p in dishPrefabs)
            {
                var d = p != null ? p.GetComponent<DishItem>() : null;
                if (d != null && d.dish == plate.dish)
                {
                    prefab = p;
                    break;
                }
            }
            if (prefab == null && dishPrefabs.Count > 0)
                prefab = dishPrefabs[0];
            if (prefab == null)
                return item;

            var meal = Instantiate(prefab, plate.transform.position, Quaternion.identity);
            meal.name = prefab.name;
            if (plate.table != null)
                plate.table.PlateLost();   // the guest's ticket can be plated again
            var res = KitchenGame.Res;
            if (res != null && goldPerMeal > 0f)
                res.Add(ResourceId.Gold, goldPerMeal);
            KitchenGame.Float(transform.position + Vector3.up * 0.6f,
                $"To go: {KitchenGame.ResourceName(plate.dish)} for the {partner}  +{goldPerMeal:0} Gold", new Color(0.7f, 0.6f, 1f));
            plate.gameObject.SetActive(false);   // stop its other colliders from reporting in this same physics step
            Destroy(plate.gameObject);
            return meal;
        }
    }
}
