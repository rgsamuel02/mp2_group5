using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Maps a Stat onto renderer color (e.g. burner glow by Heat, grime by Cleanliness, frost by Chill).
    /// Uses a MaterialPropertyBlock so shared materials are untouched.
    /// </summary>
    public class StatTint : MonoBehaviour
    {
        public StatBlock stats;
        public StatId stat;
        public Renderer[] renderers;
        public Gradient gradient = new Gradient();
        public string colorProperty = "_BaseColor";
        [Tooltip("Also drive _EmissionColor (material must have emission enabled).")]
        public bool driveEmission;
        public float emissionIntensity = 1.5f;
        public float updateInterval = 0.12f;

        static readonly int s_Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock m_Block;
        int m_ColorId;
        float m_Timer;

        void OnEnable()
        {
            m_ColorId = Shader.PropertyToID(string.IsNullOrEmpty(colorProperty) ? "_BaseColor" : colorProperty);
            m_Timer = updateInterval;
        }

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < updateInterval)
                return;
            m_Timer = 0f;
            if (stats == null || renderers == null)
                return;
            var c = gradient.Evaluate(stats.GetNormalized(stat));
            if (m_Block == null)
                m_Block = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(m_Block);
                m_Block.SetColor(m_ColorId, c);
                if (driveEmission)
                    m_Block.SetColor(s_Emission, c * emissionIntensity);
                r.SetPropertyBlock(m_Block);
            }
        }
    }
}
