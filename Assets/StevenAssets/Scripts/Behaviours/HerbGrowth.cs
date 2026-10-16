using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Herb plants grow (Euler) only while their Moisture is above zero; moisture evaporates over time.
    /// A planter box underneath (foundation) with Nutrients makes them grow faster and slowly uses them up.
    /// If Moisture hits zero the plant wilts and dies (lethal stat).
    /// </summary>
    public class HerbGrowth : MonoBehaviour
    {
        public StatBlock stats;
        public GridBuilding building;
        public float growthPerSecond = 1.4f;
        public float evaporationPerSecond = 0.5f;
        public float nutrientBoost = 0.6f;
        public float nutrientUsePerSecond = 0.15f;

        void Update()
        {
            if (stats == null)
                return;
            var moist = stats.Get(StatId.Moisture) > 0.01f;
            var rate = moist ? growthPerSecond : 0f;
            var planter = building != null ? building.Foundation : null;
            if (moist && planter != null && planter.stats != null && planter.stats.Has(StatId.Nutrients) && planter.stats.Get(StatId.Nutrients) > 0.01f)
            {
                rate *= 1f + nutrientBoost;
                planter.stats.Add(StatId.Nutrients, -nutrientUsePerSecond * Time.deltaTime);
            }
            stats.SetRate(StatId.Growth, rate);
            stats.SetRate(StatId.Moisture, -evaporationPerSecond);
        }
    }
}
