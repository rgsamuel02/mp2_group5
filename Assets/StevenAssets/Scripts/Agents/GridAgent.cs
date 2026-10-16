using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Moves an Instance across the kitchen grid along an A*-planned path. Re-plans dynamically when the
    /// layout changes (a counter is built in the way, a walkway is removed) and draws its planned route
    /// as a scrolling chevron line so the planning is visible.
    /// </summary>
    public class GridAgent : MonoBehaviour
    {
        public PathMode mode = PathMode.PathwaysOnly;
        public float speed = 0.9f;
        public float turnSpeed = 540f;
        [Tooltip("Also re-plan every N seconds even if the layout did not change (0 = only on change).")]
        public float replanInterval;
        public LineRenderer pathLine;
        public float pathLineHeight = 0.04f;
        public Transform bobVisual;
        public float bobHeight = 0.025f;
        public float bobFrequency = 3.5f;
        [Tooltip("Wear added to a Walkway Mat each time this agent steps onto it.")]
        public float wearPerStep = 0.5f;

        readonly List<Vector2Int> m_Cells = new List<Vector2Int>();
        readonly List<Vector3> m_Points = new List<Vector3>();
        int m_Index;
        bool m_HasGoal;
        Vector2Int m_GoalCell;
        bool m_HasFinalPoint;
        Vector3 m_FinalPoint;
        int m_PlannedVersion;
        float m_ReplanTimer;
        float m_BobPhase;
        Vector3 m_BobBase;
        bool m_HasBobBase;

        public bool IsMoving => m_HasGoal && m_Index < m_Points.Count;
        public bool HasGoal => m_HasGoal;
        public IReadOnlyList<Vector2Int> PlannedCells => m_Cells;

        public event Action ArrivedAtGoal;
        public event Action PathLost;

        KitchenGrid Grid => KitchenGame.Grid;

        public Vector2Int CurrentCell => Grid != null ? Grid.WorldToCell(transform.position) : Vector2Int.zero;

        void Awake()
        {
            if (bobVisual != null)
            {
                m_BobBase = bobVisual.localPosition;
                m_HasBobBase = true;
            }
            m_BobPhase = UnityEngine.Random.value * 6f;
        }

        /// <summary>Plans to goalCell. If finalPoint is given the agent walks onto it at the end (to "bump" into a target).</summary>
        public bool PlanTo(Vector2Int goalCell, Vector3? finalPoint = null)
        {
            var grid = Grid;
            if (grid == null)
                return false;
            m_GoalCell = goalCell;
            m_HasFinalPoint = finalPoint.HasValue;
            m_FinalPoint = finalPoint ?? Vector3.zero;
            if (!GridPathfinder.FindPath(grid, CurrentCell, goalCell, mode, m_Cells))
            {
                m_HasGoal = false;
                ClearLine();
                return false;
            }
            m_HasGoal = true;
            m_PlannedVersion = grid.LayoutVersion;
            m_ReplanTimer = 0f;
            BuildPoints();
            return true;
        }

        public void Stop()
        {
            m_HasGoal = false;
            m_Points.Clear();
            m_Cells.Clear();
            ClearLine();
        }

        void BuildPoints()
        {
            var grid = Grid;
            m_Points.Clear();
            var floorY = grid.origin.y;
            // Skip the start cell; walk through every later cell center.
            for (var i = 1; i < m_Cells.Count; i++)
            {
                var p = grid.CellCenter(m_Cells[i]);
                p.y = floorY;
                m_Points.Add(p);
            }
            if (m_HasFinalPoint)
            {
                var fp = m_FinalPoint;
                fp.y = floorY;
                if (m_Points.Count > 0)
                    m_Points[m_Points.Count - 1] = fp;
                else
                    m_Points.Add(fp);
            }
            m_Index = 0;
            DrawLine();
        }

        void Update()
        {
            var dt = Time.deltaTime;
            Bob(dt);
            if (!m_HasGoal)
                return;

            var grid = Grid;
            if (grid != null)
            {
                m_ReplanTimer += dt;
                var layoutChanged = grid.LayoutVersion != m_PlannedVersion;
                var timed = replanInterval > 0f && m_ReplanTimer >= replanInterval;
                if (layoutChanged || timed)
                {
                    // Dynamic re-planning: route around whatever was just built or removed.
                    var saveFinal = m_HasFinalPoint ? (Vector3?)m_FinalPoint : null;
                    if (!PlanTo(m_GoalCell, saveFinal))
                    {
                        PathLost?.Invoke();
                        return;
                    }
                }
            }

            if (m_Index >= m_Points.Count)
            {
                m_HasGoal = false;
                ClearLine();
                ArrivedAtGoal?.Invoke();
                return;
            }

            var target = m_Points[m_Index];
            var pos = transform.position;
            target.y = pos.y;
            var to = target - pos;
            var step = speed * dt;
            if (to.magnitude <= step)
            {
                transform.position = target;
                m_Index++;
                // Foot traffic wears the walkway mats down a little.
                if (grid != null)
                {
                    var top = grid.TopAt(grid.WorldToCell(target));
                    if (top != null && top.stats != null && top.stats.Has(StatId.Wear))
                        top.stats.Add(StatId.Wear, wearPerStep);
                }
                if (pathLine != null && m_Index < m_Points.Count)
                    DrawLine();
            }
            else
            {
                transform.position = pos + to.normalized * step;
            }

            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
            {
                var want = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
            }

            if (pathLine != null && pathLine.enabled && pathLine.positionCount > 0)
            {
                var head = transform.position;
                head.y = (Grid != null ? Grid.origin.y : 0f) + pathLineHeight;
                pathLine.SetPosition(0, head);
            }
        }

        void Bob(float dt)
        {
            if (bobVisual == null || !m_HasBobBase)
                return;
            var moving = IsMoving;
            m_BobPhase += dt * bobFrequency * (moving ? 1f : 0.25f);
            bobVisual.localPosition = m_BobBase + Vector3.up * (Mathf.Abs(Mathf.Sin(m_BobPhase * Mathf.PI)) * (moving ? bobHeight : bobHeight * 0.3f));
        }

        void DrawLine()
        {
            if (pathLine == null)
                return;
            var count = m_Points.Count - m_Index + 1;
            if (count < 2)
            {
                ClearLine();
                return;
            }
            pathLine.positionCount = count;
            var p0 = transform.position;
            p0.y = Grid != null ? Grid.origin.y + pathLineHeight : pathLineHeight;
            pathLine.SetPosition(0, p0);
            for (var i = m_Index; i < m_Points.Count; i++)
            {
                var p = m_Points[i];
                p.y += pathLineHeight;
                pathLine.SetPosition(i - m_Index + 1, p);
            }
            pathLine.enabled = true;
        }

        void ClearLine()
        {
            if (pathLine == null)
                return;
            pathLine.positionCount = 0;
            pathLine.enabled = false;
        }
    }
}
