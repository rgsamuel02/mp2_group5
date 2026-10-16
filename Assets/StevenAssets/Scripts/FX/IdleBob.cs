using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>Gentle idle bob/spin for signifiers (mini equipment, harvest-ready sparkles).</summary>
    public class IdleBob : MonoBehaviour
    {
        public float amplitude = 0.01f;
        public float frequency = 1.5f;
        public float spinDegreesPerSecond;
        Vector3 m_Base;
        bool m_HasBase;
        float m_Phase;

        void OnEnable()
        {
            m_Base = transform.localPosition;
            m_HasBase = true;
            m_Phase = Random.value * 10f;
        }

        void Update()
        {
            if (!m_HasBase)
                return;
            transform.localPosition = m_Base + Vector3.up * (Mathf.Sin((Time.time + m_Phase) * frequency * Mathf.PI * 2f) * amplitude);
            if (!Mathf.Approximately(spinDegreesPerSecond, 0f))
                transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
