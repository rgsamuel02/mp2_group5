using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Steven.MP2A
{
    /// <summary>
    /// Signifier: when a hand or ray hovers an interactable, a glowing halo appears under it and it swells a little.
    /// Hover events are wired as persistent listeners in the Inspector. Also fires the object's first-hover tutorial.
    /// </summary>
    public class HoverHighlight : MonoBehaviour
    {
        public GameObject halo;
        public Transform scaleTarget;
        public float hoverScale = 1.06f;
        public string tutorialId;

        Vector3 m_BaseScale;
        bool m_HasBase;
        int m_HoverCount;

        void Awake()
        {
            if (scaleTarget == null)
                scaleTarget = transform;
            m_BaseScale = scaleTarget.localScale;
            m_HasBase = true;
            if (halo != null)
                halo.SetActive(false);
        }

        public void HoverEntered(HoverEnterEventArgs args)
        {
            m_HoverCount++;
            Apply();
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, transform.position + Vector3.up * 0.3f);
        }

        public void HoverExited(HoverExitEventArgs args)
        {
            m_HoverCount = Mathf.Max(0, m_HoverCount - 1);
            Apply();
        }

        void Apply()
        {
            var on = m_HoverCount > 0;
            if (halo != null)
                halo.SetActive(on);
            if (m_HasBase && scaleTarget != null)
                scaleTarget.localScale = on ? m_BaseScale * hoverScale : m_BaseScale;
        }
    }
}
