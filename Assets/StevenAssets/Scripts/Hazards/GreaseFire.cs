using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// A grease fire sitting on a station. Its Intensity grows (Euler) and burns the station's Integrity.
    /// Fire-extinguisher foam that collides with it is Destroyed and lowers Intensity; at 0 Intensity the
    /// fire dies. A roaring fire also heats the station and annoys the Head Chef.
    /// </summary>
    public class GreaseFire : MonoBehaviour
    {
        public StatBlock stats;
        public Transform flameVisual;
        [Tooltip("Integrity damage per second at 100 Intensity.")]
        public float damageAtFull = 6f;
        public float temperPerSecond = 0.6f;

        StatBlock m_Station;

        public void Attach(StatBlock station)
        {
            m_Station = station;
        }

        void Update()
        {
            if (stats == null || stats.IsDead)
                return;
            if (m_Station == null || m_Station.IsDead)
            {
                // Nothing left to burn: the fire starves.
                stats.SetRate(StatId.Intensity, -20f);
                return;
            }
            var n = stats.GetNormalized(StatId.Intensity);
            var dt = Time.deltaTime;
            if (m_Station.Has(StatId.Integrity))
                m_Station.Add(StatId.Integrity, -damageAtFull * n * dt);
            if (m_Station.Has(StatId.Heat))
                m_Station.Add(StatId.Heat, 4f * n * dt);
            if (KitchenGame.Res != null)
                KitchenGame.Res.Add(ResourceId.Temper, temperPerSecond * n * dt);
        }

        /// <summary>Persistent listener target for StatBlock.onDied.</summary>
        public void OnExtinguished()
        {
            if (stats != null && stats.CauseOfDeath == StatId.Intensity && m_Station != null && !m_Station.IsDead)
            {
                KitchenGame.Float(transform.position + Vector3.up * 0.3f, "Fire's out!", new Color(0.6f, 0.9f, 1f));
                if (KitchenGame.Res != null)
                    KitchenGame.Res.Add(ResourceId.Temper, -8f);
            }
            KitchenGame.Blast(transform.position, new Color(0.7f, 0.7f, 0.75f), 0.7f);
        }
    }
}
