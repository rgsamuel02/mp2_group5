using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Cookware (Saute Pan, Stockpot) takes its Heat from the burner it is built on (its Foundation).
    /// Heat relaxes toward the foundation's Heat every frame (Euler integration of dH/dt = k (Hstove - H)).
    /// </summary>
    public class HeatFromFoundation : MonoBehaviour
    {
        public GridBuilding building;
        public StatBlock stats;
        [Tooltip("Relaxation rate (1/s).")]
        public float conduction = 0.6f;
        [Tooltip("Heat lost per second when there is no hot foundation.")]
        public float ambientLoss = 3f;

        void Update()
        {
            if (stats == null || !stats.Has(StatId.Heat))
                return;
            var heat = stats.Get(StatId.Heat);
            var foundation = building != null ? building.Foundation : null;
            float rate;
            if (foundation != null && foundation.stats != null && foundation.stats.Has(StatId.Heat))
                rate = (foundation.stats.Get(StatId.Heat) - heat) * conduction;
            else
                rate = -ambientLoss;
            stats.SetRate(StatId.Heat, rate);
        }
    }
}
