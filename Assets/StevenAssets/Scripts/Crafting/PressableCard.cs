using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Steven.MP2A
{
    /// <summary>
    /// A kitchen "ticket" or knob that can be poked or grabbed (XRSimpleInteractable select).
    /// XRSimpleInteractable.selectEntered calls Press, and onPressed calls the owner's action
    /// (e.g. CraftingStation.StartRecipeAt(index)); both are persistent UnityEvent listeners set in the Inspector.
    /// </summary>
    public class PressableCard : MonoBehaviour
    {
        public Text title;
        public Text body;
        public Image background;
        [Tooltip("Moved back slightly when pressed (button feel).")]
        public Transform visual;
        public float pressDepth = 0.008f;
        public float cooldown = 0.35f;

        [Tooltip("The card's canvas is switched off beyond this distance from the player (saves draw calls on Quest).")]
        public Canvas canvas;
        public float hideDistance = 7f;

        [Tooltip("Optional tutorial popup shown the first time this card is hovered/pressed.")]
        public string tutorialId;

        public UnityEvent onPressed = new UnityEvent();

        float m_LastPress = -10f;
        float m_CullTimer;
        float m_PressAnim;
        Vector3 m_VisualRest;
        bool m_HasRest;

        void Awake()
        {
            if (visual != null)
            {
                m_VisualRest = visual.localPosition;
                m_HasRest = true;
            }
        }

        /// <summary>Persistent listener target for XRSimpleInteractable.selectEntered.</summary>
        public void Press(SelectEnterEventArgs args) => PressNow();

        /// <summary>Persistent listener target for hover (shows the tutorial on first hover).</summary>
        public void Hovered(HoverEnterEventArgs args)
        {
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, transform.position + Vector3.up * 0.25f);
        }

        public void PressNow()
        {
            if (Time.time - m_LastPress < cooldown)
                return;
            m_LastPress = Time.time;
            m_PressAnim = 1f;
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, transform.position + Vector3.up * 0.25f);
            onPressed?.Invoke();
        }

        public void SetState(string titleText, string bodyText, Color backgroundColor)
        {
            if (title != null && title.text != titleText)
                title.text = titleText;
            if (body != null && body.text != bodyText)
                body.text = bodyText;
            if (background != null && background.color != backgroundColor)
                background.color = backgroundColor;
        }

        void Update()
        {
            m_CullTimer -= Time.deltaTime;
            if (m_CullTimer <= 0f && canvas != null)
            {
                m_CullTimer = 0.5f + Random.value * 0.2f;
                var game = KitchenGame.Instance;
                if (game != null)
                {
                    var near = (game.PlayerHeadPosition - transform.position).sqrMagnitude < hideDistance * hideDistance;
                    if (canvas.enabled != near)
                        canvas.enabled = near;
                }
            }

            if (visual == null || !m_HasRest || m_PressAnim <= 0f)
                return;
            m_PressAnim = Mathf.Max(0f, m_PressAnim - Time.deltaTime * 4f);
            // Local +Z points away from the reader, so pushing in is +Z.
            visual.localPosition = m_VisualRest + Vector3.forward * (pressDepth * Mathf.Sin(m_PressAnim * Mathf.PI));
        }
    }
}
