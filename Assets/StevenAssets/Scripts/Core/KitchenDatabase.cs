using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Steven.MP2A
{
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceId id;
        public int amount;

        public ResourceAmount(ResourceId id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }
    }

    [Serializable]
    public class ResourceInfo
    {
        public ResourceId id;
        public string displayName;
        public ResourceCategory category;
        public Color color = Color.white;
        public float startAmount;
        public bool showOnHud;
        [Tooltip("Optional lower/upper clamp. Max <= 0 means unbounded.")]
        public float maxAmount;
    }

    [Serializable]
    public class RecipeInfo
    {
        public string id;
        public string displayName;
        [Tooltip("Which station type offers this recipe (CraftingStation.stationKey).")]
        public string stationKey;
        public List<ResourceAmount> inputs = new List<ResourceAmount>();
        public List<ResourceAmount> outputs = new List<ResourceAmount>();
        [Tooltip("Seconds of cooking at 1 crew-less, full-heat station. 0 = instant exchange.")]
        public float duration = 6f;
        [Tooltip("Station Heat needed before the recipe can be fired (0 = no heat needed).")]
        public float minHeat;
        [Tooltip("Prep work leaves Food Scraps next to the station.")]
        public bool makesScraps;
    }

    [Serializable]
    public class DishInfo
    {
        public ResourceId dish;
        public int price = 20;
        public float orderWeight = 1f;
    }

    [Serializable]
    public class NeighborRule
    {
        public string label;
        public BuildingId a;
        public StatId statA;
        [Tooltip("Change per second applied to A while B is on a neighboring cell.")]
        public float deltaA;
        public BuildingId b;
        public StatId statB;
        [Tooltip("Change per second applied to B while A is on a neighboring cell.")]
        public float deltaB;
        public Color color = Color.white;
        [Tooltip("Optional tutorial popup shown the first time this pair interacts.")]
        public string tutorialId;
    }

    [Serializable]
    public class TutorialInfo
    {
        public string id;
        public string title;
        [TextArea(2, 8)] public string body;
    }

    [Serializable]
    public class BuildingInfo
    {
        public BuildingId id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public GameObject prefab;
        public GameObject miniPrefab;
        public GameObject ghostPrefab;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
        [Tooltip("Top of the cell stack must be one of these. Empty = must be built on bare (unbuilt) floor.")]
        public List<BuildingId> foundations = new List<BuildingId>();
        [Tooltip("Agents in PathwaysOnly mode may only walk on cells whose top building is a pathway.")]
        public bool isPathway;
        [Tooltip("Blocks agents that plan in AvoidBuildings mode.")]
        public bool blocksMovement;
        [Tooltip("Height of the top surface (what gets stacked on it sits at this height).")]
        public float height = 0.9f;
        [Tooltip("Catalog shelf row/column (for layout only).")]
        public int catalogOrder;
    }

    /// <summary>
    /// All designer data for the simulation. Edit it in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Steven/MP2A Kitchen Database", fileName = "KitchenDatabase")]
    public class KitchenDatabase : ScriptableObject
    {
        public List<ResourceInfo> resources = new List<ResourceInfo>();
        public List<BuildingInfo> buildings = new List<BuildingInfo>();
        public List<RecipeInfo> recipes = new List<RecipeInfo>();
        public List<DishInfo> dishes = new List<DishInfo>();
        public List<NeighborRule> neighborRules = new List<NeighborRule>();
        public List<TutorialInfo> tutorials = new List<TutorialInfo>();

        [NonSerialized] Dictionary<ResourceId, ResourceInfo> m_Resources;
        [NonSerialized] Dictionary<BuildingId, BuildingInfo> m_Buildings;
        [NonSerialized] Dictionary<string, RecipeInfo> m_Recipes;
        [NonSerialized] Dictionary<ResourceId, DishInfo> m_Dishes;
        [NonSerialized] Dictionary<string, TutorialInfo> m_Tutorials;

        void OnEnable() => ClearCaches();
        void OnValidate() => ClearCaches();

        public void ClearCaches()
        {
            m_Resources = null;
            m_Buildings = null;
            m_Recipes = null;
            m_Dishes = null;
            m_Tutorials = null;
        }

        void EnsureCaches()
        {
            if (m_Resources != null)
                return;
            m_Resources = new Dictionary<ResourceId, ResourceInfo>();
            foreach (var r in resources)
                if (r != null) m_Resources[r.id] = r;
            m_Buildings = new Dictionary<BuildingId, BuildingInfo>();
            foreach (var b in buildings)
                if (b != null) m_Buildings[b.id] = b;
            m_Recipes = new Dictionary<string, RecipeInfo>();
            foreach (var r in recipes)
                if (r != null && !string.IsNullOrEmpty(r.id)) m_Recipes[r.id] = r;
            m_Dishes = new Dictionary<ResourceId, DishInfo>();
            foreach (var d in dishes)
                if (d != null) m_Dishes[d.dish] = d;
            m_Tutorials = new Dictionary<string, TutorialInfo>();
            foreach (var t in tutorials)
                if (t != null && !string.IsNullOrEmpty(t.id)) m_Tutorials[t.id] = t;
        }

        public ResourceInfo GetResource(ResourceId id)
        {
            EnsureCaches();
            m_Resources.TryGetValue(id, out var info);
            return info;
        }

        public BuildingInfo GetBuilding(BuildingId id)
        {
            EnsureCaches();
            m_Buildings.TryGetValue(id, out var info);
            return info;
        }

        public RecipeInfo GetRecipe(string id)
        {
            EnsureCaches();
            if (string.IsNullOrEmpty(id))
                return null;
            m_Recipes.TryGetValue(id, out var info);
            return info;
        }

        public DishInfo GetDish(ResourceId id)
        {
            EnsureCaches();
            m_Dishes.TryGetValue(id, out var info);
            return info;
        }

        public TutorialInfo GetTutorial(string id)
        {
            EnsureCaches();
            if (string.IsNullOrEmpty(id))
                return null;
            m_Tutorials.TryGetValue(id, out var info);
            return info;
        }

        public string ResourceName(ResourceId id)
        {
            var info = GetResource(id);
            return info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : id.ToString();
        }

        public string BuildingName(BuildingId id)
        {
            var info = GetBuilding(id);
            return info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : id.ToString();
        }

        public string Describe(List<ResourceAmount> amounts)
        {
            if (amounts == null || amounts.Count == 0)
                return "nothing";
            var sb = new StringBuilder();
            for (var i = 0; i < amounts.Count; i++)
            {
                if (i > 0)
                    sb.Append(" + ");
                if (amounts[i].amount != 1)
                    sb.Append(amounts[i].amount).Append(' ');
                sb.Append(ResourceName(amounts[i].id));
            }
            return sb.ToString();
        }

        public string DescribeFoundations(BuildingInfo info)
        {
            if (info == null || info.foundations == null || info.foundations.Count == 0)
                return "bare floor";
            var sb = new StringBuilder();
            for (var i = 0; i < info.foundations.Count; i++)
            {
                if (i > 0)
                    sb.Append(" or ");
                sb.Append(info.foundations[i] == BuildingId.None ? "bare floor" : BuildingName(info.foundations[i]));
            }
            return sb.ToString();
        }

        /// <summary>Picks a dish weighted by <see cref="DishInfo.orderWeight"/>.</summary>
        /// <summary>
        /// Runtime-only menu: when set, diners only order these dishes (the menu grows as the player unlocks
        /// stations). Not saved into the asset.
        /// </summary>
        [NonSerialized] public HashSet<ResourceId> orderFilter;

        bool OnMenu(DishInfo d) => orderFilter == null || orderFilter.Count == 0 || orderFilter.Contains(d.dish);

        public DishInfo PickRandomDish()
        {
            float total = 0f;
            DishInfo first = null;
            foreach (var d in dishes)
            {
                if (!OnMenu(d))
                    continue;
                first ??= d;
                total += Mathf.Max(0f, d.orderWeight);
            }
            if (total <= 0f)
                return first ?? (dishes.Count > 0 ? dishes[0] : null);
            var roll = UnityEngine.Random.value * total;
            DishInfo last = first;
            foreach (var d in dishes)
            {
                if (!OnMenu(d))
                    continue;
                last = d;
                roll -= Mathf.Max(0f, d.orderWeight);
                if (roll <= 0f)
                    return d;
            }
            return last;
        }

        /// <summary>Returns the rule (and whether it is reversed) that applies between two building types, if any.</summary>
        public void FindNeighborRules(BuildingId x, BuildingId y, List<(NeighborRule rule, bool reversed)> results)
        {
            results.Clear();
            foreach (var rule in neighborRules)
            {
                if (rule == null)
                    continue;
                if (rule.a == x && rule.b == y)
                    results.Add((rule, false));
                else if (rule.a == y && rule.b == x)
                    results.Add((rule, true));
            }
        }
    }
}
