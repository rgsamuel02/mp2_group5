using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Population Resource with Pathing: the "Line Cooks" Resource is embodied by cook Instances idling in the
    /// staff area. Hiring (+Resource) spawns more cooks; calling a cook to a station (-Resource) sends one along
    /// the walkways to its point of use, where it despawns into the station's Crew.
    /// </summary>
    public class PopulationManager : MonoBehaviour
    {
        public CookAgent cookPrefab;
        [Tooltip("Grid cells (should be Walkway Mats) where idle cooks wait.")]
        public List<Vector2Int> staffCells = new List<Vector2Int>();
        public Transform cooksRoot;
        public ResourceId resource = ResourceId.LineCooks;

        readonly List<CookAgent> m_Idle = new List<CookAgent>();
        float m_SyncTimer;

        public int IdleCount
        {
            get
            {
                m_Idle.RemoveAll(c => c == null);
                return m_Idle.Count;
            }
        }

        void Update()
        {
            m_SyncTimer += Time.deltaTime;
            if (m_SyncTimer < 0.3f)
                return;
            m_SyncTimer = 0f;
            Sync();
        }

        /// <summary>Keeps the number of idle cook Instances equal to the Line Cooks resource.</summary>
        void Sync()
        {
            var res = KitchenGame.Res;
            if (res == null || cookPrefab == null)
                return;
            m_Idle.RemoveAll(c => c == null || c.Dispatched);
            var target = res.GetInt(resource);
            var guard = 0;
            while (m_Idle.Count < target && guard++ < 12)
                SpawnCook();
            while (m_Idle.Count > target && m_Idle.Count > 0)
            {
                var last = m_Idle[m_Idle.Count - 1];
                m_Idle.RemoveAt(m_Idle.Count - 1);
                if (last != null)
                {
                    KitchenGame.Blast(last.transform.position + Vector3.up, Color.white, 0.4f);
                    Destroy(last.gameObject);
                }
            }
        }

        void SpawnCook()
        {
            var grid = KitchenGame.Grid;
            Vector3 pos;
            if (grid != null && staffCells.Count > 0)
            {
                var cell = staffCells[m_Idle.Count % staffCells.Count];
                pos = grid.CellCenter(cell);
                pos += new Vector3(Random.Range(-0.12f, 0.12f), 0f, Random.Range(-0.12f, 0.12f));
            }
            else
            {
                pos = transform.position;
            }
            var cook = Instantiate(cookPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), cooksRoot != null ? cooksRoot : transform);
            cook.Say("Ready, Chef!");
            m_Idle.Add(cook);
            KitchenGame.Blast(pos + Vector3.up * 0.9f, new Color(0.6f, 0.8f, 1f), 0.5f);
        }

        /// <summary>Sends the closest idle cook that has a walkway route. Returns a status message.</summary>
        public string Dispatch(CraftingStation station)
        {
            var res = KitchenGame.Res;
            if (res == null || station == null)
                return "No kitchen";
            m_Idle.RemoveAll(c => c == null || c.Dispatched);
            if (m_Idle.Count == 0 || res.GetInt(resource) < 1)
            {
                KitchenGame.Tutorial("line_cooks", station.transform.position + Vector3.up * 1.3f);
                return "No Line Cooks: hire one at the Staff Board";
            }

            var building = station.GetComponentInParent<GridBuilding>();
            if (building == null)
                return "Station is not on the grid";

            m_Idle.Sort((a, b) =>
                (a.transform.position - station.transform.position).sqrMagnitude.CompareTo(
                    (b.transform.position - station.transform.position).sqrMagnitude));

            foreach (var cook in m_Idle)
            {
                if (!cook.TryDispatch(station, building))
                    continue;
                m_Idle.Remove(cook);
                res.Add(resource, -1f);
                KitchenGame.Tutorial("call_cook", cook.transform.position + Vector3.up * 1.8f);
                return "A cook is on the way!";
            }

            KitchenGame.Tutorial("walkway", station.transform.position + Vector3.up * 1.3f);
            return "No walkway route! Lay Walkway Mats to it";
        }
    }
}
