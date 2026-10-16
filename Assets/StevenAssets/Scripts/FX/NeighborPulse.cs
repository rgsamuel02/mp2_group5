using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>A short-lived glowing arc between two neighboring buildings while they exchange stats.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class NeighborPulse : MonoBehaviour
    {
        public LineRenderer line;
        public int segments = 12;
        public float arcHeight = 0.25f;
        public float lifetime = 0.9f;

        KitchenFX m_Owner;
        float m_Age;
        Color m_Color;

        public void Play(KitchenFX owner, Vector3 from, Vector3 to, Color color)
        {
            m_Owner = owner;
            m_Age = 0f;
            m_Color = color;
            if (line == null)
                line = GetComponent<LineRenderer>();
            line.positionCount = segments + 1;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var p = Vector3.Lerp(from, to, t) + Vector3.up * (4f * arcHeight * t * (1f - t));
                line.SetPosition(i, p);
            }
            gameObject.SetActive(true);
            ApplyColor(1f);
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            var t = Mathf.Clamp01(m_Age / Mathf.Max(0.01f, lifetime));
            ApplyColor(1f - t);
            if (t >= 1f)
            {
                if (m_Owner != null)
                    m_Owner.Release(this);
                else
                    Destroy(gameObject);
            }
        }

        void ApplyColor(float alpha)
        {
            if (line == null)
                return;
            var c = m_Color;
            c.a = alpha;
            line.startColor = c;
            line.endColor = c;
        }
    }
}
