using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Remembers the kitchen while the player is in another scene (the team's scene switching unloads it). Kitchen-only:
    /// it snapshots this scene every second while it is open (no hook in anyone else's code); when StevenScene
    /// loads again, it puts back the Pantry Ledger (gold, reputation, covers, ingredients, line cooks), the goals and
    /// their unlocks, the trophies, what was built or demolished on the grid and the crew at each station.
    /// Customers, orders in progress and loose items start fresh. Put it on the Progression object.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class KitchenSave : MonoBehaviour
    {
        class Built
        {
            public BuildingId id;
            public Vector2Int cell;
            public int level;
            public int rotation;
            public float crew;
        }

        class Data
        {
            public Dictionary<ResourceId, float> resources;
            public int goalsCleared;
            public List<bool> sandboxDone = new List<bool>();
            public List<bool> earned = new List<bool>();
            public HashSet<string> sceneBuildings = new HashSet<string>();
            public Dictionary<string, float> sceneCrew = new Dictionary<string, float>();
            public List<Built> built = new List<Built>();
        }

        [Tooltip("Seconds between snapshots while the kitchen is open.")]
        public float saveInterval = 1f;

        static Data s_Data;
        static KitchenSave s_Live;
        readonly List<GridBuilding> m_SceneBuildings = new List<GridBuilding>();
        float m_Timer;

        public static bool HasSave => s_Data != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Data = null;
            s_Live = null;
        }

        /// <summary>Optional: save right now (it already saves every second). Does nothing if the kitchen isn't open.</summary>
        public static void SaveNow()
        {
            if (s_Live != null)
                s_Live.Capture();
        }

        /// <summary>Forget the saved kitchen (next visit starts from scratch).</summary>
        public static void Forget() => s_Data = null;

        void Awake() => s_Live = this;

        void OnDestroy()
        {
            if (s_Live == this)
                s_Live = null;
        }

        // Runs after the other Start methods (simplifier, progression), so the restore has the final say.
        void Start()
        {
            if (s_Data != null)
                Restore(s_Data);
            // Scene-placed buildings (hidden ones too); one that becomes null later was demolished.
            foreach (var b in FindObjectsByType<GridBuilding>(FindObjectsInactive.Include))
                if (b != null && b.placedInScene && b.gameObject.scene == gameObject.scene)
                    m_SceneBuildings.Add(b);
        }

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < saveInterval)
                return;
            m_Timer = 0f;
            Capture();
        }

        void Capture()
        {
            var res = KitchenGame.Res;
            if (res == null)
                return;
            var d = new Data { resources = res.Snapshot() };

            var progression = GetComponent<KitchenProgression>();
            if (progression == null)
                progression = FindAnyObjectByType<KitchenProgression>();
            if (progression != null)
            {
                d.goalsCleared = progression.GoalsCleared;
                foreach (var g in progression.goals)
                    d.sandboxDone.Add(g.sandboxDone);
                foreach (var a in progression.achievements)
                    d.earned.Add(a.earned);
            }

            // Scene buildings (hidden ones too) that still exist, and everything the player built.
            foreach (var b in m_SceneBuildings)
            {
                if (b == null)
                    continue;
                d.sceneBuildings.Add(b.name);
                d.sceneCrew[b.name] = Crew(b);
            }
            var grid = KitchenGame.Grid;
            if (grid != null)
            {
                foreach (var b in grid.AllBuildings)
                {
                    if (b == null || b.placedInScene)
                        continue;
                    d.built.Add(new Built { id = b.buildingId, cell = b.cell, level = b.stackLevel, rotation = b.rotationSteps, crew = Crew(b) });
                }
            }

            s_Data = d;
        }

        void Restore(Data d)
        {
            var res = KitchenGame.Res;
            if (res != null)
                res.Restore(d.resources);

            // Goals first: they reveal hidden objects, so the building pass below sees the same scene as at save time.
            var progression = GetComponent<KitchenProgression>();
            if (progression == null)
                progression = FindAnyObjectByType<KitchenProgression>();
            if (progression != null)
                progression.RestoreState(d.goalsCleared, d.sandboxDone, d.earned);

            // Scene buildings the player demolished stay gone; the others get their crew back.
            foreach (var b in FindObjectsByType<GridBuilding>(FindObjectsInactive.Include))
            {
                if (b == null || !b.placedInScene || b.gameObject.scene != gameObject.scene)
                    continue;
                if (!d.sceneBuildings.Contains(b.name))
                {
                    b.Demolish();
                    continue;
                }
                if (d.sceneCrew.TryGetValue(b.name, out var crew))
                    SetCrew(b, crew);
            }

            // Rebuild what the player built, bottom of each stack first.
            var grid = KitchenGame.Grid;
            if (grid != null)
            {
                var order = new List<Built>(d.built);
                order.Sort((a, b) => a.level.CompareTo(b.level));
                foreach (var p in order)
                {
                    var b = grid.Build(p.id, p.cell, p.rotation, false);
                    if (b == null)
                        Debug.LogWarning("[KitchenSave] Could not rebuild " + p.id + " at " + p.cell);
                    else
                        SetCrew(b, p.crew);
                }
            }
            Debug.Log("[KitchenSave] Restored kitchen: " + d.goalsCleared + " goals, " + d.built.Count + " built pieces.");
        }

        static float Crew(GridBuilding b)
        {
            var stats = b.stats != null ? b.stats : b.GetComponent<StatBlock>();
            return stats != null && stats.Has(StatId.Crew) ? stats.Get(StatId.Crew) : 0f;
        }

        static void SetCrew(GridBuilding b, float crew)
        {
            if (crew <= 0f)
                return;
            var stats = b.stats != null ? b.stats : b.GetComponent<StatBlock>();
            if (stats != null && stats.Has(StatId.Crew))
                stats.Set(StatId.Crew, crew);
        }
    }
}
