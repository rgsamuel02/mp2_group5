using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Steven.MP2A
{
    [Serializable]
    public class ToolRule
    {
        public string label;
        public EntityKind targetKind = EntityKind.Building;
        [Tooltip("For Building targets: match any building (that has the stat).")]
        public bool anyBuilding;
        public BuildingId building;
        public ToolAction action;

        [Header("ModifyStat")]
        public StatId stat;
        public float delta;
        [Tooltip("Skip if the target stat is already at its limit (no wasted resources).")]
        public bool onlyIfBelowMax = true;

        [Header("Requirements")]
        [Tooltip("Tool must be tilted (spout pointing down), e.g. the watering can.")]
        public bool requireTilt;
        public float minSpeed;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
        public List<ResourceAmount> reward = new List<ResourceAmount>();

        [Header("Tool's own stat (e.g. watering can WaterLevel)")]
        public bool usesToolStat;
        public StatId toolStat;
        [Tooltip("Negative = spent per use (needs at least that much). Positive = refilled.")]
        public float toolStatDelta;

        public Color color = Color.white;
        public string tutorialId;
    }

    /// <summary>
    /// The "business end" of a hand tool (cleaver blade, watering-can spout, shears, wrench, rolling pin...).
    /// When it touches a matching Instance while held it harvests, shatters, demolishes or modifies a stat
    /// (Harvesting / Shatter / Caretaking affordances).
    /// </summary>
    public class ToolEffect : MonoBehaviour
    {
        public XRGrabInteractable grab;
        public bool requireHeld = true;
        public StatBlock toolStats;
        [Tooltip("Direction (local axis of this transform) that must point down for tilt rules.")]
        public Vector3 tiltAxis = Vector3.forward;
        [Range(-1f, 1f)] public float tiltDotThreshold = 0.35f;
        public float cooldown = 0.4f;
        public List<ToolRule> rules = new List<ToolRule>();

        readonly Dictionary<KitchenEntity, float> m_LastUse = new Dictionary<KitchenEntity, float>();
        Vector3 m_LastPos;
        float m_Speed;
        float m_LastFeedback;

        public float Speed => m_Speed;

        void OnEnable()
        {
            m_LastPos = transform.position;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt > 0f)
            {
                // Peak-hold speed with exponential decay so a quick swing still registers on contact.
                var instant = (transform.position - m_LastPos).magnitude / dt;
                m_Speed = Mathf.Max(instant, m_Speed * Mathf.Exp(-8f * dt));
            }
            m_LastPos = transform.position;
        }

        void OnTriggerEnter(Collider other) => Handle(other);

        void OnTriggerStay(Collider other) => Handle(other);

        void Handle(Collider other)
        {
            if (requireHeld && (grab == null || !grab.isSelected))
                return;
            var target = KitchenEntity.FromCollider(other);
            if (target == null || target.Consumed)
                return;
            if (transform.IsChildOf(target.transform))
                return;
            if (m_LastUse.TryGetValue(target, out var last) && Time.time - last < cooldown)
                return;

            foreach (var rule in rules)
            {
                if (!Matches(rule, target))
                    continue;
                var problem = CheckRequirements(rule, target);
                if (problem != null)
                {
                    if (problem.Length > 0 && Time.time - m_LastFeedback > 1.2f)
                    {
                        m_LastFeedback = Time.time;
                        KitchenGame.Float(target.transform.position + Vector3.up * 1.1f, problem, new Color(1f, 0.6f, 0.5f));
                    }
                    continue;
                }
                if (Execute(rule, target))
                {
                    m_LastUse[target] = Time.time;
                    break;
                }
            }
        }

        bool Matches(ToolRule rule, KitchenEntity target)
        {
            if (rule.targetKind != target.kind)
                return false;
            if (target.kind == EntityKind.Building && !rule.anyBuilding)
            {
                var gb = target.GetComponent<GridBuilding>();
                return gb != null && gb.buildingId == rule.building;
            }
            return true;
        }

        /// <summary>null = OK, "" = silently skip, otherwise a message to show.</summary>
        string CheckRequirements(ToolRule rule, KitchenEntity target)
        {
            if (rule.minSpeed > 0f && m_Speed < rule.minSpeed)
                return "";
            if (rule.requireTilt)
            {
                var axis = transform.TransformDirection(tiltAxis).normalized;
                if (Vector3.Dot(axis, Vector3.down) < tiltDotThreshold)
                    return "";
            }
            var stats = target.Stats;
            if (rule.action == ToolAction.ModifyStat || rule.action == ToolAction.ModifyStatAndTool)
            {
                if (stats == null || !stats.Has(rule.stat) || stats.IsDead)
                    return "";
                if (rule.onlyIfBelowMax && rule.delta > 0f && stats.Get(rule.stat) >= stats.GetMax(rule.stat) - 0.01f)
                    return "";
                if (rule.onlyIfBelowMax && rule.delta < 0f && stats.Get(rule.stat) <= stats.Find(rule.stat).min + 0.01f)
                    return "";
            }
            if (rule.usesToolStat && toolStats != null)
            {
                if (rule.toolStatDelta < 0f && toolStats.Get(rule.toolStat) < -rule.toolStatDelta)
                    return "Empty! Refill the " + rule.toolStat;
                if (rule.toolStatDelta > 0f && toolStats.Get(rule.toolStat) >= toolStats.GetMax(rule.toolStat) - 0.01f)
                    return "";
            }
            var res = KitchenGame.Res;
            if (res != null && rule.cost.Count > 0 && !res.Has(rule.cost))
                return res.DescribeMissing(rule.cost);
            return null;
        }

        bool Execute(ToolRule rule, KitchenEntity target)
        {
            var pos = target.transform.position + Vector3.up * 0.9f;
            var ok = false;
            switch (rule.action)
            {
                case ToolAction.Harvest:
                {
                    var h = target.GetComponent<Harvestable>();
                    ok = h != null && h.TryHarvest();
                    break;
                }
                case ToolAction.Shatter:
                {
                    var s = target.GetComponent<Shatterable>();
                    ok = s != null && s.TryShatter();
                    break;
                }
                case ToolAction.Demolish:
                {
                    var gb = target.GetComponent<GridBuilding>();
                    var grid = KitchenGame.Grid;
                    if (gb != null && grid != null)
                    {
                        var top = grid.TopAt(gb.cell);
                        var name = top != null && top.Info != null ? top.Info.displayName : "building";
                        ok = grid.DemolishTop(gb.cell, 0.5f);
                        if (ok)
                            KitchenGame.Float(pos, "Removed " + name + " (50% refund)", new Color(0.85f, 0.85f, 0.85f));
                    }
                    break;
                }
                default:
                {
                    var stats = target.Stats;
                    if (stats != null)
                    {
                        stats.Add(rule.stat, rule.delta);
                        ok = true;
                        KitchenGame.Float(pos, rule.label, rule.color, 0.8f);
                    }
                    break;
                }
            }

            if (!ok)
                return false;

            var res = KitchenGame.Res;
            if (res != null)
            {
                res.TrySpend(rule.cost);
                res.Add(rule.reward);
            }
            if (rule.usesToolStat && toolStats != null)
                toolStats.Add(rule.toolStat, rule.toolStatDelta);
            if (!string.IsNullOrEmpty(rule.tutorialId))
                KitchenGame.Tutorial(rule.tutorialId, pos);
            if ((rule.action == ToolAction.ModifyStat || rule.action == ToolAction.ModifyStatAndTool) && rule.delta > 0f)
                KitchenGame.Tutorial("caretaking", pos);
            return true;
        }
    }
}
