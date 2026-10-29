using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Interconnected Sims: lets teammates' prefabs feed the kitchen.
    /// Mode "Collision": a teammate's object that enters this trigger (e.g. a garden tomato dropped in the pantry
    /// chute, a cafe coffee placed on a dining table) is consumed and changes my resources and/or a stat.
    /// Mode "Nearby": every few seconds, teammate objects within range (e.g. an orchestra violin next to the
    /// dining room) change a stat, without being consumed.
    /// Objects are matched by name, so no code from the other sims is needed.
    /// </summary>
    public class PartnerIntake : MonoBehaviour
    {
        public enum Mode { Collision, Nearby }

        [Serializable]
        public class Rule
        {
            [Tooltip("Matches objects whose name contains this text (not case sensitive), e.g. \"Tomato\". Separate alternatives with |, e.g. \"violin|cello|flute\".")]
            public string nameContains = "Tomato";
            [Tooltip("Whose sim it comes from, shown in the feedback, e.g. \"Garden\".")]
            public string partner = "Garden";
            public bool giveResource = true;
            public ResourceId resource = ResourceId.Tomato;
            public float amount = 1f;
            public bool changeStat;
            [Tooltip("StatBlock to change (e.g. a Dining Table's). Empty = the one on this object or its parents.")]
            public StatBlock target;
            public StatId stat = StatId.Patience;
            public float statDelta = 10f;
            public string message = "Partner delivery!";
            public Color color = new Color(0.5f, 1f, 0.6f);
        }

        public Mode mode = Mode.Collision;
        public List<Rule> rules = new List<Rule>();
        [Header("Nearby mode")]
        public float radius = 2f;
        public float interval = 3f;

        float m_Timer;
        readonly Collider[] m_Hits = new Collider[64];
        readonly HashSet<GameObject> m_Seen = new HashSet<GameObject>();

        void Awake()
        {
            if (mode != Mode.Collision)
                return;
            // Triggers need a Rigidbody on one side; a kinematic one here makes any object work.
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var c in GetComponents<Collider>())
                c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (mode != Mode.Collision || !enabled)
                return;
            var go = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (go == gameObject || go.transform.IsChildOf(transform))
                return;
            var rule = Match(go, out var matched);
            if (rule == null)
                return;
            Apply(rule, matched.position);
            Destroy(matched.gameObject);
        }

        void Update()
        {
            if (mode != Mode.Nearby)
                return;
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f)
                return;
            m_Timer = interval;
            m_Seen.Clear();
            var n = Physics.OverlapSphereNonAlloc(transform.position, radius, m_Hits, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < n; i++)
            {
                var c = m_Hits[i];
                var go = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
                if (go == gameObject || go.transform.IsChildOf(transform))
                    continue;
                var rule = Match(go, out var matched);
                if (rule == null || !m_Seen.Add(matched.gameObject))
                    continue;
                Apply(rule, matched.position);
                var game = KitchenGame.Instance;
                if (game != null && game.fx != null)
                    game.fx.NeighborPulse(matched.position, transform.position, rule.color);
            }
        }

        /// <summary>Checks the object and its parents (a collider is often on a child like "offset").</summary>
        Rule Match(GameObject go, out Transform matched)
        {
            for (var t = go.transform; t != null; t = t.parent)
            {
                if (t == transform)
                    break;
                foreach (var r in rules)
                {
                    if (NameMatches(t.name, r.nameContains))
                    {
                        matched = t;
                        return r;
                    }
                }
            }
            matched = null;
            return null;
        }

        static bool NameMatches(string name, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
            foreach (var part in pattern.Split('|'))
            {
                var p = part.Trim();
                if (p.Length > 0 && name.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        void Apply(Rule r, Vector3 from)
        {
            var text = r.message;
            if (r.giveResource && KitchenGame.Res != null)
            {
                KitchenGame.Res.Add(r.resource, r.amount);
                text += $"  +{r.amount:0} {KitchenGame.ResourceName(r.resource)}";
            }
            if (r.changeStat)
            {
                var target = r.target != null ? r.target : GetComponentInParent<StatBlock>();
                if (target != null && target.Has(r.stat))
                {
                    target.Add(r.stat, r.statDelta);
                    text += $"  {(r.statDelta >= 0 ? "+" : "")}{r.statDelta:0} {r.stat}";
                }
            }
            if (!string.IsNullOrEmpty(r.partner))
                text = "[" + r.partner + "] " + text;
            KitchenGame.Float(from + Vector3.up * 0.35f, text, r.color, 1.1f);
            KitchenGame.Blast(from, r.color, 0.5f);
        }

        void OnDrawGizmosSelected()
        {
            if (mode != Mode.Nearby)
                return;
            Gizmos.color = new Color(0.5f, 1f, 0.6f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
