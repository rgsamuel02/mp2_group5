using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Intermittent hazard: a station that is both very hot and greasy/dirty may burst into a Grease Fire
    /// at a random moment. Keep stations clean, spaced out, or cooled with ice.
    /// </summary>
    public class FireHazard : MonoBehaviour
    {
        public StatBlock stats;
        public GreaseFire firePrefab;
        public Transform firePoint;
        public float checkInterval = 3f;
        public float minHeat = 88f;
        public float maxCleanliness = 35f;
        [Tooltip("Fryers: oil this degraded also counts as greasy.")]
        public float maxOilQuality = 25f;
        [Range(0f, 1f)] public float chancePerCheck = 0.2f;

        GreaseFire m_Current;
        float m_Timer;

        void Update()
        {
            if (stats == null || firePrefab == null || stats.IsDead)
                return;
            m_Timer += Time.deltaTime;
            if (m_Timer < checkInterval)
                return;
            m_Timer = 0f;
            if (m_Current != null)
                return;
            if (stats.Get(StatId.Heat) < minHeat)
                return;
            var greasy = (stats.Has(StatId.Cleanliness) && stats.Get(StatId.Cleanliness) <= maxCleanliness) ||
                         (stats.Has(StatId.OilQuality) && stats.Get(StatId.OilQuality) <= maxOilQuality);
            if (!greasy || Random.value > chancePerCheck)
                return;
            Ignite();
        }

        public GreaseFire Ignite()
        {
            if (m_Current != null || firePrefab == null)
                return m_Current;
            var p = firePoint != null ? firePoint.position : transform.position + Vector3.up;
            m_Current = Instantiate(firePrefab, p, Quaternion.identity);
            m_Current.Attach(stats);
            KitchenGame.Float(p + Vector3.up * 0.4f, "GREASE FIRE!", new Color(1f, 0.35f, 0.1f));
            KitchenGame.Tutorial("grease_fire", p + Vector3.up * 0.5f);
            if (KitchenGame.Res != null)
                KitchenGame.Res.Add(ResourceId.Temper, 10f);
            return m_Current;
        }

        public bool IsBurning => m_Current != null;
    }
}
