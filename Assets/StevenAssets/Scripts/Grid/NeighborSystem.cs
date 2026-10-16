using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Neighbor interactions: on a regular tick, every pair of buildings on orthogonally adjacent cells
    /// that has a NeighborRule in the database exchanges stats (both Instances' stats change).
    /// Example: a Sink next to a Counter spends WaterLevel and raises the Counter's Cleanliness.
    /// </summary>
    public class NeighborSystem : MonoBehaviour
    {
        public KitchenDatabase database;
        public KitchenGrid grid;

        [Tooltip("Seconds between neighbor ticks. Deltas in the rules are per second.")]
        public float tickInterval = 1f;

        [Tooltip("Draw the glowing link + floating text on every Nth tick so the effect stays readable.")]
        public int pulseEveryNTicks = 3;

        [Tooltip("Only draw pulses for pairs within this distance of the player (meters).")]
        public float pulseRadius = 6f;

        float m_Timer;
        int m_Tick;
        readonly List<GridBuilding> m_Snapshot = new List<GridBuilding>(256);
        readonly List<(NeighborRule rule, bool reversed)> m_Rules = new List<(NeighborRule, bool)>();

        /// <summary>(rule, buildingA, buildingB)</summary>
        public event Action<NeighborRule, GridBuilding, GridBuilding> Interacted;

        /// <summary>Number of rule applications on the most recent tick (shown on the debug board).</summary>
        public int LastTickInteractions { get; private set; }

        KitchenDatabase Db => database != null ? database : KitchenGame.Db;
        KitchenGrid Grid => grid != null ? grid : KitchenGame.Grid;

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < tickInterval)
                return;
            var dt = m_Timer;
            m_Timer = 0f;
            Tick(dt);
        }

        static bool AtMin(StatBlock block, StatId id)
        {
            var s = block.Find(id);
            return s != null && s.value <= s.min + 0.001f;
        }

        public void Tick(float dt)
        {
            var g = Grid;
            var db = Db;
            if (g == null || db == null)
                return;

            m_Tick++;
            var showPulse = pulseEveryNTicks <= 1 || m_Tick % pulseEveryNTicks == 0;
            var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHeadPosition : Vector3.zero;
            var interactions = 0;

            m_Snapshot.Clear();
            m_Snapshot.AddRange(g.AllBuildings);

            foreach (var b in m_Snapshot)
            {
                if (b == null || b.stats == null || b.stats.IsDead)
                    continue;
                foreach (var d in KitchenGrid.Directions4)
                {
                    var stack = g.StackAt(b.cell + d);
                    for (var i = 0; i < stack.Count; i++)
                    {
                        var o = stack[i];
                        if (o == null || o == b || o.stats == null || o.stats.IsDead)
                            continue;
                        // Each unordered pair is processed once per tick.
                        if (b.Uid > o.Uid)
                            continue;

                        db.FindNeighborRules(b.buildingId, o.buildingId, m_Rules);
                        foreach (var (rule, reversed) in m_Rules)
                        {
                            var a = reversed ? o : b;
                            var c = reversed ? b : o;
                            if (!a.stats.Has(rule.statA) || !c.stats.Has(rule.statB))
                                continue;
                            // Nothing left to give: a side that would lose a stat already at its minimum stops the exchange.
                            if ((rule.deltaA < 0f && AtMin(a.stats, rule.statA)) || (rule.deltaB < 0f && AtMin(c.stats, rule.statB)))
                                continue;

                            a.stats.Add(rule.statA, rule.deltaA * dt);
                            c.stats.Add(rule.statB, rule.deltaB * dt);
                            interactions++;
                            Interacted?.Invoke(rule, a, c);

                            var mid = (a.transform.position + c.transform.position) * 0.5f + Vector3.up * 1.0f;
                            KitchenGame.Tutorial("neighbor", mid, 2.6f);
                            if (!string.IsNullOrEmpty(rule.tutorialId))
                                KitchenGame.Tutorial(rule.tutorialId, mid, 2.2f);

                            if (showPulse && (mid - head).sqrMagnitude < pulseRadius * pulseRadius)
                            {
                                var fx = KitchenGame.Instance != null ? KitchenGame.Instance.fx : null;
                                if (fx != null)
                                {
                                    fx.NeighborPulse(a.transform.position + Vector3.up * (a.Info != null ? a.Info.height + 0.1f : 1f),
                                        c.transform.position + Vector3.up * (c.Info != null ? c.Info.height + 0.1f : 1f),
                                        rule.color);
                                    fx.FloatText(mid, rule.label, rule.color, 0.6f);
                                }
                            }
                        }
                    }
                }
            }

            LastTickInteractions = interactions;
        }
    }
}
