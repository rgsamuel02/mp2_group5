using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    public struct PlacementResult
    {
        public bool ok;
        public string reason;
        public bool foundationProblem;
        public bool costProblem;

        public static PlacementResult Ok => new PlacementResult { ok = true, reason = "Ready to install" };

        public static PlacementResult Fail(string reason, bool foundation = false, bool cost = false) =>
            new PlacementResult { ok = false, reason = reason, foundationProblem = foundation, costProblem = cost };
    }

    /// <summary>
    /// The kitchen floor grid. Every Building snaps to a cell; each cell holds a vertical stack of buildings
    /// (e.g. Floor Tile -> Gas Line -> Range Stove -> Saute Pan). The grid answers placement, neighbor and
    /// walkability queries. Buildings placed in the scene register themselves on enable.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public class KitchenGrid : MonoBehaviour
    {
        public static readonly Vector2Int[] Directions4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        public KitchenDatabase database;

        [Tooltip("World position of the minimum corner of cell (0,0). Y is the floor height.")]
        public Vector3 origin = new Vector3(-4.8f, 0f, 0f);
        public float cellSize = 0.6f;
        public int width = 16;
        public int depth = 12;

        [Tooltip("Cells occupied by fixed fixtures (the Pass, walls...). Nothing can be built there and nobody walks there.")]
        public List<Vector2Int> blockedCells = new List<Vector2Int>();

        [Tooltip("Parent for buildings created at runtime.")]
        public Transform buildingsRoot;

        [Tooltip("Small vertical gap between stacked buildings to avoid z-fighting.")]
        public float stackEpsilon = 0.002f;

        List<GridBuilding>[] m_Cells;
        HashSet<int> m_Blocked;
        readonly List<GridBuilding> m_All = new List<GridBuilding>();

        public event Action<GridBuilding> BuildingAdded;
        public event Action<GridBuilding> BuildingRemoved;

        /// <summary>Increments whenever the layout changes, so agents know to re-plan.</summary>
        public int LayoutVersion { get; private set; }

        public IReadOnlyList<GridBuilding> AllBuildings => m_All;

        KitchenDatabase Db => database != null ? database : KitchenGame.Db;

        void EnsureInit()
        {
            if (m_Cells != null && m_Cells.Length == width * depth)
                return;
            m_Cells = new List<GridBuilding>[Mathf.Max(1, width * depth)];
            for (var i = 0; i < m_Cells.Length; i++)
                m_Cells[i] = new List<GridBuilding>(4);
            m_Blocked = new HashSet<int>();
            foreach (var c in blockedCells)
                if (InBounds(c))
                    m_Blocked.Add(Index(c));
        }

        int Index(Vector2Int c) => c.y * width + c.x;

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < depth;

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = world - origin;
            return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
        }

        /// <summary>Center of the cell on the floor plane.</summary>
        public Vector3 CellCenter(Vector2Int c) =>
            origin + new Vector3((c.x + 0.5f) * cellSize, 0f, (c.y + 0.5f) * cellSize);

        public bool IsStaticBlocked(Vector2Int c)
        {
            EnsureInit();
            return InBounds(c) && m_Blocked.Contains(Index(c));
        }

        public IReadOnlyList<GridBuilding> StackAt(Vector2Int c)
        {
            EnsureInit();
            return InBounds(c) ? m_Cells[Index(c)] : (IReadOnlyList<GridBuilding>)Array.Empty<GridBuilding>();
        }

        public GridBuilding TopAt(Vector2Int c)
        {
            var stack = StackAt(c);
            return stack.Count > 0 ? stack[stack.Count - 1] : null;
        }

        public GridBuilding FindInStack(Vector2Int c, BuildingId id)
        {
            var stack = StackAt(c);
            for (var i = 0; i < stack.Count; i++)
                if (stack[i] != null && stack[i].buildingId == id)
                    return stack[i];
            return null;
        }

        /// <summary>Height at which the next building on this cell would sit.</summary>
        public float SurfaceHeight(Vector2Int c)
        {
            var top = TopAt(c);
            return top != null ? top.TopHeight + stackEpsilon : origin.y;
        }

        // ------------------------------------------------------------------ placement

        public PlacementResult CheckPlacement(BuildingId id, Vector2Int cell, bool checkCost = true)
        {
            EnsureInit();
            var db = Db;
            var info = db != null ? db.GetBuilding(id) : null;
            if (info == null || info.prefab == null)
                return PlacementResult.Fail("Unknown building");
            if (!InBounds(cell))
                return PlacementResult.Fail("Outside the kitchen grid");
            if (m_Blocked.Contains(Index(cell)))
                return PlacementResult.Fail("A fixture is in the way");

            var top = TopAt(cell);
            if (info.foundations == null || info.foundations.Count == 0)
            {
                if (top != null)
                    return PlacementResult.Fail($"Cell already has a {db.BuildingName(top.buildingId)}", true);
            }
            else
            {
                // BuildingId.None in the list means "bare floor is also fine".
                var bareOk = info.foundations.Contains(BuildingId.None);
                if (top == null && bareOk)
                {
                    // allowed
                }
                else if (top == null || !info.foundations.Contains(top.buildingId))
                {
                    var have = top != null ? db.BuildingName(top.buildingId) : "bare floor";
                    return PlacementResult.Fail($"Needs {db.DescribeFoundations(info)} first (found {have})", true);
                }
            }

            if (checkCost)
            {
                var res = KitchenGame.Res;
                if (res != null && !res.Has(info.cost))
                    return PlacementResult.Fail(res.DescribeMissing(info.cost) ?? "Can't afford it", false, true);
            }

            return PlacementResult.Ok;
        }

        /// <summary>Spawns a building instance on the grid. Used by the Equipment Catalog minis.</summary>
        public GridBuilding Build(BuildingId id, Vector2Int cell, int rotationSteps, bool payCost)
        {
            var check = CheckPlacement(id, cell, payCost);
            if (!check.ok)
                return null;
            var db = Db;
            var info = db.GetBuilding(id);
            if (payCost && KitchenGame.Res != null && !KitchenGame.Res.TrySpend(info.cost))
                return null;

            var level = StackAt(cell).Count;
            var position = CellCenter(cell);
            position.y = SurfaceHeight(cell);
            var rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
            var go = Instantiate(info.prefab, position, rotation, buildingsRoot != null ? buildingsRoot : transform);
            go.name = info.displayName + $" ({cell.x},{cell.y})";
            var building = go.GetComponent<GridBuilding>();
            if (building == null)
            {
                Debug.LogError($"Prefab for {id} has no GridBuilding component", go);
                Destroy(go);
                return null;
            }

            building.PlaceAt(this, cell, level, rotationSteps);
            return building;
        }

        internal void Register(GridBuilding b)
        {
            EnsureInit();
            if (!InBounds(b.cell))
            {
                Debug.LogWarning($"{b.name} is outside the grid at {b.cell}", b);
                return;
            }
            var stack = m_Cells[Index(b.cell)];
            if (stack.Contains(b))
                return;
            var insertAt = stack.Count;
            for (var i = 0; i < stack.Count; i++)
            {
                if (stack[i] != null && stack[i].stackLevel > b.stackLevel)
                {
                    insertAt = i;
                    break;
                }
            }
            stack.Insert(insertAt, b);
            m_All.Add(b);
            LayoutVersion++;
            BuildingAdded?.Invoke(b);
        }

        internal void Unregister(GridBuilding b)
        {
            if (m_Cells == null || !InBounds(b.cell))
                return;
            var stack = m_Cells[Index(b.cell)];
            if (stack.Remove(b))
            {
                m_All.Remove(b);
                LayoutVersion++;
                BuildingRemoved?.Invoke(b);
            }
        }

        /// <summary>Removes the top building of a cell (and refunds part of its cost).</summary>
        public bool DemolishTop(Vector2Int cell, float refundFraction)
        {
            var top = TopAt(cell);
            if (top == null)
                return false;
            var info = top.Info;
            if (info != null && KitchenGame.Res != null && refundFraction > 0f)
            {
                foreach (var c in info.cost)
                {
                    var refund = Mathf.FloorToInt(c.amount * refundFraction);
                    if (refund > 0)
                        KitchenGame.Res.Add(c.id, refund);
                }
            }
            top.Demolish();
            return true;
        }

        // ------------------------------------------------------------------ walkability / neighbors

        public bool IsWalkable(Vector2Int c, PathMode mode)
        {
            EnsureInit();
            if (!InBounds(c) || m_Blocked.Contains(Index(c)))
                return false;
            var stack = m_Cells[Index(c)];
            if (mode == PathMode.PathwaysOnly)
            {
                var top = stack.Count > 0 ? stack[stack.Count - 1] : null;
                return top != null && top.Info != null && top.Info.isPathway;
            }

            for (var i = 0; i < stack.Count; i++)
            {
                var info = stack[i] != null ? stack[i].Info : null;
                if (info != null && info.blocksMovement)
                    return false;
            }
            return true;
        }

        public void GetNeighborCells(Vector2Int c, List<Vector2Int> results)
        {
            results.Clear();
            foreach (var d in Directions4)
            {
                var n = c + d;
                if (InBounds(n))
                    results.Add(n);
            }
        }

        /// <summary>Closest walkable cell (by Manhattan rings) to a cell, or null.</summary>
        public Vector2Int? NearestWalkable(Vector2Int from, PathMode mode, int maxRadius = 6)
        {
            if (IsWalkable(from, mode))
                return from;
            for (var r = 1; r <= maxRadius; r++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    var dz = r - Mathf.Abs(dx);
                    var a = new Vector2Int(from.x + dx, from.y + dz);
                    if (IsWalkable(a, mode))
                        return a;
                    var b = new Vector2Int(from.x + dx, from.y - dz);
                    if (IsWalkable(b, mode))
                        return b;
                }
            }
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.35f);
            for (var x = 0; x <= width; x++)
                Gizmos.DrawLine(origin + new Vector3(x * cellSize, 0.01f, 0f), origin + new Vector3(x * cellSize, 0.01f, depth * cellSize));
            for (var z = 0; z <= depth; z++)
                Gizmos.DrawLine(origin + new Vector3(0f, 0.01f, z * cellSize), origin + new Vector3(width * cellSize, 0.01f, z * cellSize));
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            foreach (var c in blockedCells)
                Gizmos.DrawCube(CellCenter(c) + Vector3.up * 0.05f, new Vector3(cellSize, 0.1f, cellSize) * 0.9f);
        }
    }
}
