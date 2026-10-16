using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// A Building Instance that lives on a grid cell. Buildings placed in the scene carry their cell in serialized data
    /// and register with the grid when enabled; runtime-built ones are placed by <see cref="KitchenGrid.Build"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class GridBuilding : MonoBehaviour
    {
        public BuildingId buildingId;
        public Vector2Int cell;
        public int stackLevel;
        public int rotationSteps;

        [Tooltip("True for instances placed in the scene in the Editor (they register themselves on enable).")]
        public bool placedInScene;

        public StatBlock stats;
        public KitchenEntity entity;

        [Tooltip("Optional crafting station on this building (to refund crew if it is destroyed).")]
        public CraftingStation station;

        KitchenGrid m_Grid;
        bool m_Registered;
        bool m_Placed;
        int m_Uid;
        static int s_NextUid;

        /// <summary>Stable per-session id (used to process each neighbor pair once per tick).</summary>
        public int Uid => m_Uid != 0 ? m_Uid : (m_Uid = ++s_NextUid);

        public KitchenGrid Grid => m_Grid != null ? m_Grid : (m_Grid = KitchenGame.Grid);

        public BuildingInfo Info
        {
            get
            {
                var db = KitchenGame.Db;
                return db != null ? db.GetBuilding(buildingId) : null;
            }
        }

        public float BaseHeight => transform.position.y;

        public float TopHeight
        {
            get
            {
                var info = Info;
                return BaseHeight + (info != null ? info.height : 0f);
            }
        }

        public bool IsPlaced => m_Placed || placedInScene;

        /// <summary>Building directly below in the same cell (its foundation), if any.</summary>
        public GridBuilding Foundation
        {
            get
            {
                var grid = Grid;
                if (grid == null)
                    return null;
                var stack = grid.StackAt(cell);
                var index = IndexOf(stack);
                return index > 0 ? stack[index - 1] : null;
            }
        }

        /// <summary>Building directly above in the same cell, if any.</summary>
        public GridBuilding Above
        {
            get
            {
                var grid = Grid;
                if (grid == null)
                    return null;
                var stack = grid.StackAt(cell);
                var index = IndexOf(stack);
                return index >= 0 && index + 1 < stack.Count ? stack[index + 1] : null;
            }
        }

        int IndexOf(System.Collections.Generic.IReadOnlyList<GridBuilding> stack)
        {
            for (var i = 0; i < stack.Count; i++)
                if (stack[i] == this)
                    return i;
            return -1;
        }

        internal void PlaceAt(KitchenGrid grid, Vector2Int newCell, int level, int rotation)
        {
            m_Grid = grid;
            cell = newCell;
            stackLevel = level;
            rotationSteps = rotation;
            m_Placed = true;
            TryRegister();
            KitchenGame.Blast(transform.position + Vector3.up * 0.05f, new Color(1f, 0.6f, 0.2f), 0.6f);
        }

        void OnEnable()
        {
            if (stats == null)
                stats = GetComponent<StatBlock>();
            if (entity == null)
                entity = GetComponent<KitchenEntity>();
            TryRegister();
        }

        void OnDisable()
        {
            if (m_Registered && Grid != null)
                Grid.Unregister(this);
            m_Registered = false;
        }

        void TryRegister()
        {
            if (m_Registered || !IsPlaced || !isActiveAndEnabled)
                return;
            var grid = Grid;
            if (grid == null)
                return;
            grid.Register(this);
            m_Registered = true;
        }

        /// <summary>Wired (persistent UnityEvent) to StatBlock.onDied in the Inspector.</summary>
        public void OnDied()
        {
            CollapseAbove();
            RefundCrew();
            if (m_Registered && Grid != null)
                Grid.Unregister(this);
            m_Registered = false;
        }

        /// <summary>Removes this building (and everything stacked on it) without a death.</summary>
        public void Demolish()
        {
            CollapseAbove();
            RefundCrew();
            KitchenGame.Blast(transform.position + Vector3.up * 0.2f, new Color(0.6f, 0.6f, 0.6f), 0.7f);
            if (m_Registered && Grid != null)
                Grid.Unregister(this);
            m_Registered = false;
            Destroy(gameObject);
        }

        void CollapseAbove()
        {
            var above = Above;
            if (above == null || above == this)
                return;
            above.Demolish();
        }

        void RefundCrew()
        {
            if (stats == null || !stats.Has(StatId.Crew))
                return;
            var crew = Mathf.RoundToInt(stats.Get(StatId.Crew));
            if (crew > 0 && KitchenGame.Res != null)
            {
                KitchenGame.Res.Add(ResourceId.LineCooks, crew);
                KitchenGame.Float(transform.position + Vector3.up, $"{crew} cook(s) back to the locker", Color.white);
                stats.Set(StatId.Crew, 0f);
            }
        }
    }
}
