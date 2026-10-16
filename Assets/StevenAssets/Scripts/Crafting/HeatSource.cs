using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Passive Stat Gain: a lit burner/oven/fryer/grill integrates its Heat stat upward every frame
    /// (Euler integration through StatBlock.passiveRate); turning the knob off makes it cool down.
    /// The flame visuals (polar-UV "solar blast" rings) scale with Heat.
    /// </summary>
    public class HeatSource : MonoBehaviour
    {
        public StatBlock stats;
        public bool lit = true;
        [Tooltip("Heat gained per second while lit.")]
        public float heatUpRate = 6f;
        [Tooltip("Heat lost per second while off.")]
        public float coolDownRate = 4f;
        [Tooltip("Lit sources hover around this heat instead of pinning at max (stops runaway fires when clean).")]
        public float idleHeat = 85f;
        public float overheatRate = 0.4f;

        [Header("Visuals")]
        public Transform[] flames;
        public float flameMinScale = 0.05f;
        public float flameMaxScale = 1f;
        public Transform knob;
        public float knobOnAngle = -90f;

        float m_KnobAngle;

        void Update()
        {
            if (stats == null)
                return;
            var heat = stats.Get(StatId.Heat);
            float rate;
            if (!lit)
                rate = -coolDownRate;
            else if (heat < idleHeat)
                rate = heatUpRate;
            else
                rate = overheatRate; // slow creep above idle: neighbors and dirt push it into fire territory
            stats.SetRate(StatId.Heat, rate);

            var n = stats.GetNormalized(StatId.Heat);
            if (flames != null)
            {
                var s = lit ? Mathf.Lerp(flameMinScale, flameMaxScale, n) : Mathf.Lerp(0f, flameMinScale, n);
                foreach (var f in flames)
                    if (f != null)
                        f.localScale = Vector3.one * s;
            }

            if (knob != null)
            {
                m_KnobAngle = Mathf.MoveTowards(m_KnobAngle, lit ? knobOnAngle : 0f, 360f * Time.deltaTime);
                knob.localRotation = Quaternion.Euler(0f, 0f, m_KnobAngle);
            }
        }

        /// <summary>Persistent listener target for the knob card.</summary>
        public void Toggle()
        {
            lit = !lit;
            KitchenGame.Float(transform.position + Vector3.up * 1.1f, lit ? "Burner ON" : "Burner OFF", lit ? new Color(1f, 0.6f, 0.2f) : new Color(0.6f, 0.8f, 1f));
            KitchenGame.Tutorial("heat", transform.position + Vector3.up * 1.2f);
        }
    }
}
