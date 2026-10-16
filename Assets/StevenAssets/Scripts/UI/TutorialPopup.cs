using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>A pop-up tutorial card. Dismiss it by poking "YES, CHEF!" or wait for it to fade.</summary>
    public class TutorialPopup : MonoBehaviour
    {
        public Text title;
        public Text body;
        public Text counter;
        public CanvasGroup group;
        public float fadeTime = 0.4f;

        TutorialManager m_Owner;
        float m_Age;
        float m_Lifetime;
        bool m_Closing;
        float m_CloseT;
        Vector3 m_BaseScale;

        public void Show(TutorialManager owner, string titleText, string bodyText, float lifetime, int number)
        {
            m_Owner = owner;
            m_Lifetime = lifetime;
            m_BaseScale = transform.localScale;
            if (title != null)
                title.text = titleText;
            if (body != null)
                body.text = bodyText;
            if (counter != null)
                counter.text = "TIP #" + number;
            Face(true);
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (!m_Closing && m_Age >= m_Lifetime)
                Dismiss();

            var appear = Mathf.Clamp01(m_Age / 0.25f);
            if (m_Closing)
            {
                m_CloseT += Time.deltaTime / Mathf.Max(0.01f, fadeTime);
                if (group != null)
                    group.alpha = 1f - m_CloseT;
                transform.localScale = m_BaseScale * Mathf.Lerp(1f, 0.8f, m_CloseT);
                if (m_CloseT >= 1f)
                    Destroy(gameObject);
            }
            else
            {
                if (group != null)
                    group.alpha = appear;
                transform.localScale = m_BaseScale * Mathf.Lerp(0.7f, 1f, appear);
            }
            Face(false);
        }

        void Face(bool snap)
        {
            var game = KitchenGame.Instance;
            if (game == null)
                return;
            var dir = transform.position - game.PlayerHeadPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            var want = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = snap ? want : Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }

        /// <summary>Persistent listener target for the "YES, CHEF!" card.</summary>
        public void Dismiss()
        {
            if (m_Closing)
                return;
            m_Closing = true;
            m_CloseT = 0f;
            if (m_Owner != null)
                m_Owner.Closed(this);
        }
    }
}
