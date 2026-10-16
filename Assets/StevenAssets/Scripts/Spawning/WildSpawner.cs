using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Wild Instances + Intermittent Instances. Each channel scatters its prefabs at random positions inside a
    /// region when the game starts (wild crates, mushrooms, a rat, loose produce), then keeps spawning more at
    /// random times (supplier deliveries, rats sneaking in, mushrooms regrowing) up to a cap.
    /// Channels are configured in the Inspector; all spawning happens at runtime.
    /// </summary>
    public class WildSpawner : MonoBehaviour
    {
        [Serializable]
        public class Channel
        {
            public string name = "Channel";
            public List<GameObject> prefabs = new List<GameObject>();
            public EntityKind countKind;
            [Tooltip("Spawned at random when the game starts (Wild Instances).")]
            public int initialCount = 3;
            [Tooltip("Random seconds between intermittent spawns (x = min, y = max). 0,0 disables.")]
            public Vector2 interval = new Vector2(30f, 60f);
            public int maxAlive = 6;
            [Tooltip("Random positions inside this box (world space).")]
            public Vector3 regionCenter;
            public Vector3 regionSize = new Vector3(1f, 0f, 1f);
            [Tooltip("If set, spawn at one of these grid cells instead of the region (e.g. rat holes).")]
            public List<Vector2Int> cells = new List<Vector2Int>();
            public float clearance = 0.25f;
            public bool randomYaw = true;
            public float heightOffset;
            public string announce;
            public string tutorialId;

            [NonSerialized] public float timer;
            [NonSerialized] public float next;
        }

        public List<Channel> channels = new List<Channel>();
        public Transform spawnedRoot;
        public bool spawnOnStart = true;

        void Start()
        {
            foreach (var ch in channels)
            {
                ch.timer = 0f;
                ch.next = NextInterval(ch);
                if (!spawnOnStart)
                    continue;
                for (var i = 0; i < ch.initialCount; i++)
                    SpawnOne(ch, false);
            }
        }

        void Update()
        {
            var dt = Time.deltaTime;
            foreach (var ch in channels)
            {
                if (ch.interval.y <= 0f)
                    continue;
                ch.timer += dt;
                if (ch.timer < ch.next)
                    continue;
                ch.timer = 0f;
                ch.next = NextInterval(ch);
                if (ch.countKind != EntityKind.None && KitchenEntity.Count(ch.countKind) >= ch.maxAlive)
                    continue;
                SpawnOne(ch, true);
            }
        }

        static float NextInterval(Channel ch) => UnityEngine.Random.Range(Mathf.Max(1f, ch.interval.x), Mathf.Max(1f, ch.interval.y));

        /// <summary>Spawns one Instance of a channel by name (used by the debug board / video helpers).</summary>
        public GameObject Spawn(string channelName)
        {
            foreach (var ch in channels)
                if (ch.name == channelName)
                    return SpawnOne(ch, true);
            return null;
        }

        GameObject SpawnOne(Channel ch, bool announce)
        {
            if (ch.prefabs.Count == 0)
                return null;
            var prefab = ch.prefabs[UnityEngine.Random.Range(0, ch.prefabs.Count)];
            if (prefab == null)
                return null;
            if (!TryPickPosition(ch, out var pos, out var cell))
                return null;
            var rot = ch.randomYaw ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) : Quaternion.identity;
            var go = Instantiate(prefab, pos + Vector3.up * ch.heightOffset, rot, spawnedRoot != null ? spawnedRoot : transform);
            // Make the new collider visible to the next CheckSphere in this same frame.
            Physics.SyncTransforms();
            var rat = go.GetComponent<RatAgent>();
            if (rat != null && cell.HasValue)
                rat.homeCell = cell.Value;
            if (announce)
            {
                if (!string.IsNullOrEmpty(ch.announce))
                    KitchenGame.Float(pos + Vector3.up * 0.8f, ch.announce, new Color(1f, 0.9f, 0.6f));
                KitchenGame.Tutorial("intermittent", pos + Vector3.up * 0.9f);
            }
            if (!string.IsNullOrEmpty(ch.tutorialId))
                KitchenGame.Tutorial(ch.tutorialId, pos + Vector3.up * 0.9f);
            return go;
        }

        bool TryPickPosition(Channel ch, out Vector3 pos, out Vector2Int? cell)
        {
            var grid = KitchenGame.Grid;
            cell = null;
            if (ch.cells.Count > 0 && grid != null)
            {
                var c = ch.cells[UnityEngine.Random.Range(0, ch.cells.Count)];
                cell = c;
                pos = grid.CellCenter(c);
                return true;
            }

            for (var attempt = 0; attempt < 12; attempt++)
            {
                var p = ch.regionCenter + new Vector3(
                    (UnityEngine.Random.value - 0.5f) * ch.regionSize.x,
                    0f,
                    (UnityEngine.Random.value - 0.5f) * ch.regionSize.z);
                if (grid != null)
                {
                    var c = grid.WorldToCell(p);
                    if (grid.InBounds(c) && !grid.IsWalkable(c, PathMode.AvoidBuildings))
                        continue;
                }
                if (ch.clearance > 0f && Physics.CheckSphere(p + Vector3.up * (ch.clearance + 0.05f), ch.clearance, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                pos = p;
                return true;
            }
            pos = ch.regionCenter;
            return false;
        }
    }
}
