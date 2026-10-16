using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Steven.MP2A
{
    /// <summary>
    /// Quality of life for tools: if a tool is left lying around (or falls out of the world) it pops back to
    /// its hook on the tool wall after a while.
    /// </summary>
    public class ReturnHome : MonoBehaviour
    {
        public XRGrabInteractable grab;
        public Rigidbody body;
        public Vector3 homePosition;
        public Quaternion homeRotation = Quaternion.identity;
        [Tooltip("Seconds of being left alone away from home before returning.")]
        public float idleSeconds = 25f;
        public float homeRadius = 0.3f;
        public float killY = -3f;

        float m_Idle;

        void Update()
        {
            var held = grab != null && grab.isSelected;
            var away = (transform.position - homePosition).sqrMagnitude > homeRadius * homeRadius;
            if (held || !away)
            {
                m_Idle = 0f;
                return;
            }
            m_Idle += Time.deltaTime;
            if (m_Idle >= idleSeconds || transform.position.y < killY)
                GoHome();
        }

        public void GoHome()
        {
            m_Idle = 0f;
            KitchenGame.Blast(transform.position, new Color(0.8f, 0.8f, 1f), 0.3f);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (body != null)
            {
                body.position = homePosition;
                body.rotation = homeRotation;
            }
            KitchenGame.Blast(homePosition, new Color(0.8f, 0.8f, 1f), 0.3f);
        }
    }
}
