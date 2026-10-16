using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Tags an Instance with a kind so collision rules, tools and AI can recognise it.
    /// Keeps a static registry so agents can find targets (e.g. rats looking for food) without scene searches.
    /// </summary>
    [DisallowMultipleComponent]
    public class KitchenEntity : MonoBehaviour
    {
        public EntityKind kind;
        public string displayName;

        [Tooltip("Optional: the StatBlock that collision rules and tools modify. Defaults to the one on this object.")]
        public StatBlock stats;

        static readonly List<KitchenEntity> s_All = new List<KitchenEntity>();

        public static IReadOnlyList<KitchenEntity> All => s_All;

        /// <summary>Set when something is about to destroy this entity, so it is not consumed twice in one frame.</summary>
        public bool Consumed { get; private set; }

        public StatBlock Stats
        {
            get
            {
                if (stats == null)
                    stats = GetComponent<StatBlock>();
                return stats;
            }
        }

        void OnEnable()
        {
            if (!s_All.Contains(this))
                s_All.Add(this);
        }

        void OnDisable()
        {
            s_All.Remove(this);
        }

        /// <summary>Marks the entity as used up and destroys it (collision "one instance is Destroyed").</summary>
        public void Consume(float delay = 0f)
        {
            if (Consumed)
                return;
            Consumed = true;
            s_All.Remove(this);
            // Disable colliders immediately so it cannot trigger anything else this frame.
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;
            Destroy(gameObject, delay);
        }

        public static KitchenEntity FindNearest(EntityKind kind, Vector3 from, Func<KitchenEntity, bool> filter = null)
        {
            KitchenEntity best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < s_All.Count; i++)
            {
                var e = s_All[i];
                if (e == null || e.Consumed || e.kind != kind)
                    continue;
                if (filter != null && !filter(e))
                    continue;
                var d = (e.transform.position - from).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            return best;
        }

        public static int Count(EntityKind kind)
        {
            var n = 0;
            for (var i = 0; i < s_All.Count; i++)
                if (s_All[i] != null && !s_All[i].Consumed && s_All[i].kind == kind)
                    n++;
            return n;
        }

        public static KitchenEntity FromCollider(Collider c)
        {
            if (c == null)
                return null;
            return c.GetComponentInParent<KitchenEntity>();
        }
    }
}
