using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Foam blobs / sanitizer mist. The thing they hit reacts through its CollisionReactor (which Destroys the
    /// projectile); if nothing reacts the projectile splats and disappears shortly after.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float lifetime = 1.6f;
        public float splatDelay = 0.03f;
        public float growTo = 1.8f;

        float m_Age;
        bool m_Splatting;
        Vector3 m_StartScale;

        void OnEnable()
        {
            m_Age = 0f;
            m_StartScale = transform.localScale;
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            var t = Mathf.Clamp01(m_Age / lifetime);
            transform.localScale = m_StartScale * Mathf.Lerp(1f, growTo, t);
            if (m_Age >= lifetime)
                Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Let the other side's CollisionReactor run first in this physics step; then splat.
            if (m_Splatting)
                return;
            m_Splatting = true;
            Destroy(gameObject, splatDelay);
        }
    }
}
