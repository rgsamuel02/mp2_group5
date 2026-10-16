using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Steven.MP2A
{
    /// <summary>
    /// Fire extinguisher / sanitizer spray bottle: while the trigger is held (XR activate) it launches
    /// projectiles from the nozzle. Can spend a player Resource and/or a stat on the tool itself (Pressure).
    /// </summary>
    public class ProjectileLauncher : MonoBehaviour
    {
        public Rigidbody projectilePrefab;
        public Transform muzzle;
        public float speed = 6f;
        public float spread = 3f;
        public float shotsPerSecond = 10f;

        [Header("Ammo")]
        public StatBlock toolStats;
        public bool usesToolStat;
        public StatId toolStat = StatId.Pressure;
        public float toolStatPerShot = 1f;
        public bool usesResource;
        public ResourceId resource = ResourceId.Sanitizer;
        [Tooltip("One unit of the resource is spent every N shots.")]
        public int shotsPerResource = 8;

        public string tutorialId;

        bool m_Firing;
        float m_Cooldown;
        int m_ShotsSinceResource;

        /// <summary>Persistent listener target: XRGrabInteractable.activated.</summary>
        public void StartFiring(ActivateEventArgs args)
        {
            m_Firing = true;
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, transform.position + Vector3.up * 0.3f);
        }

        /// <summary>Persistent listener target: XRGrabInteractable.deactivated.</summary>
        public void StopFiring(DeactivateEventArgs args) => m_Firing = false;

        /// <summary>Persistent listener target: XRGrabInteractable.selectExited (stop if dropped).</summary>
        public void Dropped(SelectExitEventArgs args) => m_Firing = false;

        void Update()
        {
            if (m_Cooldown > 0f)
                m_Cooldown -= Time.deltaTime;
            if (!m_Firing || m_Cooldown > 0f || projectilePrefab == null)
                return;
            m_Cooldown = 1f / Mathf.Max(0.1f, shotsPerSecond);
            Fire();
        }

        void Fire()
        {
            if (usesToolStat && toolStats != null)
            {
                if (toolStats.Get(toolStat) < toolStatPerShot)
                {
                    KitchenGame.Float(transform.position + Vector3.up * 0.2f, "Empty! Let it recharge", Color.white);
                    m_Firing = false;
                    return;
                }
                toolStats.Add(toolStat, -toolStatPerShot);
            }
            if (usesResource)
            {
                var res = KitchenGame.Res;
                if (m_ShotsSinceResource == 0)
                {
                    if (res == null || !res.TrySpend(resource, 1f))
                    {
                        KitchenGame.Float(transform.position + Vector3.up * 0.2f, "Out of " + KitchenGame.ResourceName(resource) + " - buy more", new Color(1f, 0.5f, 0.4f));
                        m_Firing = false;
                        return;
                    }
                }
                m_ShotsSinceResource = (m_ShotsSinceResource + 1) % Mathf.Max(1, shotsPerResource);
            }

            var origin = muzzle != null ? muzzle : transform;
            var dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * Vector3.forward;
            dir = origin.rotation * dir;
            var p = Instantiate(projectilePrefab, origin.position, Quaternion.LookRotation(dir));
            p.linearVelocity = dir * speed;

            // Never let a blob hit the tool that fired it.
            if (m_ToolColliders == null)
                m_ToolColliders = GetComponentsInChildren<Collider>();
            var projectileColliders = p.GetComponentsInChildren<Collider>();
            foreach (var pc in projectileColliders)
                foreach (var tc in m_ToolColliders)
                    if (pc != null && tc != null)
                        Physics.IgnoreCollision(pc, tc, true);
        }

        Collider[] m_ToolColliders;
    }
}
