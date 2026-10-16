using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// A pest that sneaks out of a hole in the wall and plans a path AROUND buildings (A*, AvoidBuildings mode)
    /// toward the nearest food: a supply crate or dropped food scraps. Re-plans whenever you build in its way.
    /// Collisions: walking into a baited Mouse Trap kills it; reaching a crate steals from it; eating scraps
    /// satisfies its Hunger so it scurries home. Whack it with the rolling pin to drop its Health to zero.
    /// </summary>
    public class RatAgent : MonoBehaviour
    {
        public GridAgent agent;
        public StatBlock stats;
        public KitchenEntity entity;
        public Vector2Int homeCell;
        public float retargetInterval = 2.5f;
        [Tooltip("Hunger at or below this sends the rat home.")]
        public float satisfiedHunger = 20f;
        public float maxLifetime = 150f;

        KitchenEntity m_Target;
        float m_Timer;
        float m_Age;
        bool m_GoingHome;
        bool m_Wandering;

        void Start()
        {
            if (agent != null)
            {
                agent.ArrivedAtGoal += OnArrived;
                agent.PathLost += OnPathLost;
            }
            KitchenGame.Tutorial("rat", transform.position + Vector3.up * 0.5f);
            Retarget();
        }

        void OnDestroy()
        {
            if (agent != null)
            {
                agent.ArrivedAtGoal -= OnArrived;
                agent.PathLost -= OnPathLost;
            }
        }

        void Update()
        {
            if (stats != null && stats.IsDead)
                return;
            m_Age += Time.deltaTime;
            if (!m_GoingHome && (m_Age > maxLifetime || (stats != null && stats.Has(StatId.Hunger) && stats.Get(StatId.Hunger) <= satisfiedHunger)))
            {
                GoHome();
                return;
            }

            m_Timer += Time.deltaTime;
            if (m_Timer < retargetInterval)
                return;
            m_Timer = 0f;
            if (!m_GoingHome && (m_Target == null || m_Target.Consumed || m_Wandering))
                Retarget();
        }

        void Retarget()
        {
            var grid = KitchenGame.Grid;
            if (grid == null || agent == null)
                return;
            var pos = transform.position;
            var crate = KitchenEntity.FindNearest(EntityKind.Crate, pos);
            var scraps = KitchenEntity.FindNearest(EntityKind.FoodScraps, pos);
            KitchenEntity best = crate;
            if (scraps != null && (best == null || (scraps.transform.position - pos).sqrMagnitude < (best.transform.position - pos).sqrMagnitude))
                best = scraps;

            if (best != null)
            {
                var cell = grid.WorldToCell(best.transform.position);
                if (grid.InBounds(cell) && agent.PlanTo(cell, best.transform.position))
                {
                    m_Target = best;
                    m_Wandering = false;
                    return;
                }
            }

            // Nothing to eat (or no route): wander to a random reachable cell.
            m_Target = null;
            m_Wandering = true;
            for (var i = 0; i < 8; i++)
            {
                var c = new Vector2Int(Random.Range(0, grid.width), Random.Range(0, grid.depth));
                if (grid.IsWalkable(c, agent.mode) && agent.PlanTo(c))
                    return;
            }
        }

        void GoHome()
        {
            m_GoingHome = true;
            m_Target = null;
            if (agent == null || !agent.PlanTo(homeCell))
                Vanish();
        }

        void OnArrived()
        {
            if (m_GoingHome)
            {
                Vanish();
                return;
            }
            // If we reached the target without a collision (e.g. it moved), pick something else.
            m_Timer = retargetInterval;
        }

        void OnPathLost()
        {
            if (m_GoingHome)
                Vanish();
            else
                m_Timer = retargetInterval;
        }

        void Vanish()
        {
            KitchenGame.Blast(transform.position + Vector3.up * 0.05f, new Color(0.4f, 0.35f, 0.3f), 0.3f);
            if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
        }
    }
}
