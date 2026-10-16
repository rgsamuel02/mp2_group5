using System;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Scene-wide hub that holds references to the managers (assigned in the Inspector) so that runtime-spawned
    /// prefab instances can reach them without scene references inside prefab assets.
    /// This class does not build anything; it only exposes the managers already in the scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class KitchenGame : MonoBehaviour
    {
        static KitchenGame s_Instance;

        public static KitchenGame Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = FindAnyObjectByType<KitchenGame>();
                return s_Instance;
            }
        }

        [Header("Data")]
        public KitchenDatabase database;

        [Header("Managers")]
        public PlayerResources resources;
        public KitchenGrid grid;
        public TutorialManager tutorials;
        public KitchenFX fx;
        public PopulationManager population;
        public OrderRail orderRail;
        public PassConveyor conveyor;
        public NeighborSystem neighbors;
        public CustomerSpawner customers;
        public WildSpawner wildSpawner;

        [Header("Optional")]
        [Tooltip("If empty, Camera.main is used as the player's head.")]
        public Transform playerHeadOverride;

        Transform m_CachedHead;

        /// <summary>Raised whenever an Instance is Destroyed because one of its lethal Stats reached zero.</summary>
        public static event Action<StatBlock, StatId> EntityDied;

        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        public Transform PlayerHead
        {
            get
            {
                if (playerHeadOverride != null)
                    return playerHeadOverride;
                if (m_CachedHead == null || !m_CachedHead.gameObject.activeInHierarchy)
                {
                    var cam = Camera.main;
                    m_CachedHead = cam != null ? cam.transform : null;
                }
                return m_CachedHead;
            }
        }

        public Vector3 PlayerHeadPosition
        {
            get
            {
                var head = PlayerHead;
                return head != null ? head.position : new Vector3(0f, 1.6f, -1f);
            }
        }

        internal static void RaiseEntityDied(StatBlock block, StatId stat)
        {
            EntityDied?.Invoke(block, stat);
        }

        // ---- Null-safe convenience helpers used throughout gameplay code ----

        public static KitchenDatabase Db => Instance != null ? Instance.database : null;

        public static PlayerResources Res => Instance != null ? Instance.resources : null;

        public static KitchenGrid Grid => Instance != null ? Instance.grid : null;

        public static void Float(Vector3 position, string text, Color color, float scale = 1f)
        {
            var game = Instance;
            if (game != null && game.fx != null)
                game.fx.FloatText(position, text, color, scale);
        }

        public static void Blast(Vector3 position, Color color, float size = 0.5f)
        {
            var game = Instance;
            if (game != null && game.fx != null)
                game.fx.Blast(position, color, size);
        }

        public static void Tutorial(string id, Vector3 position)
        {
            var game = Instance;
            if (game != null && game.tutorials != null)
                game.tutorials.Notify(id, position);
        }

        /// <summary>Tutorial that only fires when the player is within <paramref name="radius"/> meters.</summary>
        public static void Tutorial(string id, Vector3 position, float radius)
        {
            var game = Instance;
            if (game != null && game.tutorials != null)
                game.tutorials.Notify(id, position, radius);
        }

        public static string ResourceName(ResourceId id)
        {
            var db = Db;
            return db != null ? db.ResourceName(id) : id.ToString();
        }
    }
}
