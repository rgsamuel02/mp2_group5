using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>Rising, fading world-space text ("+2 Herbs", "-20 Heat").</summary>
    public class FloatingText : MonoBehaviour
    {
        public Text label;
        public CanvasGroup group;
        public float lifetime = 1.6f;
        public float riseDistance = 0.35f;

        KitchenFX m_Owner;
        float m_Age;
        Vector3 m_Start;
        Vector3 m_BaseScale = Vector3.one;
        bool m_HasBaseScale;

        public void Play(KitchenFX owner, string text, Color color, float scale)
        {
            if (!m_HasBaseScale)
            {
                m_BaseScale = transform.localScale;
                m_HasBaseScale = true;
            }
            m_Owner = owner;
            m_Age = 0f;
            m_Start = transform.position;
            transform.localScale = m_BaseScale * scale;
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }
            if (group != null)
                group.alpha = 1f;
            gameObject.SetActive(true);
            FaceCamera();
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            var t = Mathf.Clamp01(m_Age / Mathf.Max(0.01f, lifetime));
            transform.position = m_Start + Vector3.up * (riseDistance * (1f - (1f - t) * (1f - t)));
            if (group != null)
                group.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            FaceCamera();
            if (t >= 1f)
            {
                if (m_Owner != null)
                    m_Owner.Release(this);
                else
                    Destroy(gameObject);
            }
        }

        void FaceCamera()
        {
            var game = KitchenGame.Instance;
            if (game == null)
                return;
            var dir = transform.position - game.PlayerHeadPosition;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
