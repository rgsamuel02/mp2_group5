using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// MP2C "focused kitchen": switches off the systems that don't serve the learning curve and trims the
    /// restaurant down to a few dishes, a short catalog and a handful of resources. Everything here is a
    /// play-time setting (nothing in the scene or the data asset is changed), so unticking a box brings the
    /// full MP2A kitchen back.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class KitchenSimplifier : MonoBehaviour
    {
        [Header("Switch off")]
        public bool noRats = true;
        public bool noGreaseFires = true;
        [Tooltip("No hygiene inspector, no chef's temper meltdown, no stinking-bin drain.")]
        public bool noHygieneOrTemper = true;
        [Tooltip("Stations never need cleaning, repairs or oil changes.")]
        public bool noChores = true;
        [Tooltip("Neighbor exchanges keep running (sinks still water herbs) but their arcs and labels are hidden.")]
        public bool hideNeighborEffects = true;
        [Tooltip("CALL A COOK cards stay hidden until a goal unlocks them.")]
        public bool hideCallCookCards = true;

        [Header("Wild spawn channels to keep (others are switched off)")]
        public List<string> keepSpawnChannels = new List<string> { "Crates", "Deliveries" };

        [Header("Recipe tickets visible at the start (goals unlock the rest)")]
        public List<string> startRecipes = new List<string> { "charveg", "buy_produce" };

        [Header("Dishes diners can order at the start (goals add more)")]
        public List<ResourceId> startMenu = new List<ResourceId> { ResourceId.CharredVeg };

        [Header("Catalog miniatures that can ever appear")]
        public List<BuildingId> catalog = new List<BuildingId>
        {
            BuildingId.FloorTile, BuildingId.WalkwayMat, BuildingId.GasLine, BuildingId.CharGrill,
            BuildingId.DiningTable, BuildingId.PlaceSetting,
        };

        [Header("Tools that can ever appear (by name on the tool table)")]
        public List<string> tools = new List<string> { "KitchenShears", "Cleaver", "HarvestBasket" };
        static readonly string[] s_AllTools =
        {
            "WateringCan", "KitchenShears", "HarvestBasket", "Cleaver", "RollingPin", "Wrench",
            "DemolitionMallet", "SanitizerSpray", "FireExtinguisher",
        };

        [Header("Resources shown on the Pantry Board and HUD")]
        public List<ResourceId> shownResources = new List<ResourceId>
        {
            ResourceId.Gold, ResourceId.Reputation, ResourceId.CoversServed, ResourceId.LineCooks,
            ResourceId.Potato, ResourceId.Onion, ResourceId.Carrot, ResourceId.Tomato, ResourceId.Beef, ResourceId.Herbs,
            ResourceId.RawFries, ResourceId.CharredVeg, ResourceId.Fries, ResourceId.GrilledSteak,
        };

        [Header("Tips left out on purpose (mechanics for the player to discover)")]
        public List<string> undiscoveredTips = new List<string> { "crate", "cleaver", "basket", "ingredients", "harvest" };

        void Awake()
        {
            // Spawners place their first instances in Start, so trim channels before that.
            var wild = FindAnyObjectByType<WildSpawner>();
            if (wild != null)
            {
                foreach (var ch in wild.channels)
                {
                    var keep = keepSpawnChannels.Contains(ch.name) && !(noRats && ch.name == "Rats");
                    if (!keep)
                    {
                        ch.initialCount = 0;
                        ch.maxAlive = 0;
                    }
                }
            }
        }

        void Start()
        {
            var db = KitchenGame.Db;
            if (db != null)
                db.orderFilter = new HashSet<ResourceId>(startMenu);

            if (noGreaseFires)
                foreach (var f in FindObjectsByType<FireHazard>(FindObjectsInactive.Include))
                    f.enabled = false;

            var res = KitchenGame.Res;
            if (noHygieneOrTemper && res != null)
            {
                res.drifts.Clear();
                res.temperMeltdown = float.MaxValue;
                res.hygieneWarningLevel = -1f;
                foreach (var bin in FindObjectsByType<TrashBin>(FindObjectsInactive.Include))
                    bin.hygieneDrainWhenStinking = 0f;
            }

            if (hideNeighborEffects)
            {
                var n = KitchenGame.Instance != null ? KitchenGame.Instance.neighbors : FindAnyObjectByType<NeighborSystem>();
                if (n != null)
                    n.pulseRadius = 0f;
            }

            foreach (var st in FindObjectsByType<CraftingStation>(FindObjectsInactive.Include))
            {
                if (noChores)
                {
                    st.requiresCleanliness = false;
                    st.minIntegrity = -1f;
                    st.oilCostPerRecipe = 0f;
                }
                for (var i = 0; i < st.recipeIds.Count && i < st.recipeCards.Count; i++)
                    if (st.recipeCards[i] != null && !startRecipes.Contains(st.recipeIds[i]))
                        st.recipeCards[i].gameObject.SetActive(false);
                if (hideCallCookCards && st.callCookCard != null)
                    st.callCookCard.gameObject.SetActive(false);
            }

            foreach (var mini in FindObjectsByType<BlueprintMini>(FindObjectsInactive.Include))
                if (!catalog.Contains(mini.buildingId))
                    mini.gameObject.SetActive(false);

            foreach (var toolName in s_AllTools)
            {
                if (tools.Contains(toolName))
                    continue;
                var t = FindRootNamed(toolName);
                if (t != null)
                    t.SetActive(false);
            }

            foreach (var board in FindObjectsByType<ResourceBoard>(FindObjectsInactive.Include))
                foreach (var row in board.rows)
                    if (!shownResources.Contains(row.id) && row.flash != null && row.flash.transform.parent != null)
                        row.flash.transform.parent.gameObject.SetActive(false);

            var tips = KitchenGame.Instance != null ? KitchenGame.Instance.tutorials : null;
            if (tips != null)
                foreach (var id in undiscoveredTips)
                    tips.Suppress(id);
        }

        void OnDestroy()
        {
            // The filter lives on the data asset in memory; clear it so the full menu returns next time.
            var db = KitchenGame.Db;
            if (db != null)
                db.orderFilter = null;
        }

        GameObject FindRootNamed(string objectName)
        {
            foreach (var go in gameObject.scene.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    if (t.name == objectName)
                        return t.gameObject;
            return null;
        }
    }
}
