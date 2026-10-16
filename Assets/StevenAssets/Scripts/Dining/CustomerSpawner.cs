using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Intermittent Instances: dinner parties arrive at the front door at random intervals whenever a set table
    /// is free, and leave through it again after eating (or storming out).
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public CustomerAgent customerPrefab;
        [Tooltip("Grid cell of the front door (should hold a Walkway Mat).")]
        public Vector2Int doorCell;
        public Vector2 intervalRange = new Vector2(16f, 34f);
        public float firstArrival = 10f;
        public int maxPartySize = 2;
        public bool spawningEnabled = true;
        public Transform customersRoot;

        float m_Timer;
        float m_Next;
        readonly List<DiningTable> m_Free = new List<DiningTable>();

        void Start()
        {
            m_Next = firstArrival;
        }

        void Update()
        {
            if (!spawningEnabled)
                return;
            m_Timer += Time.deltaTime;
            if (m_Timer < m_Next)
                return;
            m_Timer = 0f;
            m_Next = Random.Range(intervalRange.x, intervalRange.y);
            SpawnArrival();
        }

        /// <summary>Spawns a party for a random free table (also callable from the debug board).</summary>
        public bool SpawnArrival()
        {
            var grid = KitchenGame.Grid;
            if (grid == null || customerPrefab == null)
                return false;
            m_Free.Clear();
            foreach (var t in DiningTable.All)
                if (t != null && t.IsAvailable)
                    m_Free.Add(t);
            if (m_Free.Count == 0)
                return false;
            var table = m_Free[Random.Range(0, m_Free.Count)];
            var c = Spawn(doorCell);
            c.SetPartySize(Random.Range(1, maxPartySize + 1));
            table.Reserve(c);
            c.BeginArrival(table, doorCell);
            return true;
        }

        public void SpawnLeaving(Vector2Int fromCell, bool happy, int party)
        {
            var grid = KitchenGame.Grid;
            if (grid == null || customerPrefab == null)
                return;
            var start = grid.NearestWalkable(fromCell, PathMode.PathwaysOnly, 2) ?? fromCell;
            var c = Spawn(start);
            c.SetPartySize(party);
            c.BeginLeaving(doorCell, happy);
        }

        CustomerAgent Spawn(Vector2Int cell)
        {
            var grid = KitchenGame.Grid;
            var pos = grid.CellCenter(cell);
            return Instantiate(customerPrefab, pos, Quaternion.identity, customersRoot != null ? customersRoot : transform);
        }
    }
}
