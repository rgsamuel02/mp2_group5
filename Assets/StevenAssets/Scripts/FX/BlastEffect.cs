using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// "Solar Blast": a circular (polar-UV) mesh whose texture coordinates scroll outward in the shader,
    /// expanding and fading out. Used for build/harvest/serve/death bursts.
    /// </summary>
    public class BlastEffect : MonoBehaviour
    {
        public Renderer targetRenderer;
        public float lifetime = 0.7f;
        public bool faceCamera = true;

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock m_Block;
        KitchenFX m_Owner;
        float m_Age;
        float m_Size;
        Color m_Color;

        public void Play(KitchenFX owner, Color color, float size)
        {
            m_Owner = owner;
            m_Age = 0f;
            m_Size = Mathf.Max(0.05f, size);
            m_Color = color;
            gameObject.SetActive(true);
            Apply(0f);
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            var t = Mathf.Clamp01(m_Age / Mathf.Max(0.01f, lifetime));
            Apply(t);
            if (t >= 1f)
            {
                if (m_Owner != null)
                    m_Owner.Release(this);
                else
                    Destroy(gameObject);
            }
        }

        void Apply(float t)
        {
            var eased = 1f - (1f - t) * (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, eased) * m_Size;
            if (faceCamera && KitchenGame.Instance != null)
            {
                var dir = transform.position - KitchenGame.Instance.PlayerHeadPosition;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            if (targetRenderer != null)
            {
                if (m_Block == null)
                    m_Block = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(m_Block);
                var c = m_Color;
                c.a *= 1f - t;
                m_Block.SetColor(s_BaseColor, c);
                targetRenderer.SetPropertyBlock(m_Block);
            }
        }
    }
}
