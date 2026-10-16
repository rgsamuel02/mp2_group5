using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Steven.MP2A
{
    /// <summary>
    /// Fires a pop-up tutorial when the player first encounters this object: by walking close to it
    /// (Proximity), hovering it, or grabbing it (Grab/Hover events are wired in the Inspector).
    /// </summary>
    public class TutorialTrigger : MonoBehaviour
    {
        public string tutorialId;
        public TutorialTriggerMode mode = TutorialTriggerMode.Proximity;
        public float radius = 1.6f;
        public Vector3 offset = new Vector3(0f, 1.2f, 0f);
        public float checkInterval = 0.3f;

        float m_Timer;
        bool m_Done;

        void Update()
        {
            if (m_Done || mode != TutorialTriggerMode.Proximity)
                return;
            m_Timer += Time.deltaTime;
            if (m_Timer < checkInterval)
                return;
            m_Timer = 0f;
            var game = KitchenGame.Instance;
            if (game == null)
                return;
            var tm = game.tutorials;
            if (tm != null && tm.HasShown(tutorialId))
            {
                m_Done = true;
                return;
            }
            var head = game.PlayerHeadPosition;
            var flat = transform.position - head;
            flat.y = 0f;
            if (flat.sqrMagnitude < radius * radius)
                Fire();
        }

        public void Fire()
        {
            if (string.IsNullOrEmpty(tutorialId))
                return;
            KitchenGame.Tutorial(tutorialId, transform.position + offset);
        }

        /// <summary>Persistent listener target for XRBaseInteractable.selectEntered.</summary>
        public void OnGrabbed(SelectEnterEventArgs args)
        {
            if (mode == TutorialTriggerMode.Grab || mode == TutorialTriggerMode.Hover)
                Fire();
        }

        /// <summary>Persistent listener target for XRBaseInteractable.hoverEntered.</summary>
        public void OnHovered(HoverEnterEventArgs args)
        {
            if (mode == TutorialTriggerMode.Hover)
                Fire();
        }
    }
}
